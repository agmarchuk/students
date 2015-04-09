using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;
using System.Diagnostics.Contracts;

namespace TestPolarBtree
{
    class BTree: PxCell
    {
        /// <summary>
        /// BDegree - минимальная степень дерева 
        /// зависит от объема оперативной памяти
        /// обычно берется из диапазона от 50 до 2000
        /// </summary>
        private const int BDegree = 50;

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

        public BTree(Func<object, object, int> compareKeys, string filePath, bool readOnly = false)
            : base(PStructTree(), filePath, readOnly)
        {
            this._compareKeys = compareKeys;

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


        private void DiskAllocateNode(PxEntry node)
        {
            node.Set(EmptyNode);
        }

        private void SplitRoot()
        {
            var keys = Root.UElement().Field(1).Get() as object[];
            int numChilds= Root.UElement().Field(3).Elements().ToArray().Count();


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

            bool isLeaf = (bool)Root.UElement().Field(2).Get();
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
                Right.UElement().Field(3).Element(i).SetHead((byte[])childsRight[i - numChilds / 2]);
            }

            //var res = Root.GetValue();
            //Console.WriteLine("\n" + res.Type.Interpret(res.Value));
            //Console.ReadKey();

            //for (int i = 0; i < 2 * BDegree; i++)
            //{
            //    Left.UElement().Field(3).Element(i).Set(childs[i]);
            //    Right.UElement().Field(3).Element(i).Set(childs[i]);
            //}

        }

        /// <summary>
        /// Добавление ключей в узел
        /// </summary>
        /// <param name="key"></param>
        public void Add(long key)
        {
            
            // когда дерево пустое, организовать одиночное значение
            if (Root.Tag() == 0)
            {
                //TODO: Требуется выполнять только один раз при добавлении первого ключа
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


                //TODO: случай когда равно
                if (key <= middleKey)//Здесь должен быть компаратор compareKeys
                    AddInNode(parent, leftTree, key);
                else
                    AddInNode(parent, rightTree, key);

                return;
            }
            else if (isLeaf == true)
            { 
                this.InsertNonFull(ref arrayKeys, node, key);
                return;
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

        private int InsertKeyInArray(ref object[] arrayKeys, long key)
        {
            //TODO: Реализовать "правильную" вставку (сдвигом) нового ключа с сохранением порядка
            int NumKeys = arrayKeys.Length;

            Contract.Assert(NumKeys <= 2 * BDegree - 1);

            Array.Resize<object>(ref arrayKeys, NumKeys + 1);//выделяем место под новый ключ
            arrayKeys[NumKeys] = (object)key;
            Array.Sort(arrayKeys);
            return Array.FindIndex(arrayKeys, ent => ((long)ent == key));
        }

        private void InsertNonFull(ref object[] arrayKeys, PxEntry node, long key)
        {
            InsertKeyInArray(ref arrayKeys, key);

            node.UElement().Field(0).Set(arrayKeys.Length);
            node.UElement().Field(1).Set(arrayKeys);
            node.UElement().Field(2).Set(true);
        }

        public void SplitNode(PxEntry parent, PxEntry node,out PxEntry leftTree, out PxEntry rightTree, out long middleKey)
        {
            var keysNode = node.UElement().Field(1).Get() as object[];
            var keysParent = parent.UElement().Field(1).Get() as object[];
            int numChilds = node.UElement().Field(3).Elements().ToArray().Count();

            middleKey = (long)keysNode[BDegree-1];

            int position = InsertKeyInArray(ref keysParent,middleKey);
            parent.UElement().Field(0).Set(keysParent.Length);
            parent.UElement().Field(1).Set(keysParent);

            PxEntry Left = node;
            PxEntry Right = GetPlace4Child(parent, position);

            DiskAllocateNode(Right);

            object[] arr1 = new object[(keysNode.Length - 1) / 2];
            object[] arr2 = new object[(keysNode.Length - 1) / 2];

            for (int i = 0; i < BDegree - 1; i++)
            {
                arr1[i] = keysNode[i];
                arr2[i] = keysNode[i + BDegree];
                //Console.WriteLine(arr1[i] + "||" + arr2[i]);
            }
                        

            byte[][] childsLeft = new byte[numChilds][];
            byte[][] childsRight = new byte[numChilds][];

            for (int i = 0; i < numChilds / 2; i++)
            {
                childsLeft[i] = parent.UElement().Field(3).Element(i).GetHead();
            }
            for (int i = numChilds / 2; i < numChilds; i++)
            {
                childsRight[i - numChilds / 2] = parent.UElement().Field(3).Element(i).GetHead();
            }

            bool isLeaf = (bool)node.UElement().Field(2).Get();

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
                Right.UElement().Field(3).Element(i).SetHead((byte[])childsRight[i - numChilds / 2]);
            }

            leftTree = Left;
            rightTree = Right;
            node = Left;
            //object[] ob = Right.UElement().Field(1).Get() as object[];
            //for (int j = 0; j < ob.Length; j++)
            //        Console.Write((long)ob[j]+" ");
            //Console.ReadKey();
        }

     
        private PxEntry GetPlace4Child(PxEntry parent, int position)
        {
            //parent должен иметь по максимуму пустых дочерних узлов
            int numKeysInNode = (int)parent.UElement().Field(0).Get();
            int numChilds = numKeysInNode + 1;
            for (int j = numChilds-1; j > position; j-- )
            {
                var headChild = parent.UElement().Field(3).Element(j-1).GetHead();
                parent.UElement().Field(3).Element(j).SetHead(headChild);
            }
            //DiskAllocateNode(parent.UElement().Field(3).Element(position));

            return parent.UElement().Field(3).Element(position);
        }

        /// <summary>
        /// Метод поиска ключа в дереве
        /// </summary>
        /// <param name="node"></param>
        /// <param name="key"></param>
        /// <returns></returns>
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
            
            //Получаем ссылку на дочерний узел, содержащий диапазон ключей, 
            //в котором может быть найден необходимый ключ
            PxEntry child = node.UElement().Field(3).Element(indexChild);

            return Search(child, key);
        }


         //public void Delete();

    }
}
