using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;
using System.IO;
using Common;

namespace ExtendedIndexBTree
{
    public class OptimBTreeInd: PaCell,IIndex
    {
        public int bDegree { get; private set; }

        private PaCell index_cell;
        private string path;

        private PType typeElement;

        public PCell IndexCell { get { return index_cell; } }

        private readonly Func<object, object, int> keyComparer;//используется при поиске ключа
        private readonly Func<object, object, int> elementComparer;//используется при добавлении элементов

        public Func<object, object> keyProducer { get; set; }
        public Func<object, int> halfProducer { get; set; }
        
        private object[] PTypeNull;

        private static PTypeRecord PStructTree(int bDegree, PType tpElement)
        {
            NamedType[] nt = new NamedType[1 + (2 * bDegree - 1) + (2 * bDegree)];

            nt[0] = new NamedType("NumKeys", new PType(PTypeEnumeration.integer));
                
            for (int i=1;i<2*bDegree;++i)
            {
                nt[i] = new NamedType("Keys"+i, new PTypeSequence(tpElement));
                nt[i + 2 * bDegree] = new NamedType("Childs"+i, new PTypeSequence(new PType(PTypeEnumeration.longinteger)));
            }

            var sequence = new PTypeRecord(nt);
            return sequence;
        }

        private struct Row
        {
            static int _bDegree;
            int NumKeys;
            object[] ArrayKeys;
            object[] Childs;

            public Row(int degree) {
                _bDegree = degree;
                NumKeys = 0;
                ArrayKeys = new object[2 * _bDegree];
                Childs = new object[2 * _bDegree + 1];
            }

            public void SetNumKeys(int value)
            {
                NumKeys = value;
            }
            public void SetArrayKeys(object[] array)
            {
                for(int i=0;i<=array.Count(); ++i)
                {
                    ArrayKeys[i] = array[i];
                }
            }
            public void SetChilds(object[] array)
            {
                for (int i = 0; i <= array.Count(); ++i)
                {
                    Childs[i] = array[i];
                }
            }

            public object[] GetRow()
            {
                object[] objs = new object[1 + 2 * _bDegree + (2 * _bDegree + 1)];
                int i;
                objs[0] = (object)NumKeys;
                for (i = 1; i <= 2*_bDegree; ++i)
                {
                    objs[i] = ArrayKeys[i];
                    objs[i + 2 * _bDegree] = Childs[i];
                }

                objs[i + 2 * _bDegree + 1] = Childs[i + 1];
                return objs;
            }
        };


        public OptimBTreeInd(PType tpElement,
                        Func<object, object, int> keyComparer,
                        Func<object, object, int> elementComparer,
                        string filePath,
                        bool readOnly = false,
                        int degree = 125
                        ) : base(PStructTree(degree, tpElement), filePath, readOnly)
        {
            index_cell = this;
            typeElement = tpElement;
            this.keyComparer = keyComparer;
            this.elementComparer = elementComparer;
            bDegree = degree;

            index_cell.Clear();
            index_cell.Fill(new object[0]);

            path = filePath;
        }

        public void AppendElement(object key)
        {
            Add(key);
        }
   
        /// <summary>
        /// Разделение корня дерева
        /// </summary>
        //private void SplitRoot()
        //{
        //    var keys = Root.UElement().Field(1).Get() as object[];
        //    bool isLeaf = (bool)Root.UElement().Field(2).Get();
        //    int numChilds = 0;
        //    if (!isLeaf)
        //        numChilds = keys.Length + 1;

        //    object[] arr1 = new object[(keys.Length - 1) / 2];
        //    object[] arr2 = new object[(keys.Length - 1) / 2];

        //    for (int i = 0; i < bDegree - 1; i++)
        //    {
        //        arr1[i] = keys[i];
        //        arr2[i] = keys[i + bDegree];
        //    }

        //    byte[][] childsLeft = new byte[numChilds][];
        //    byte[][] childsRight = new byte[numChilds][];

        //    for (int i = 0; i < numChilds / 2; i++)
        //    {
        //        childsLeft[i] = Root.UElement().Field(3).Element(i).GetHead();
        //    }
        //    for (int i = numChilds / 2; i < numChilds; i++)
        //    {
        //        childsRight[i - numChilds / 2] = Root.UElement().Field(3).Element(i).GetHead();
        //    }

        //    bool isLeafLeft = false,
        //         isLeafRight = false;
        //    if (isLeaf)
        //    {
        //        isLeafLeft = true;
        //        isLeafRight = true;
        //    }

        //    DiskAllocateNode(Root);

        //    Root.UElement().Field(0).Set(1);
        //    Root.UElement().Field(1).Set(new object[] { keys[bDegree - 1] });
        //    Root.UElement().Field(2).Set(false);

        //    PxEntry Left = Root.UElement().Field(3).Element(0);
        //    PxEntry Right = Root.UElement().Field(3).Element(1);

        //    DiskAllocateNode(Left);
        //    DiskAllocateNode(Right);

        //    Left.UElement().Field(0).Set(arr1.Length);
        //    Left.UElement().Field(1).Set(arr1);
        //    Left.UElement().Field(2).Set(isLeafLeft);

        //    Right.UElement().Field(0).Set(arr2.Length);
        //    Right.UElement().Field(1).Set(arr2);
        //    Right.UElement().Field(2).Set(isLeafRight);


        //    for (int i = 0; i < numChilds / 2; i++)
        //    {
        //        Left.UElement().Field(3).Element(i).SetHead((byte[])childsLeft[i]);
        //    }
        //    for (int i = numChilds / 2; i < numChilds; i++)
        //    {
        //        Right.UElement().Field(3).Element(i - numChilds / 2).SetHead((byte[])childsRight[i - numChilds / 2]);
        //    }
        //}

        /// <summary>
        /// Разделение узла
        /// </summary>
        /// <param name="parent">родитель</param>
        /// <param name="node">узел</param>
        /// <param name="leftTree">левое поддерево</param>
        /// <param name="rightTree">правое поддерево</param>
        /// <param name="middleKey">медианный ключ</param>
        //private void SplitNode(PxEntry parent, PxEntry node, out PxEntry leftTree, out PxEntry rightTree, out object middleKey)
        //{
        //    var keysNode = node.UElement().Field(1).Get() as object[];
        //    var keysParent = parent.UElement().Field(1).Get() as object[];
        //    bool isLeaf = (bool)node.UElement().Field(2).Get();
        //    int numChilds = 0;
        //    if (!isLeaf)
        //        numChilds = keysNode.Length + 1;

        //    middleKey = keysNode[bDegree - 1];

        //    int position = InsertKeyInArray(ref keysParent, middleKey);
        //    parent.UElement().Field(0).Set(keysParent.Length);
        //    parent.UElement().Field(1).Set(keysParent);

        //    PxEntry Left = node;
        //    PxEntry Right = GetPlace4Child(parent, position + 1);

        //    object[] arr1 = new object[(keysNode.Length - 1) / 2];
        //    object[] arr2 = new object[(keysNode.Length - 1) / 2];

        //    for (int i = 0; i < bDegree - 1; i++)
        //    {
        //        arr1[i] = keysNode[i];
        //        arr2[i] = keysNode[i + bDegree];
        //    }

        //    byte[][] childsLeft = new byte[numChilds][];
        //    byte[][] childsRight = new byte[numChilds][];

        //    for (int i = 0; i < numChilds / 2; i++)
        //    {
        //        childsLeft[i] = node.UElement().Field(3).Element(i).GetHead();
        //    }
        //    for (int i = numChilds / 2; i < numChilds; i++)
        //    {
        //        childsRight[i - numChilds / 2] = node.UElement().Field(3).Element(i).GetHead();
        //    }

        //    bool isLeafLeft = false,
        //         isLeafRight = false;

        //    if (isLeaf)
        //    {
        //        isLeafLeft = true;
        //        isLeafRight = true;
        //    }

        //    Left.UElement().Field(0).Set(arr1.Length);
        //    Left.UElement().Field(1).Set(arr1);
        //    Left.UElement().Field(2).Set(isLeafLeft);

        //    Right.UElement().Field(0).Set(arr2.Length);
        //    Right.UElement().Field(1).Set(arr2);
        //    Right.UElement().Field(2).Set(isLeafRight);

        //    for (int i = 0; i < numChilds / 2; i++)
        //    {
        //        Left.UElement().Field(3).Element(i).SetHead((byte[])childsLeft[i]);
        //    }
        //    for (int i = numChilds / 2; i < numChilds; i++)
        //    {
        //        Left.UElement().Field(3).Element(i).Set(new object[] { 0, null });
        //        Right.UElement().Field(3).Element(i - numChilds / 2).SetHead((byte[])childsRight[i - numChilds / 2]);
        //    }

        //    leftTree = Left;
        //    rightTree = Right;
        //}

        /// <summary>
        /// Добавление ключей в узел
        /// </summary>
        private void Add(object element)
        {
            //когда дерево пустое, организовать одиночное значение
            if (Root.Count()==0)
            {
                Row r = new Row(bDegree);

                r.SetNumKeys(1);
                r.SetArrayKeys(new object[] { element });
                r.SetChilds(new object[0]);

                Root.AppendElement( r.GetRow() );
                IndexCell.Flush();
            }
            else
            {
                var node = Root.Element(0);
                AddInNode(node, node, element);
            }

        }
        /// <summary>
        /// Вставка ключа в узел
        /// </summary>
        /// <param name="parent">родительский узел</param>
        /// <param name="node">текущий узел</param>
        /// <param name="key">ключ</param>
        private void AddInNode(PaEntry parent, PaEntry node, object element)
        {
            object[] arrayKeys = (node.Field(1).Get() as object[]);
            int numKeysInNode = (int)node.Field(0).Get();
            bool isLeaf = ((node.Field(2).Get() as object[]).Count()==0) ? true :false; //TODO: new заменить

            //Если узел заполнен
            if (numKeysInNode == 2 * bDegree - 1)
            {
                object middleKey = arrayKeys[bDegree];

                //SplitNode(parent, node, out leftTree, out rightTree, out middleKey);
                object[] LeftArray = new object[bDegree];
                object[] RightArray = new object[bDegree];

                for (int i=0;i<bDegree;++i)
                {
                    LeftArray[i] = arrayKeys[i];
                    RightArray[i] = arrayKeys[bDegree + i];
                }

                long offsetLeftTree = Root.AppendElement(new object[] { bDegree, LeftArray, new object[0] });
                long offsetRightTree = Root.AppendElement(new object[] { bDegree, RightArray, new object[0] });

                object[] parentArrayKeys = (parent.Field(1).Get() as object[]);
                InsertNonFull(ref parentArrayKeys, parent, middleKey);
                parent.Field(2).Set(new object[] {offsetLeftTree, offsetRightTree });

                if (elementComparer(element, middleKey) < 0)
                {
                    PaEntry child = new PaEntry(typeElement, offsetLeftTree, (PaCell)IndexCell);
                    AddInNode(parent, child, element);
                }
                else
                {
                    PaEntry child = new PaEntry(typeElement, offsetRightTree, (PaCell)IndexCell);
                    AddInNode(parent, child, element);
                }
            }
            else
            if (isLeaf)
            {
                InsertNonFull(ref arrayKeys, node, element);
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

                long offset = (long)node.Field(2).Element(indexChild).Get();
                PaEntry entryChild = new PaEntry(PStructTree(bDegree,typeElement),offset,(PaCell)IndexCell);

                AddInNode(node, entryChild, element);
            }

            IndexCell.Flush();
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
                arrayKeys[position] = arrayKeys[position - 1];
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
        private void InsertNonFull(ref object[] arrayKeys, PaEntry node, object element)
        {
            InsertKeyInArray(ref arrayKeys, element);
            node.Field(0).Set(arrayKeys.Count());
            node.Field(1).Set( arrayKeys );
            node.Field(2).Set(new object[0]);
        }

        /// <summary>
        /// Выделение памяти под дочерний узел
        /// </summary>
        /// <param name="parent">родитель</param>
        /// <param name="position">позиция</param>
        /// <returns>указатель на новый дочерний узел</returns>
        //private PxEntry GetPlace4Child(PxEntry parent, int position)
        //{
        //    int numKeysInNode = (int)parent.UElement().Field(0).Get();
        //    int numChilds = numKeysInNode + 1;

        //    for (int j = bDegree * 2 - 1; j > position; --j)
        //    {
        //        var headChild = parent.UElement().Field(3).Element(j - 1).GetHead();
        //        parent.UElement().Field(3).Element(j).SetHead(headChild);
        //    }
        //    DiskAllocateNode(parent.UElement().Field(3).Element(position));
        //    return parent.UElement().Field(3).Element(position);
        //}

        /// <summary>
        /// Функция поиска
        /// </summary>
        /// <param name="node">исходный узел</param>
        /// <param name="element">искомый ключ</param>
        /// <returns>оффсет</returns>
        public long Search(PxEntry node, object element)
        {
            object[] arrayKeys;
            while (true)
            {
                arrayKeys = (node.UElement().Field(1).Get() as object[]);
                int numKeysInNode = (int)node.UElement().Field(0).Get();
                int indexChild = numKeysInNode;
                for (int i = 0; i < numKeysInNode; ++i)
                {
                    int cmp = keyComparer(element, arrayKeys[i]);
                    if (cmp == 0)
                    {
                        long offset = (long)(arrayKeys[i] as object[])[0];
                        return offset;
                    }
                    if (cmp < 0)
                    {
                        indexChild = i;
                        break;
                    }
                }
                bool isLeaf = (bool)node.UElement().Field(2).Get();
                if (isLeaf == true)
                    return Int64.MinValue;

                node = node.UElement().Field(3).Element(indexChild);
            }
        }

        /// <summary>
        /// Рекурсивный поиск ключа в дереве (не оптимальный)
        /// </summary>
        /// <param name="node">Узел, с которого начинается поиск</param>
        /// <param name="key">Ключ</param>
        /// <param name="position">номер ключа в узле</param>
        /// <returns>узел дерева</returns>
        public PxEntry Search(PxEntry node, object element, out int position)
        {
            //получаем массив ключей из узла
            object[] arrayKeys = (node.UElement().Field(1).Get() as object[]);
            int numKeysInNode = (int)node.UElement().Field(0).Get();

            int indexChild = numKeysInNode;
            for (int i = 0; i < numKeysInNode; ++i)
            {
                int cmp = keyComparer(element, arrayKeys[i]);

                if (cmp == 0)
                {
                    position = i;
                    return node;
                }
                if (cmp < 0)
                {
                    indexChild = i;
                    break;
                }
            }

            bool isLeaf = (bool)node.UElement().Field(2).Get();

            var entry = Root;
            if (isLeaf == true)
            {
                position = 0;
 //               return new PxEntry(entry.Typ, Int64.MinValue, entry.fis);
            }

            //продолжаем поиск ключа в дочернем узле 
            PxEntry child = node.UElement().Field(3).Element(indexChild);
            return Search(child, element, out position);
        }

        /// <summary>
        /// Метод поиска (используется при удалении)
        /// </summary>
        /// <param name="node">исходный узел</param>
        /// <param name="element">искомый ключ</param>
        /// <param name="position">номер ключа в узле</param>
        /// <param name="pathToNode">путь до узла, в котором найден ключ</param>
        /// <returns>узел дерева</returns>
        private PxEntry Search(PxEntry node, object element, out int position, ref List<PxEntry> pathToNode)
        {
            //Делать из вне
            //if (pathToNode == null)
            //    pathToNode = new List<PxEntry>();

            pathToNode.Add(node);
            //получаем массив ключей из узла
            object[] arrayKeys = (node.UElement().Field(1).Get() as object[]);
            int numKeysInNode = (int)node.UElement().Field(0).Get();

            int indexChild = numKeysInNode;
            for (int i = 0; i < numKeysInNode; ++i)
            {
                int cmp = keyComparer(element, arrayKeys[i]);

                if (cmp == 0) { position = i; return node; }
                if (cmp < 0) { indexChild = i; break; }
            }

            bool isLeaf = (bool)node.UElement().Field(2).Get();

            var entry = Root;
            if (isLeaf == true)
            {
                position = 0;
//                return new PxEntry(entry.Typ, Int64.MinValue, entry.fis);
            }

            //продолжаем поиск ключа в дочернем узле 
            PxEntry child = node.UElement().Field(3).Element(indexChild);
            return Search(child, element, out position, ref pathToNode);
        }

        /// <summary>
        /// вывод дерева в файл (для отладки)
        /// </summary>
        /// <param name="path">путь до файла</param>
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

        // TODO: дописать
        private void Delete(ref object key, int pos, ref PxEntry node)
        {
            //получаем массив ключей из узла
            object[] arrayKeys = (node.UElement().Field(1).Get() as object[]);
            int numKeysInNode = (int)node.UElement().Field(0).Get();
            for (int i = pos; i < numKeysInNode - 1; ++i)
            {
                arrayKeys[i] = arrayKeys[i + 1];
            }
            --numKeysInNode;
            Array.Resize(ref arrayKeys, numKeysInNode);
            node.UElement().Field(0).Set(numKeysInNode);
            node.UElement().Field(1).Set(arrayKeys);
        }












        public void DeleteElement(object key)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            IndexCell.Close();
        }

        public IEnumerable<long> FindAll(object key)
        {
            throw new NotImplementedException();
        }

        public long FindFirst(object key)
        {
            throw new NotImplementedException();
        }
    }
}
