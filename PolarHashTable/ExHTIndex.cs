using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using PolarDB;

namespace PolarHashTable
{
    using HashType = UInt64;

    public class ExHTIndex : PaCell, IIndex
    {
        private string path;
        private PaCell index_cell;
        private PaCell table_db;
        private readonly Func<object, object, int> keyComparer;//используется при поиске ключа
        private readonly Func<object, object, int> elementComparer;//используется при добавлении элементов

        public PCell IndexCell { get { return index_cell; } }

        /// <summary>
        /// Тип хеш-таблицы
        /// </summary>
        /// <param name="tpElementHashTable">структура элемента таблицы, содержит ссылку на лист ключей</param>
        /// <returns></returns>
        private static PTypeSequence PStructHashTable(PType tpElementHashTable) //tpElementHashTable = {hash} || {offset, hash}
        {
            return new PTypeSequence(tpElementHashTable);
        }

        public ExHTIndex(PType tpElement,
                       Func<object, object, int> keyComparer,
                       PaCell database,
                       string filePath,
                       bool readOnly = false) : base(PStructHashTable(tpElement), filePath + "hashTableIndex.pac", readOnly)
        {
            path = filePath;
            index_cell = this;
            this.keyComparer = keyComparer;
            table_db = database;

            index_cell.Clear();
            index_cell.Fill(new object[0]);
        }

        public new void Flush()
        {
            index_cell.Flush();
        }

        public void AppendElement(object key)
        {
            object[] pair = (object[])key;
            long hash = (long)pair[1];
            long offset = (long)pair[0];

            object[] offsets = new object[0];

            long insertIndex = GetIndexElement(hash);

            if (insertIndex == long.MinValue)
            {
                Array.Resize(ref offsets, 1);
                offsets[0] = offset;
                if (index_cell.Root.Count() == 0)
                {
                    index_cell.Root.Set(new object[] { 0, new object[0] });
                    index_cell.Root.Element(0).Set(new object[] { hash, offsets });
                }
                else
                {
                    index_cell.Root.Element(index_cell.Root.Count() + 1).Set(new object[] { 0, new object[0] });
                    index_cell.Root.Element(index_cell.Root.Count() + 1).Set(new object[] { hash, offsets });
                }
            }
            else
            {
                offsets = (object[])index_cell.Root.Element(insertIndex).Field(1).Get();
                InsertKeyInArray(ref offsets, offset);
                index_cell.Root.Element(insertIndex).Field(1).Set(offsets);
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
            Array.Resize(ref arrayKeys, NumKeys + 1);//выделяем место под новый ключ

            int position = NumKeys;
            while ((position > 0) && ((long)element < (long)arrayKeys[position - 1]))
            {
                arrayKeys[position] = arrayKeys[position - 1];
                --position;
            }

            arrayKeys[position] = element;
            return position;
        }

        public void DeleteElement(object key)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            index_cell.Close();
        }

        public IEnumerable<long> FindAll(object key)
        {
            throw new NotImplementedException();
        }

        private long GetIndexElement(object key)
        {
            long hash = (long)key;
            bool founded = false;
            long index = 0;
            foreach (var ent in index_cell.Root.Elements())
            {
                long currHash = (long)ent.Field(0).Get();
                if (hash == currHash)
                {
                    founded = true;
                    break;
                }
                ++index;
            }
            if (!founded)
                return long.MinValue;
            return index;
        }

        public long FindFirst(object key)
        {
            throw new NotImplementedException();
        }

        public HashType GetHashCode(object obj)
        {
            HashFunctions hf = new HashFunctions();
            return hf.GetHashAdd((string)obj);
        }
    }
}
