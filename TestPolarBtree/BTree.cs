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
        private const int BDegree = 2;

        private readonly Func<object, object, int> _compareKeys;

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
        /// Добавление ключей в узел
        /// </summary>
        /// <param name="key"></param>
        public void Add(long key)
        {
            // когда дерево пустое, организовать одиночное значение
            if (Root.Tag() == 0)
            {//TODO: Требуется выполнять только один раз при добавлении первого ключа
                object[] childsEmpty = new object[2 * BDegree];

                for (int i=0;i<2*BDegree;i++)
                {
                    childsEmpty[i] = new object[] { 0, null };
                }
                object[] ob =
                    {
                        1,
                        new object[]
                        {
                            1,
                            new object[] { key },
                            true,
                            childsEmpty
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
            object[] arrayKeys = (node.UElement().Field(1).Get() as object[]);
            int numKeysInNode = (int)node.UElement().Field(0).Get();

            bool isLeaf = (bool)node.UElement().Field(2).Get();
            //Если узел - лист
            if (isLeaf == true)
            { 
                // Если лист не заполнен
                if (numKeysInNode < 2 * BDegree - 1)
                {
                    this.InsertNonFull(ref arrayKeys, node, key, numKeysInNode);
                    return;
                }
                //Если заполнен!!!!!!!!!!!!!!!
                //var newNode = SplitNode(ref arrayKeys, Root, node); //for debug (:
                //TODO: необходимо определять parent 
                //var newNode = SplitNode(ref arrayKeys, parent, node);
                //AddInNode(newNode, key);                   

            }
            else //Если узел - НЕ лист
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

                //var newNode = SplitNode(ref arrayKeys, parent, node);
                //AddInNode(newNode, key);

                //SplitNode(ref ArrayKeys, parent, indexChild, node);
                //AddInNode();

            }
        }

        private int InsertKeyInArray(ref object[] arrayKeys, long key)
        {
            //TODO: Реализовать "правильную" вставку (сдвигом) нового ключа с сохранением порядка
            int NumKeys = arrayKeys.Length;
            Array.Resize<object>(ref arrayKeys, NumKeys + 1);//выделяем место под новый ключ
            arrayKeys[NumKeys] = (object)key;
            Array.Sort(arrayKeys);
            return Array.FindIndex(arrayKeys, ent => ((long)ent == key));
        }

        private void InsertNonFull(ref object[] arrayKeys, PxEntry node, long key, int numKeys)
        {
            InsertKeyInArray(ref arrayKeys, key);

            bool isLeaf = (bool)node.UElement().Field(2).Get();
            //Если лист
            if (isLeaf == true)
            {
                object[] ob =
                {
                    1,
                    new object[]
                    {
                        numKeys+1,
                        arrayKeys,
                        true,
                        new object[0]
                    }
                };


            node.Set(ob);
            }
            
        }

        public PxEntry SplitNode(ref object[] arrayKeys, PxEntry parent, PxEntry child)
        {
            var parentKeys = parent.UElement().Field(1).Get() as object[];
            int IndexInsert = InsertKeyInArray(ref parentKeys, (long)arrayKeys[BDegree - 1]);
            var childs = parent.UElement().Field(3).Elements();
            
            //Array.Resize(ref childs,childs.Length+1);

            //for (int i = childs.Length; i > IndexInsert; --i)
            //{
            //    childs[i] = childs[i - 1];
            //}
            ////TODO: создание нового узла
            //childs[IndexInsert] = new object[0];

            //object[] obj =
            //{
            //    parentKeys.Length,
            //    parentKeys,
            //    false,
            //    childs
            //};
            //parent.Set(obj);
            var ArrayChilds = this.Root.UElement().Field(3).Element(1);
            
            object[] arr1 = new object[(int)(arrayKeys.Length - 1) / 2];
            object[] arr2 = new object[(int)(arrayKeys.Length - 1) / 2];

            for (int i = 0; i < (int)(arrayKeys.Length - 1) / 2; i++)
            {
                 arr1[i] = arrayKeys[i];
                 arr2[i] = arrayKeys[i+BDegree];
            }

            object[] ob =
            {
                1,
                new object[]
                    {
                    1,
                    new object[]{(long)arrayKeys[BDegree-1]},
                    false,
                    new object[]
                        {
                            new object[] {//child#1
                                1,
                                new object[]{
                                    arr1.Length,
                                    arr1,
                                    true,
                                    new object[0]
                                    }
                                },
                            new object[] {//child#2
                                1,
                                new object[]{
                                    arr2.Length,
                                    arr2,
                                    true,
                                    new object[0]
                                    }
                                }
                        }

                    }
            };
            this.Fill2(ob);

            
            return parent;
        }

        private void InsertChild(PxEntry parent,object[] child, int position)
        {
            //parent должен иметь по максимуму пустых дочерних узлов
            int numKeysInNode = (int)parent.UElement().Field(0).Get();
            int numChilds = numKeysInNode + 1;
            for (int j = numChilds; j >= position; j-- )
            {
                var headChild = parent.UElement().Field(3).Element(j).GetHead();
                parent.UElement().Field(3).Element(j+1).SetHead(headChild);
            }
            parent.UElement().Field(3).Element(position).Set(child);
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
