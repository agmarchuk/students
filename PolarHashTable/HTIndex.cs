using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using PolarDB;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace PolarHashTable
{
    using HashType = UInt32;

    public class HTIndex : PaCell, IIndex
    {
        private string path;
        private PaCell index_cell, table_db;
        private readonly Func<object, object, int> keyComparer;//используется при поиске ключа
        private readonly Func<object, object, int> elementComparer;//используется при добавлении элементов

        public PCell IndexCell { get { return index_cell; } }

        private List<long>[] offsets = new List<long>[1];

        /// <summary>
        /// Тип хеш-таблицы
        /// </summary>
        /// <param name="tpElementHashTable">структура элемента таблицы, содержит ссылку на лист ключей</param>
        /// <returns></returns>
        private static PTypeSequence PStructHashTable(PType tpElementHashTable) //tpElementHashTable = {hash} || {offset, hash}
        {
            return new PTypeSequence(tpElementHashTable);
        }

        public HTIndex(PType tpElement,
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
            int insertIndex = (int)pair[1];
            List<long> tempOffsetsList = new List<long>();

            if (insertIndex >= offsets.Length)
            {
                Array.Resize(ref offsets, insertIndex + 1);
            }
            else
            {
                if (offsets[insertIndex] != null) tempOffsetsList = offsets[insertIndex];
            }

            tempOffsetsList.Add((long)pair[0]);
            tempOffsetsList.Sort();
            offsets[insertIndex] = tempOffsetsList;
            index_cell.Root.AppendElement(new object[] { insertIndex });
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
            List<long> offs = new List<long>();
            HashFunctions hf = new HashFunctions();
            PaEntry entry = table_db.Root.Element(0);//TODO: придумать, как избавиться от ссылки на опорную БД
                                                     //Скорее всего это возможно, только если запихать листы офсетов в индексный файл
            int hash = (int)hf.GetHashAdd((string)key);

            IEnumerable<PaEntry> found = index_cell.Root.BinarySearchAll(ent =>//TODO: не работает лямбда
            {
                int currHash = (int)ent.Field(0).Get();
                if (hash == currHash)
                {
                    List<long> tempOffsetsList = offsets[hash];
                    bool founded = false;
                    foreach (long off in tempOffsetsList)
                    {
                        entry.offset = off;
                        int cmp = String.Compare((string)key, (string)entry.Field(1).Get());
                        if (cmp == 0) { offs.Add(off); founded = true; }
                    }
                    if (founded) return 0;
                }
                if (currHash < hash) return -1;
                return 1;
            });
            return offs;
        }

        public long FindFirst(object key)
        {
            PaEntry entry = table_db.Root.Element(0);
            int hash = (int)GetHashCode((string)key);

            PaEntry found = index_cell.Root.BinarySearchFirst(ent =>
            {
                int currHash = (int)ent.Field(0).Get();
                if (hash == currHash)
                {
                    List<long> tempOffsetsList = offsets[hash];
                    foreach(long off in tempOffsetsList)
                    {
                        entry.offset = off;
                        int cmp = String.Compare((string)key, (string)entry.Field(1).Get());
                        if (cmp == 0) return 0;
                    }
                }
                if (currHash < hash) return -1;
                return 1;
            });
            if (found.IsEmpty) return 0;
            return found.offset;
        }

        public void SaveToBinaryFile()
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (FileStream fstream = new FileStream(path + "offsets.dat", FileMode.Create))
            {
                bf.Serialize(fstream, offsets);
            }
        }

        public List<long>[] OpenFromBinaryFile()
        {
            BinaryFormatter bf = new BinaryFormatter();
            using (FileStream fstream = new FileStream(path + "offsets.dat", FileMode.Open))
            {
                offsets = (List<long>[])bf.Deserialize(fstream);
            }
            return offsets;
        }

        public void RestoreOffsets(IEnumerable<PaEntry> records)
        {
        }

        public HashType GetHashCode(object obj)
        {
            HashFunctions hf = new HashFunctions();
            return hf.GetHashPJW((string)obj);
        }
    }
}
