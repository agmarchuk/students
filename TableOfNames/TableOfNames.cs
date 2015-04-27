using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;
using System.ComponentModel;
using System.Threading;
using System.Runtime.Caching;

namespace TableOfNames
{
    class TableOfNames
    {
        private string path;

        private PType tp, binTreeType;
        public static PaCell tableNames, index;
        private BinaryTreeIndex binTreeInd, binTreeInd_tmp;
        private CacheItemPolicy policy;

        //Компаратор для офсетов на строки
        private Func<object, object, int> elementCompare = (object ob1, object ob2) =>
            {
                object[] node1 = (object[])ob1;
                int hash1 = (int)node1[1];

                object[] node2 = (object[])ob2;
                int hash2 = (int)node2[1];

                if (hash1 == hash2) //идем в опорную таблицу, если хеши равны
                {
                    PaEntry entry = tableNames.Root.Element(0);
                    long offset1 = (long)node1[0];
                    long offset2 = (long)node2[0];

                    object[] pair1 = (object[])entry.Get();
                    entry.offset = offset1;
                    string name1 = (string)pair1[1];
                    entry.offset = offset2;
                    object[] pair2 = (object[])entry.Get();
                    string name2 = (string)pair2[1];

                    return String.Compare(name1, name2, StringComparison.Ordinal);//вернётся: -1,0,1
                }
                else
                    return ((hash1 < hash2) ? -1 : 1);
               
            };

         
        private const long maxSizeDictionary = 100000;


        BackgroundWorker worker = new BackgroundWorker();
        private AutoResetEvent autoEvent;
        private object treeLocker = new object();

        private Dictionary<string, long> index_dictionary_1 = new Dictionary<string, long>();
        private Dictionary<string, long> index_dictionary_2 = new Dictionary<string, long>();

        private PaEntry SearchString(string srch)
        {
            PaEntry entry = tableNames.Root.Element(0);
            int hash = srch.GetHashCode();

            PxEntry found = binTreeInd.BinarySearch(pe =>
            {
                object[] node = (object[])pe.Get();

                if (hash == (int)node[1])
                {
                    entry.offset = (long)node[0];
                    object getStr = entry.Field(1).Get();
                    int resultCompare = String.Compare((string)getStr, srch, StringComparison.Ordinal); //srch.CompareTo(getStr);
                    return resultCompare;
                }
                else
                    return ((hash < (int)node[1]) ? -1 : 1);
            });

            if (!found.IsEmpty)
            {
                entry.offset = (long)((object[])found.Get())[0];
                bool isDeleted = (bool)entry.Field(2).Get();
                if (!isDeleted)
                {
                    return entry;
                }
            }

            entry.offset = Int64.MinValue;
            return entry;
        }

        public long GetCount()
        {
            return tableNames.Root.Count();
        }

        public void TreeShow()
        {
            var rez = binTreeInd.Root.GetValue();
            Console.WriteLine(rez.Type.Interpret(rez.Value));
        }

        public void Warmup()
        {
            foreach (var v in tableNames.Root.ElementValues());
            foreach (var v in index.Root.ElementValues());
            //TODO: Разогрев дерева?
        }

        public long? GetIdByString(string srchStr) 
        {
            PaEntry ent;
            long offset;
            bool isExists1 = index_dictionary_1.TryGetValue(srchStr, out offset);

            if (isExists1)
            {
                ent = tableNames.Root.Element(0);
                ent.offset = offset;
                return (long)ent.Field(0).Get();
            }

            bool isExists2 = index_dictionary_2.TryGetValue(srchStr, out offset);

            if (isExists2)
            {
                ent = tableNames.Root.Element(0);
                ent.offset = offset;
                return (long)ent.Field(0).Get();
            }

            if (tableNames.Root.Count() == 0)
                return null;

            lock (treeLocker)
            {
                ent = SearchString(srchStr);
            }

            if (ent.IsEmpty)
                return null;

            object[] pair = (object[])ent.Get();

            return (long)pair[0];
        }

        public string GetStringById(int id)
        {
            if (id < 0 || id > tableNames.Root.Count() || index.IsEmpty)
                throw new Exception("Строки с таким id нет");

            if (tableNames.Root.Count()==0 || tableNames.IsEmpty)
                throw new Exception("Таблица имен пуста");

            PaEntry ent = tableNames.Root.Element(0);
            ent.offset = (long)index.Root.Element(id).Field(0).Get();
            return (string)ent.Field(1).Get();
        }

        public TableOfNames(string path)
        {
            this.path = path;

            //задаём тип для записи в ячейку БД
            tp = new PTypeSequence(new PTypeRecord(
                new NamedType("id",new PType(PTypeEnumeration.longinteger)),
                new NamedType("string",new PType(PTypeEnumeration.sstring)),
                new NamedType("deleted", new PType(PTypeEnumeration.boolean)))
            );

            if (!System.IO.File.Exists(path + "TableOfNames.pac"))
            {
                //создаём БД
                tableNames = new PaCell(tp, path + "TableOfNames.pac", false);

                //очистка БД
                tableNames.Clear();
                tableNames.Fill(new object[0]);
            }
            //типы ячеек для таблицы с офсетами и для таблицы "offset-id"
            PType tp_of = new PTypeSequence(new PType(PTypeEnumeration.longinteger));
            PType tp_id = new PTypeSequence(new PTypeRecord(
                new NamedType("offset", new PType(PTypeEnumeration.longinteger)),
                new NamedType("id", new PType(PTypeEnumeration.longinteger)))
            );

            //узел дерева состоит из офсета и хешкода
            binTreeType = new PTypeRecord(
                new NamedType("offset", new PType(PTypeEnumeration.longinteger)),
                new NamedType("hash", new PType(PTypeEnumeration.integer))
            );

            //Индексы
            index = new PaCell(tp_id, path + "index.pac", false);
            binTreeInd = new BinaryTreeIndex(binTreeType, elementCompare, path + "IndexNames.pax",false);

            //Фоновый поток
            autoEvent = new AutoResetEvent(true);
            worker.DoWork += backgroundWorker1_DoWork;
            worker.RunWorkerCompleted += backgroundWorker1_RunWorkerCompleted;
            worker.WorkerSupportsCancellation = true;
            
            ////cache
            //policy = new CacheItemPolicy();
            //policy.AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(10.0);

            //List<string> CacheFiles = new List<string>();
            //CacheFiles.Add(path + "IndexNames.pax");
            //policy.ChangeMonitors.Add(new HostFileChangeMonitor(CacheFiles));
        }

        public void Dispose()
        {
            autoEvent.WaitOne();
       
            index.Close();
            binTreeInd.Close();
            tableNames.Close();
        }

        public void SlowCreateIndex()
        {
            index.Clear();
            index.Fill(new object[0]);

            foreach (PaEntry ent in tableNames.Root.Elements())
                     //.Where(ent => (bool)ent.Field(2).Get() == false))
            {
                if ((bool)ent.Field(2).Get() == false)
                {
                    index.Root.AppendElement(new object[] { ent.offset, ent.Field(0).Get() });

                    var hash = ent.Field(1).Get().GetHashCode();
                    binTreeInd.Add(new object[] { ent.offset, hash });
                }

                //Console.WriteLine("offset на строку в опорной таблице: {0}", ent.offset);
               // Console.WriteLine("Количество узлов " + binTreeInd.Root.Elements().Count<PTypeUnion>(binTreeInd.Root.Elements()));
            }
            index.Flush();

            //Сортировка офсетов по id строк
            index.Root.SortByKey<long>(ob => (long)((object[])ob)[1]);
        }

        public void CreateIndex()
        {
            if (tableNames.Root.Count() == 0) return;

            index.Clear(); index.Fill(new object[0]);
            binTreeInd.Clear(); 
            tableNames.Root.Scan((off, ob) =>
            {
                object[] pair = (object[])ob;
                index.Root.AppendElement(new object[] { off, (long)pair[0] });

                Console.WriteLine("offset на строку в опорной таблице: {0}", off);

                binTreeInd.Add(new object[] { off, 1});

                ///binTreeInd.Add(off);//повторяются оффсеты, возможно баг?!
                return true;
            });
            index.Flush();

            //Сортировка офсетов по id строк
            index.Root.SortByKey<long>(ob => (long)((object[])ob)[1]);
        }

        public void LoadTable(uint port, uint cntport)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            Random rnd = new Random();

            uint portion = port;
            uint countPortions = cntport;

            for (uint i = 0; i < countPortions; i++)
            {
                HashSet<string> hs = new HashSet<string>();

                for (uint j = 0; j < portion; j++)
                {
                    string s = "s" + rnd.Next(10000000);
                    hs.Add(s);
                }
                string[] arr = hs.ToArray();
                Array.Sort<string>(arr);

                sw.Start();
                InsertPortion(arr);
                sw.Stop();
            }
            Console.WriteLine("Загрузка закончена. Время={0}", sw.ElapsedMilliseconds);
        }

        public void InsertPortion(string[] sortedArray)
        {
            //TODO: надо переделать из-за поля deleted
            //throw new Exception("надо переделать");
            Dictionary<string, long> dictionary = index_dictionary_1;

            if (sortedArray.Length == 0) 
                return;
            bool portionIsOver = false;

            int indexName = 0;
            string nameFromPortion = sortedArray[indexName];

            long newCode = tableNames.Root.Count();

            if (System.IO.File.Exists(path + "temp.pac")) 
                System.IO.File.Delete(path + "temp.pac");
            PaCell temp = new PaCell(tp, path + "temp.pac", false);

            temp.Clear();
            temp.Fill(new object[0]);

            int cmp;
            // добавляем прежние и новые элементы в вспомогательную ячейку с сохранением сортировки
            foreach (object[] pair in tableNames.Root.ElementValues())
            {
                string name = (string)pair[1];
                while (!portionIsOver && ((cmp = nameFromPortion.CompareTo(name)) <= 0))
                {
                    if (cmp < 0)
                    {
                        // используем новый код
                        object[] newPair = new object[] { newCode, nameFromPortion, false };
                        temp.Root.AppendElement(newPair);
                        newCode++;
                        dictionary.Add((string)newPair[1], (long)newPair[0]);
                    }
                    else
                    {
                        dictionary.Add((string)pair[1], (long)pair[0]);
                    }
                    ++indexName;
                    if (indexName < sortedArray.Length)
                        nameFromPortion = sortedArray[indexName];
                    else
                        portionIsOver = true;
                }
                temp.Root.AppendElement(pair); // переписывается тот же объект
            }
            // добавляем остальные элементы
            while (indexName < sortedArray.Length)
            {
                nameFromPortion = sortedArray[indexName++];
                object[] newPair = new object[] { newCode, nameFromPortion, false };
                temp.Root.AppendElement(newPair);
                newCode++;
                dictionary.Add((string)newPair[1], (long)newPair[0]);
            }

            temp.Flush();
            temp.Close();
            tableNames.Close();

            System.IO.File.Delete(path + "TableOfNames.pac");
            System.IO.File.Move(path + "temp.pac", path + "TableOfNames.pac");

            tableNames = new PaCell(tp, path + "TableOfNames.pac", false);
        }

        public void Add(string str)
        {
            if (maxSizeDictionary <= index_dictionary_1.Count) 
                MergeIndexes();

            //if (GetIdByString(str) == null)
            {
                long newCode = tableNames.Root.Count();
                long offset = tableNames.Root.AppendElement(new object[] { newCode, str, false });
                
                index_dictionary_1.Add(str, offset);
            }
        }
        
        public void ShowSizeDictionary()
        {
            Console.WriteLine("Размер словаря: "+index_dictionary_1.Count);
        }
        
        public void Remove(string str)
        {
            long offset;
            index_dictionary_1.TryGetValue(str, out offset);

            PaEntry ent = tableNames.Root.Element(0);
            ent.offset = offset;

            ent.Field(2).Set(true);

            index_dictionary_1.Remove(str);
        }

        public void WriteToIndex(Dictionary<string,long> dic)
        {
            lock (treeLocker)
            {
                binTreeInd.Close();
                System.IO.File.Copy(path + "IndexNames.pax", path + "tmp.pax");
                binTreeInd = new BinaryTreeIndex(binTreeType, elementCompare, path + "IndexNames.pax", false);
            }

            binTreeInd_tmp = new BinaryTreeIndex(binTreeType, elementCompare, path + "tmp.pax", false);

            foreach (var pair in dic)
            {
                long offset = pair.Value;
                string name = pair.Key;

                binTreeInd_tmp.Add(new object[] { offset, name.GetHashCode() });
            }

            lock (treeLocker)
            {
                binTreeInd_tmp.Close();
                binTreeInd.Close();
                System.IO.File.Delete(path + "IndexNames.pax");
                System.IO.File.Move(path + "tmp.pax", path + "IndexNames.pax");

                binTreeInd = new BinaryTreeIndex(binTreeType, elementCompare, path + "IndexNames.pax", false);
            }
            
            worker.CancelAsync();
        }

        public void MergeIndexes()
        {
            if (worker.IsBusy) { Console.WriteLine("\nСловарь переполнен"); return; }

            index_dictionary_2 = index_dictionary_1;
            index_dictionary_1 = new Dictionary<string, long>();

            worker.RunWorkerAsync();
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            autoEvent.Reset();
            WriteToIndex(index_dictionary_2);
        }
       
        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            tableNames.Flush();
            autoEvent.Set();
        }

        public void CacheIndex(object toCache)
        {
            ObjectCache cache = MemoryCache.Default;
            var fileContents = cache["indexCache"];
            
            //поиск объекта в кеше

            //если объект не найден
            cache.Set("indexCache", toCache, policy);
        }
    }
}
