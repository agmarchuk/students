using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace TestPolarBtree
{
    class BTree: PxCell
    {
        /// <summary>
        /// BDegree - минимальная степень дерева 
        /// зависит от объема оперативной памяти
        /// обычно берется из диапазона от 50 до 2000
        /// </summary>
        private int BDegree = 2;

        private readonly Func<object, object, int> CompareKeys;

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

        public BTree(Func<object, object, int> CompareKeys, string filePath, bool readOnly = false)
            : base(PStructTree(), filePath, readOnly)
        {
            this.CompareKeys = CompareKeys;
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
                    3,
                    new object[] { 333L,444L,555L },
                    false,
                    new object[] {
                        new object[] {//child#1
                            1,
                            new object[]{
                                2,
                                new object[] { 111L,222L},
                                false,
                                new object[0]
                                }
                        },
                        new object[] {//child#2
                            1,
                            new object[]{
                                2,
                                new object[] { 666L,777L},
                                false,
                                new object[0]
                                }
                        }
                    }
                }
            };
            this.Fill2(ob);

            var res3 = this.Root.GetValue();
            //Console.WriteLine(res3.Type.Interpret(res3.Value));
            return res3;
            //var res4 = this.Root.UElementUnchecked(1).Field(3).GetHead();
            //Console.WriteLine(res4);

        }
       
        /// <summary>
        /// Добавление ключей в узел
        /// </summary>
        /// <param name="key"></param>
        public void Add(long key)
        {
            // когда дерево пустое, организовать одиночное значение
            if (Root.Tag() == 0)
            {//TODO: Требуется выполнять только один раз при добавлении первого ключа
                object[] ob =
                    {
                        1,
                        new object[]
                        {
                            1,
                            new object[] { key },
                            true,
                            new object[0]
                        }
                    };

                this.Fill2(ob);
            }
            else
            {
                AddInNode(Root, key);
            }



        }

        private void AddInNode(PxEntry node, long key)
        {
            //получаем массив ключей из узла
            object[] ArrayKeys = (node.UElementUnchecked(1).Field(1).Get() as object[]);
            int NumKeysInNode = (int)node.UElementUnchecked(1).Field(0).Get();

            bool isLeaf = (bool)node.UElementUnchecked(1).Field(2).Get();
            //Если узел - лист
            if (isLeaf == true)
            {
                // Если лист не заполнен
                if (NumKeysInNode < 2 * BDegree - 1)
                {
                    this.InsertNonFull(ref ArrayKeys, node, key, NumKeysInNode);
                    return;
                }
                //Если заполнен
                //TODO: Необходимо создание нового узла
                //var newNode = SplitNode(ref ArrayKeys, parent, node);
                //AddInNode(newNode, key);
            }
            else //Если узел - НЕ лист
            {
                int indexChild = NumKeysInNode;
                for (int i = 0; i < NumKeysInNode; ++i)
                {
                    if (key < (long)ArrayKeys[i])
                    {
                        indexChild = i;
                        break;
                    }
                }
                //TODO: Как перейти к потомку
                //nextChild = ArrayChilds[indexChild];

                //SplitNode(ref ArrayKeys, parent, indexChild, node);
                //AddInNode();

            }
        }

        private void InsertNonFull(ref object[] ArrayKeys, PxEntry node, long key, int NumKeys)
        {
            
            //TODO: Реализовать "правильную" вставку нового ключа с сохранением порядка
            Array.Resize<object>(ref ArrayKeys, NumKeys + 1);//выделяем место под новый ключ
            ArrayKeys[NumKeys] = (object)key;
            Array.Sort(ArrayKeys);

            bool IsLeaf = (bool)node.UElementUnchecked(1).Field(2).Get();
            //Если лист
            if (IsLeaf == true)
            {
                object[] ob =
                {
                    1,
                    new object[]
                    {
                        NumKeys+1,
                        ArrayKeys,
                        true,
                        new object[0]
                    }
                };


            node.Set(ob);
            }
            
        }

        //public PxEntry SplitNode(ref object[] ArrayKeys, PxEntry parent, PxEntry child)
        //{
        //    int middlePosition = BDegree;
        //    long middleKey = (long)ArrayKeys[middlePosition];

        //    //var oldNode = node.Get();

        //    object[] leftKeys = new object[BDegree - 1];
        //    object[] rightKeys = new object[BDegree - 1];

        //    for (int i = 0; i < BDegree; i++)
        //    {
        //        leftKeys[i] = ArrayKeys[i];
        //        rightKeys[i] = ArrayKeys[(BDegree + 1) + i];
        //    }
        //    //TODO: создание нового узла
        //    PxEntry newNode;
        //    return newNode;
        //}

        /// <summary>
        /// Метод поиска ключа в дереве
        /// </summary>
        /// <param name="node"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Search(PxEntry node, long key)
        {
            //получаем массив ключей из узла
            object[] ArrayKeys = (node.UElementUnchecked(1).Field(1).Get() as object[]);
            int NumKeysInNode = (int)node.UElementUnchecked(1).Field(0).Get();

            int indexChild = NumKeysInNode;
            for (int i = 0; i < NumKeysInNode; ++i)
            {
                if (key == (long)ArrayKeys[i]) return true;
                else
                    if (key < (long)ArrayKeys[i])
                    {
                        indexChild = i;
                        break;
                    }
            }

            bool IsLeaf = (bool)node.UElementUnchecked(1).Field(2).Get();

            if (IsLeaf == true) return false;

            //получаем массив дочерних узлов
            var res3 = node.UElementUnchecked(1).Field(1).Element(0).GetValue();
            Console.WriteLine(res3.Type.Interpret(res3.Value));

            PxEntry pe = node.UElementUnchecked(1).Field(3).Element(1);

            //TODO: Определить, как осуществлять переход к потомку узла
            return Search(node.UElementUnchecked(1).Field(3).Element(1), key);
                
        }


         //public void Delete();

    }
}
