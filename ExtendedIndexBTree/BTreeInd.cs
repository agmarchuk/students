using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;
using System.Diagnostics.Contracts;
using System.IO;

namespace ExtendedIndexBTree
{
    public class BTreeInd : PxCell, IIndex
    {
        /// <summary>
        /// BDegree - минимальная степень дерева 
        /// зависит от объема оперативной памяти
        /// обычно берется из диапазона от 50 до 2000
        /// </summary>
        public int bDegree;

        private PxCell index_cell;
        private string path;

        public PxCell IndexCell { get { return index_cell; } }

        private readonly Func<object, object, int> keyComparer;//используется при поиске ключа
        private readonly Func<object, object, int> elementComparer;//используется при добавлении элементов

        public Func<object, object> keyProducer { get; set; }
        public Func<object, int> halfProducer { get; set; }

        private object[] emptyNode;

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
        /// <param name="ptElement">тип ключа</param>
        /// <param name="keyComparer">компаратор</param>
        /// <param name="elementComparer">компаратор</param>
        /// <param name="filePath">путь к файлу</param>
        /// <param name="readOnly">флаг чтения файла</param>
        /// <param name="degree">Степень дерева</param>
        public BTreeInd(PType tpElement,
                        Func<object, object, int> keyComparer,
                        Func<object, object, int> elementComparer,
                        string filePath, 
                        bool readOnly = false,
                        int degree = 125
                        ) : base(PStructTree(tpElement), filePath, readOnly)
        {
            this.keyComparer = keyComparer;
            this.elementComparer = elementComparer;
            bDegree = degree;
            object[] childsEmpty = new object[2 * bDegree];

            for (int i = 0; i < 2 * bDegree; i++)
            {
                childsEmpty[i] = new object[] { 0, null };
            }

            emptyNode = new object[]
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

            path = filePath;

            index_cell = this;
        }

        /// <summary>
        /// Выделение памяти для узла
        /// </summary>
        /// <param name="node">узел</param>
        private void DiskAllocateNode(PxEntry node)
        {
            node.Set(emptyNode);
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

            for (int i = 0; i < bDegree - 1; i++)
            {
                arr1[i] = keys[i];
                arr2[i] = keys[i + bDegree];
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
            Root.UElement().Field(1).Set(new object[] { keys[bDegree - 1] });
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

            middleKey = keysNode[bDegree - 1];

            int position = InsertKeyInArray(ref keysParent, middleKey);
            parent.UElement().Field(0).Set(keysParent.Length);
            parent.UElement().Field(1).Set(keysParent);

            PxEntry Left = node;
            PxEntry Right = GetPlace4Child(parent, position + 1);

            object[] arr1 = new object[(keysNode.Length - 1) / 2];
            object[] arr2 = new object[(keysNode.Length - 1) / 2];

            for (int i = 0; i < bDegree - 1; i++)
            {
                arr1[i] = keysNode[i];
                arr2[i] = keysNode[i + bDegree];
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
        private void Add(object element)
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
                if (numKeysInNode == 2 * bDegree - 1)
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
            if (numKeysInNode == 2 * bDegree - 1)
            {
                object middleKey;
                PxEntry leftTree, rightTree;

                SplitNode(parent, node, out leftTree, out rightTree, out middleKey);

                if (elementComparer(element, middleKey) < 0)
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

            for (int j = bDegree * 2 - 1; j > position; --j)
            {
                var headChild = parent.UElement().Field(3).Element(j - 1).GetHead();
                parent.UElement().Field(3).Element(j).SetHead(headChild);
            }
            DiskAllocateNode(parent.UElement().Field(3).Element(position));
            return parent.UElement().Field(3).Element(position);
        }

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
                return new PxEntry(entry.Typ, Int64.MinValue, entry.fis); 
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
                int cmp = keyComparer(element,arrayKeys[i]);

                if (cmp == 0) { position = i; return node; }
                if (cmp < 0) { indexChild = i; break; }
            }

            bool isLeaf = (bool)node.UElement().Field(2).Get();

            var entry = Root;
            if (isLeaf == true)
            {
                position = 0;
                return new PxEntry(entry.Typ, Int64.MinValue, entry.fis);
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

        private bool DeleteKey(ref object key)
        {
            //if (this.Root.Count() == 0) return false;

            int pos;
            //List<PxEntry> pathToNode = new List<PxEntry>();
            PxEntry found = Search(Root, key, out pos);
            if (found.IsEmpty)
                throw new Exception("Deleted key not found");

            if ((bool)found.UElement().Field(2).Get()) // является ли узел листом
            {
                Delete(ref key, pos, ref found);
            }
            // делать из вне
            //PaEntry entry = tableNames.Root.Element(0);

            //if (!found.IsEmpty)
            //{

            //    object[] keys = (object[])found.UElement().Field(1).Get();
            //    entry.offset = (long)(((object[])(keys[pos]))[0]);

            //    entry.Field(2).Set(true);//Устанавливаем флаг удаления ключа в опорной таблице

            //}
            //TODO: Удаление ключа из дерева

            return true;
        }


        /// <summary>
        /// Поиск всех указателей на ключи из опорной таблицы
        /// </summary>
        /// <param name="key">поисковый ключ</param>
        /// <returns>возвращает набор офсетов</returns>
        public IEnumerable<long> FindAll(object key)
        {
            List<long> offsets = new List<long>();
            Queue<PxEntry> entries = new Queue<PxEntry>();

            PxEntry currentNode;
            entries.Enqueue(Root);
            
            while (entries.Count > 0)
            {
                currentNode = entries.Dequeue();
                object[] arrayKeys = (currentNode.UElement().Field(1).Get() as object[]);
                int numKeysInNode = (int)currentNode.UElement().Field(0).Get();
                bool isLeaf = (bool)currentNode.UElement().Field(2).Get();
                int indexChild = numKeysInNode;

                for (int i = 0; i < numKeysInNode; ++i) // TODO: необходим рефакторинг
                {
                    object[] pair = (object[])arrayKeys[i];
                    int cmp = keyComparer(key, arrayKeys[i]);

                    PxEntry child;

                    if (cmp < 0)
                    {
                        if (!isLeaf)
                        {
                            child = currentNode.UElement().Field(3).Element(i);
                            entries.Enqueue(child);
                        }
                        break;
                    }

                    if (cmp == 0)
                    {
                        offsets.Add((long)pair[0]);
                        if (!isLeaf)
                        {
                            child = currentNode.UElement().Field(3).Element(i);
                            entries.Enqueue(child);
                        }
                    }

                    if ((i == numKeysInNode - 1) && (!isLeaf))
                    {
                        child = currentNode.UElement().Field(3).Element(indexChild);
                        entries.Enqueue(child);
                    }
                }
            }

            return offsets;
        }

        /// <summary>
        /// Метод поиска первого вхождения ключа
        /// </summary>
        /// <param name="key">искомый ключ</param>
        /// <returns>offset записи из опорной таблицы</returns>
        public long FindFirst(object key)
        {
            return Search(Root, key);
        }

        /// <summary>
        /// Метод добавления элемента в дерево
        /// </summary>
        /// <param name="key">ключ</param>
        public void AppendElement(object key)
        {
            Add(key);
        }

        /// <summary>
        /// Метод удаления ключа из дерева
        /// </summary>
        /// <param name="key">ключ</param>
        public void DeleteElement(object key)
        {
            throw new NotImplementedException();
            //DeleteKey(ref key);
        }

        /// <summary>
        /// Освобождение ячейки
        /// </summary>
        public void Dispose()
        {
            index_cell.Close();
        }
    }
}


        