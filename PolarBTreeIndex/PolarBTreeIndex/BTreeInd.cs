using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;
using System.Diagnostics.Contracts;
using System.IO;

namespace PolarBtreeIndex
{
    class BTreeInd: PxCell
    {
        /// <summary>
        /// BDegree - минимальная степень дерева 
        /// зависит от объема оперативной памяти
        /// обычно берется из диапазона от 50 до 2000
        /// </summary>
        public int BDegree;

        private readonly Func<object, object, int> elementComparer;
        private readonly Func<object, string, int> hashComparer;

        //Получатель массива ключей в узле
        //private object[] KeysGetter(object ob)
        //{
        //    object[] node = (object[])ob;
        //    object[] keys = new object[node.Length];

        //    for(int i=0;i<node.Length;i++)
        //    {
        //        keys[i]=node[0];
        //    }
        //    return keys;
        //}

        //private object GetKey(object ob)
        //{
        //    object[] node = (object[])ob;
        //    return node[0];
        //}

        private object[] EmptyNode;

        private static PTypeUnion PStructTree(PType tpElement)
        {
            var structBtree = new PTypeUnion();
            structBtree.Variants = new[]{
                new NamedType("empty", new PType(PTypeEnumeration.none)),
                new NamedType("node", new PTypeRecord(//узел
                    new NamedType("NumKeys", new PType(PTypeEnumeration.integer)),//количество ключей
                    new NamedType("Keys", new PTypeSequence(tpElement)),//массив ключей
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
        /// <param name="ptElement">тип ключа</param>
        /// <param name="compareKeys">компаратор</param>
        /// <param name="filePath">путь к файлу</param>
        /// <param name="readOnly">флаг чтения файла</param>
        public BTreeInd(int Degree, 
                        PType ptElement,
                        Func<object, string, int> hashComparer,
                        Func<object, object, int> elementComparer, 
                        string filePath, bool 
                        readOnly = false)
                        : base(PStructTree(ptElement), filePath, readOnly)
        {
            this.elementComparer = elementComparer;
            this.hashComparer = hashComparer;
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
        private void SplitNode(PxEntry parent, PxEntry node, out PxEntry leftTree, out PxEntry rightTree, out object middleKey)
        {
            var keysNode = node.UElement().Field(1).Get() as object[];
            var keysParent = parent.UElement().Field(1).Get() as object[];
            bool isLeaf = (bool)node.UElement().Field(2).Get();
            int numChilds = 0;
            if (!isLeaf)
                numChilds = keysNode.Length + 1;

            middleKey = keysNode[BDegree - 1];

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
        public void Add(object element)
        {
            //когда дерево пустое, организовать одиночное значение
            if (Root.Tag() == 0)
            {
                DiskAllocateNode(Root);

                Root.UElement().Field(0).Set(1);
                Root.UElement().Field(1).Set(new object[] { element });
            }
            else
            {
                int numKeysInNode = (int)Root.UElement().Field(0).Get();
                if (numKeysInNode == 2 * BDegree-1)
                {
                    SplitRoot();
                }

                AddInNode(Root, Root, element);
            }

        }

        /// <summary>
        /// Вставка ключа в узел
        /// </summary>
        /// <param name="parent">родительский узел</param>
        /// <param name="node">текущий узел</param>
        /// <param name="key">ключ</param>
        private void AddInNode(PxEntry parent, PxEntry node, object element)
        {
            //получаем массив ключей из узла
            object[] arrayKeys = (node.UElement().Field(1).Get() as object[]);
            int numKeysInNode = (int)node.UElement().Field(0).Get();

            bool isLeaf = (bool)node.UElement().Field(2).Get();

            //Если узел заполнен
            if (numKeysInNode == 2 * BDegree - 1)
            {
                object middleKey;
                PxEntry leftTree, rightTree;

                SplitNode(parent, node, out leftTree, out rightTree, out middleKey);
                
                if (elementComparer(element, middleKey)<0)
                    AddInNode(parent, leftTree, element);
                else
                    AddInNode(parent, rightTree, element);
            }
            else if (isLeaf == true)
            {
                this.InsertNonFull(ref arrayKeys, node, element);
            }
            else 
            {
                int indexChild = numKeysInNode;
                for (int i = 0; i < numKeysInNode; ++i)
                {
                    if (elementComparer(element, arrayKeys[i]) < 0)
                    {
                        indexChild = i;
                        break;
                    }
                }

                AddInNode(node, node.UElement().Field(3).Element(indexChild), element);
            }
        }

        /// <summary>
        /// Вставка ключа в массив ключей
        /// </summary>
        /// <param name="arrayKeys">массив ключей</param>
        /// <param name="key">ключ</param>
        /// <returns>позиция вставленного ключа в массиве</returns>
        private int InsertKeyInArray(ref object[] arrayKeys, object element)
        {
            int NumKeys = arrayKeys.Length;
            Array.Resize<object>(ref arrayKeys, NumKeys + 1);//выделяем место под новый ключ
            
            int position = NumKeys;
            while ((position > 0) && (elementComparer(element, arrayKeys[position - 1]) < 0))
            {
                arrayKeys[position]=arrayKeys[position-1];
                --position;
            }

            arrayKeys[position] = element;
            return position;
        }

        /// <summary>
        /// Вставка в незаполненный узел
        /// </summary>
        /// <param name="arrayKeys">массив ключей</param>
        /// <param name="node">узел</param>
        /// <param name="key">ключ</param>
        private void InsertNonFull(ref object[] arrayKeys, PxEntry node, object element)
        {
            InsertKeyInArray(ref arrayKeys, element);

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
        /// <returns>узел дерева</returns>
        public PxEntry Search(PxEntry node, object element, out int position)
        {
            //получаем массив ключей из узла
            object[] arrayKeys = (node.UElement().Field(1).Get() as object[]);
            int numKeysInNode = (int)node.UElement().Field(0).Get();

            int h = ((string)element).GetHashCode();
            int indexChild = numKeysInNode;
            for (int i = 0; i < numKeysInNode; ++i)
            {
                if (hashComparer(arrayKeys[i], (string)element) == 0) { position = i; return node; }
                else
                    if (hashComparer(arrayKeys[i], (string)element) < 0)
                    {
                        indexChild = i;
                        break;
                    }
            }

            bool isLeaf = (bool)node.UElement().Field(2).Get();

            var entry = Root;
            if (isLeaf == true) { position = 0; return new PxEntry(entry.Typ, Int64.MinValue, entry.fis); }
            
            //продолжаем поиск ключа в дочернем узле 
            PxEntry child = node.UElement().Field(3).Element(indexChild);
            return Search(child, element, out position);
        }

        public void WriteTreeInFile(string path)
        {
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
        }

         //public void Delete();

    }
}
