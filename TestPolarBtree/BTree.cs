using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;
using System.Diagnostics.Contracts;
using System.IO;

namespace TestPolarBtree
{
    class BTree: PxCell
    {
        /// <summary>
        /// BDegree - минимальная степень дерева 
        /// зависит от объема оперативной памяти
        /// обычно берется из диапазона от 50 до 2000
        /// </summary>
        private int BDegree;

        private readonly Func<object, object, int> _compareKeys;

        private object[] EmptyNode;

        private static PTypeUnion PStructTree()
        {
            var structBtree = new PTypeUnion();
            structBtree.Variants = new[]{
                new NamedType("empty", new PType(PTypeEnumeration.none)),
                new NamedType("node", new PTypeRecord(//узел
                    new NamedType("NumKeys", new PType(PTypeEnumeration.integer)),//количество ключей
                    new NamedType("Keys", new PTypeSequence(new PType(PTypeEnumeration.longinteger))),//массив ключей от t-1 до 2t-1 в каждом узле
                    new NamedType("IsLeaf", new PType(PTypeEnumeration.boolean)),//является ли узел листом
                    new NamedType("Childs", new PTypeSequence(structBtree))//дочерние узлы
                    ))
            };
            return structBtree;
        }
        
        /// <summary>
        /// Конструктор B-дерева
        /// </summary>
        /// <param name="Degree">Степень дерева</param>
        /// <param name="compareKeys">компаратор</param>
        /// <param name="filePath">путь к файлу</param>
        /// <param name="readOnly">флаг чтения файла</param>
        public BTree(int Degree, Func<object, object, int> compareKeys, string filePath, bool readOnly = false)
            : base(PStructTree(), filePath, readOnly)
        {
            this._compareKeys = compareKeys;
            BDegree = Degree;

            object[] childsEmpty = new object[2 * BDegree];

            for (int i = 0; i < 2 * BDegree; i++)
            {
                childsEmpty[i] = new object[] { 0, null };
            }

            EmptyNode = new object[]
            {
                1,
                new object[]
                {
                    0,
                    new object[0],
                    true,
                    childsEmpty
                }
            };
        }

        /// <summary>
        /// Тестовое выделение памяти для узлов
        /// </summary>
        public void TestAllocate() 
        {
            DiskAllocateNode(Root);
            DiskAllocateNode(Root.UElement().Field(3).Element(0));
            
            var H = Root.UElement().Field(3).Element(0).GetHead();
            Root.UElement().Field(3).Element(1).SetHead(H);

            var child = Root.UElement().Field(3).Element(0);
            DiskAllocateNode(child.UElement().Field(3).Element(0));
            Console.WriteLine(child.UElement().Field(3).Element(0).offset);
        }

        /// <summary>
        /// Тестовое заполнение дерева в ручную
        /// </summary>
        public PValue TestFillTree()
        {
            object[] ob =
            {
                1,
                new object[]
                    {
                    1,
                    new object[] { 333L },
                    false,
                    new object[] {
                        new object[] {//child#1
                            1,
                            new object[]{
                                2,
                                new object[] { 111L,222L},
                                true,
                                new object[0]
                                }
                        },
                        new object[] {//child#2
                            1,
                            new object[]{
                                3,
                                new object[] { 444L,555L,666L},
                                true,
                                new object[0]
                                }
                        },
                        new object[] {//память под третий child
                            0, 
                            null
                        }
                    }
                }
            };
            this.Fill2(ob);

            var res3 = this.Root.GetValue();
            //Console.WriteLine(res3.Type.Interpret(res3.Value));
            return res3;
        }

        /// <summary>
        /// Выделение памяти для узла
        /// </summary>
        /// <param name="node">узел</param>
        private void DiskAllocateNode(PxEntry node)
        {
            node.Set(EmptyNode);
        }

        /// <summary>
        /// Разделение корня дерева
        /// </summary>
        private void SplitRoot()
        {
            var keys = Root.UElement().Field(1).Get() as object[];
            bool isLeaf = (bool)Root.UElement().Field(2).Get();
            int numChilds = 0;
            if (!isLeaf)
                numChilds = keys.Length + 1;

            object[] arr1 = new object[(keys.Length - 1) / 2];
            object[] arr2 = new object[(keys.Length - 1) / 2];

            for (int i = 0; i < BDegree - 1; i++)
            {
                arr1[i] = keys[i];
                arr2[i] = keys[i + BDegree];
            }

            byte[][] childsLeft = new byte[numChilds][];
            byte[][] childsRight = new byte[numChilds][];

            for (int i = 0; i < numChilds / 2; i++)
            {
                childsLeft[i] = Root.UElement().Field(3).Element(i).GetHead();
            }
            for (int i = numChilds / 2; i < numChilds; i++)
            {
                childsRight[i - numChilds / 2] = Root.UElement().Field(3).Element(i).GetHead();
            }

            bool isLeafLeft = false,
                 isLeafRight = false;
            if (isLeaf)
            {
                isLeafLeft = true;
                isLeafRight = true;
            }

            DiskAllocateNode(Root);

            Root.UElement().Field(0).Set(1);
            Root.UElement().Field(1).Set(new object[] { keys[BDegree-1] });
            Root.UElement().Field(2).Set(false);

            PxEntry Left = Root.UElement().Field(3).Element(0);
            PxEntry Right = Root.UElement().Field(3).Element(1);

            DiskAllocateNode(Left);
            DiskAllocateNode(Right);

            Left.UElement().Field(0).Set(arr1.Length);
            Left.UElement().Field(1).Set(arr1);
            Left.UElement().Field(2).Set(isLeafLeft);

            Right.UElement().Field(0).Set(arr2.Length);
            Right.UElement().Field(1).Set(arr2);
            Right.UElement().Field(2).Set(isLeafRight);


            for (int i = 0; i < numChilds / 2; i++)
            {
                Left.UElement().Field(3).Element(i).SetHead((byte[])childsLeft[i]);
            }
            for (int i = numChilds / 2; i < numChilds; i++)
            {
                Right.UElement().Field(3).Element(i - numChilds / 2).SetHead((byte[])childsRight[i - numChilds / 2]);
            }
        }

        /// <summary>
        /// Разделение узла
        /// </summary>
        /// <param name="parent">родитель</param>
        /// <param name="node">узел</param>
        /// <param name="leftTree">левое поддерево</param>
        /// <param name="rightTree">правое поддерево</param>
        /// <param name="middleKey">медианный ключ</param>
        private void SplitNode(PxEntry parent, PxEntry node, out PxEntry leftTree, out PxEntry rightTree, out long middleKey)
        {
            var keysNode = node.UElement().Field(1).Get() as object[];
            var keysParent = parent.UElement().Field(1).Get() as object[];
            bool isLeaf = (bool)node.UElement().Field(2).Get();
            int numChilds = 0;
            if (!isLeaf)
                numChilds = keysNode.Length + 1;

            middleKey = (long)keysNode[BDegree - 1];

            int position = InsertKeyInArray(ref keysParent, middleKey);
            parent.UElement().Field(0).Set(keysParent.Length);
            parent.UElement().Field(1).Set(keysParent);

            PxEntry Left = node;
            PxEntry Right = GetPlace4Child(parent, position + 1);

            object[] arr1 = new object[(keysNode.Length - 1) / 2];
            object[] arr2 = new object[(keysNode.Length - 1) / 2];

            for (int i = 0; i < BDegree - 1; i++)
            {
                arr1[i] = keysNode[i];
                arr2[i] = keysNode[i + BDegree];
            }

            byte[][] childsLeft = new byte[numChilds][];
            byte[][] childsRight = new byte[numChilds][];

            for (int i = 0; i < numChilds / 2; i++)
            {
                childsLeft[i] = node.UElement().Field(3).Element(i).GetHead();
            }
            for (int i = numChilds / 2; i < numChilds; i++)
            {
                childsRight[i - numChilds / 2] = node.UElement().Field(3).Element(i).GetHead();
            }

            bool isLeafLeft = false,
                 isLeafRight = false;

            if (isLeaf)
            {
                isLeafLeft = true;
                isLeafRight = true;
            }

            Left.UElement().Field(0).Set(arr1.Length);
            Left.UElement().Field(1).Set(arr1);
            Left.UElement().Field(2).Set(isLeafLeft);

            Right.UElement().Field(0).Set(arr2.Length);
            Right.UElement().Field(1).Set(arr2);
            Right.UElement().Field(2).Set(isLeafRight);

            for (int i = 0; i < numChilds / 2; i++)
            {
                Left.UElement().Field(3).Element(i).SetHead((byte[])childsLeft[i]);
            }
            for (int i = numChilds / 2; i < numChilds; i++)
            {
                Left.UElement().Field(3).Element(i).Set(new object[] { 0, null });
                Right.UElement().Field(3).Element(i - numChilds / 2).SetHead((byte[])childsRight[i - numChilds / 2]);
            }

            leftTree = Left;
            rightTree = Right;
        }

        /// <summary>
        /// Добавление ключей в узел
        /// </summary>
        /// <param name="key"></param>
        public void Add(long key)
        {
            //когда дерево пустое, организовать одиночное значение
            if (Root.Tag() == 0)
            {
                DiskAllocateNode(Root);

                Root.UElement().Field(0).Set(1);
                Root.UElement().Field(1).Set(new object[] {key});
            }
            else
            {
                int numKeysInNode = (int)Root.UElement().Field(0).Get();
                if (numKeysInNode == 2 * BDegree-1)
                {
                    SplitRoot();
                }

                AddInNode(Root, Root, key);
            }

        }

        /// <summary>
        /// Вставка ключа в узел
        /// </summary>
        /// <param name="parent">родительский узел</param>
        /// <param name="node">текущий узел</param>
        /// <param name="key">ключ</param>
        private void AddInNode(PxEntry parent, PxEntry node, long key)
        {
            //получаем массив ключей из узла
            object[] arrayKeys = (node.UElement().Field(1).Get() as object[]);
            int numKeysInNode = (int)node.UElement().Field(0).Get();

            bool isLeaf = (bool)node.UElement().Field(2).Get();

            //Если узел заполнен
            if (numKeysInNode == 2 * BDegree - 1)
            {
                long middleKey = 0;
                PxEntry leftTree, rightTree;

                SplitNode(parent, node, out leftTree, out rightTree, out middleKey);
                
                if (key <= middleKey)
                    AddInNode(parent, leftTree, key);
                else
                    AddInNode(parent, rightTree, key);
            }
            else if (isLeaf == true)
            { 
                this.InsertNonFull(ref arrayKeys, node, key);
            }
            else 
            {
                int indexChild = numKeysInNode;
                for (int i = 0; i < numKeysInNode; ++i)
                {
                    if (key < (long)arrayKeys[i])
                    {
                        indexChild = i;
                        break;
                    }
                }

                AddInNode(node, node.UElement().Field(3).Element(indexChild), key);
            }
        }

        /// <summary>
        /// Вставка ключа в массив ключей
        /// </summary>
        /// <param name="arrayKeys">массив ключей</param>
        /// <param name="key">ключ</param>
        /// <returns>позиция вставленного ключа в массиве</returns>
        private int InsertKeyInArray(ref object[] arrayKeys, long key)
        {
            int NumKeys = arrayKeys.Length;
            Array.Resize<object>(ref arrayKeys, NumKeys + 1);//выделяем место под новый ключ
            
            int position = NumKeys;
            while ((position>0)&&(key < (long)arrayKeys[position-1]))
            {
                arrayKeys[position]=arrayKeys[position-1];
                --position;
            }

            arrayKeys[position] = (object)key;
            return position;
        }

        /// <summary>
        /// Вставка в незаполненный узел
        /// </summary>
        /// <param name="arrayKeys">массив ключей</param>
        /// <param name="node">узел</param>
        /// <param name="key">ключ</param>
        private void InsertNonFull(ref object[] arrayKeys, PxEntry node, long key)
        {
            InsertKeyInArray(ref arrayKeys, key);

            node.UElement().Field(0).Set(arrayKeys.Length);
            node.UElement().Field(1).Set(arrayKeys);
            node.UElement().Field(2).Set(true);
        }

        /// <summary>
        /// Выделение памяти под дочерний узел
        /// </summary>
        /// <param name="parent">родитель</param>
        /// <param name="position">позиция</param>
        /// <returns>указатель на новый дочерний узел</returns>
        private PxEntry GetPlace4Child(PxEntry parent, int position)
        {
            int numKeysInNode = (int)parent.UElement().Field(0).Get();
            int numChilds = numKeysInNode + 1;

            for (int j = BDegree*2-1; j > position; --j )
            {
                var headChild = parent.UElement().Field(3).Element(j-1).GetHead();
                parent.UElement().Field(3).Element(j).SetHead(headChild);
            }
            DiskAllocateNode(parent.UElement().Field(3).Element(position));
            return parent.UElement().Field(3).Element(position);
        }

        /// <summary>
        /// Метод поиска ключа в дереве
        /// </summary>
        /// <param name="node">Узел, с которого начинается поиск</param>
        /// <param name="key">Ключ</param>
        /// <returns>Возвращает true, если ключ найден, иначе false</returns>
        public bool Search(PxEntry node, long key)
        {
            //получаем массив ключей из узла
            object[] arrayKeys = (node.UElement().Field(1).Get() as object[]);
            int numKeysInNode = (int)node.UElement().Field(0).Get();

            int indexChild = numKeysInNode;
            for (int i = 0; i < numKeysInNode; ++i)
            {
                if (key == (long)arrayKeys[i]) return true;
                else
                    if (key < (long)arrayKeys[i])
                    {
                        indexChild = i;
                        break;
                    }
            }

            bool isLeaf = (bool)node.UElement().Field(2).Get();

            if (isLeaf == true) return false;

            //получаем массив ключей дочернего узла
            // var res3 = node.UElement().Field(3).Element(1).UElement().Field(1).GetValue();
            //Console.WriteLine(res3.Type.Interpret(res3.Value));
            
            //продолжаем поиск ключа в дочернем узле 
            PxEntry child = node.UElement().Field(3).Element(indexChild);
            return Search(child, key);
        }

        public void WriteTreeInFile(string path)
        {
            Console.WriteLine("Вывод дерева в файл...");
            StreamWriter swriter = File.CreateText(path);
            try
            {
                var res = Root.GetValue();
                swriter.WriteLine(res.Type.Interpret(res.Value));
            }
            finally
            {
                swriter.Close();
            }
            Console.WriteLine("Вывод дерева в файл закончен. Файл находится в " + Path.GetDirectoryName(path));
        }

         //public void Delete();

    }
}
