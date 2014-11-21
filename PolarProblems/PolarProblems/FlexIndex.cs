using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace PolarProblems
{
    public interface IIndex
    {
        // Построение индекса по опорной таблице
        void Load();
        // Расширение индекса при динамическом добавлении входа в опорную таблицу
        void AddEntry(PaEntry ent);
        // Закрытие индекса
        void Close();
    }
    public class FlexIndex<Tkey> : IIndex where Tkey : IComparable
    {
        private PaEntry table;
        private PaCell index_cell;
        private PaCell index_cell_small;
        private Func<PaEntry, Tkey> keyProducer;
        private IComparer<Tkey> comparer;
        public FlexIndex(string indexName, PaEntry table, Func<PaEntry, Tkey> keyProducer, IComparer<Tkey> comparer)
        {
            this.table = table;
            this.keyProducer = keyProducer;
            this.comparer = comparer;
            index_cell = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.longinteger)), indexName + ".pac", false);
            if (index_cell.IsEmpty) index_cell.Fill(new object[0]);
            index_cell_small = new PaCell(new PTypeSequence(new PType(PTypeEnumeration.longinteger)), indexName + "_s.pac", false);
            if (index_cell_small.IsEmpty) index_cell_small.Fill(new object[0]);
        }
        public void Close() { index_cell.Close(); index_cell_small.Close(); }

        public void Load()
        {
            // Маленький массив будет после загрузки пустым
            index_cell_small.Clear();
            index_cell_small.Fill(new object[0]); index_cell_small.Flush();
            index_cell.Clear();
            index_cell.Fill(new object[0]);
            foreach (var rec in table.Elements()) // загрузка всех элементов за исключением уничтоженных
            {
                long offset = rec.offset;
                index_cell.Root.AppendElement(offset);
            }
            index_cell.Flush();
            if (index_cell.Root.Count() == 0) return; // потому что следующая операция не пройдет
            // Сортировать index_cell специальным образом: значение (long) используется как offset ячейки и там прочитывается нулевое поле
            var ptr = table.Element(0);
            index_cell.Root.SortByKey<Tkey>((object v) =>
            {
                ptr.offset = (long)v;
                return keyProducer(ptr);
            }, comparer);
        }
        public void AddEntry(PaEntry ent)
        {
            long offset = ent.offset;
            index_cell_small.Root.AppendElement(offset);
            index_cell_small.Flush();
            var ptr = table.Element(0);
            index_cell_small.Root.SortByKey<object>((object v) =>
            {
                ptr.offset = (long)v;
                return keyProducer(ptr);
            });
        }
        public PaEntry GetFirstByKey(Tkey key)
        {
            if (table.Count() == 0) return PaEntry.Empty;
            var ent = GetFirstFromByKey(index_cell_small, key); // сначала из маленького массива
            if (!ent.IsEmpty) return ent;
            return GetFirstFromByKey(index_cell, key);
        }
        public PaEntry GetFirst(Func<PaEntry, int> elementDepth)
        {
            if (table.Count() == 0) return PaEntry.Empty;
            var ent = GetFirstFrom(index_cell_small, elementDepth); // сначала из маленького массива
            if (!ent.IsEmpty) return ent;
            return GetFirstFrom(index_cell, elementDepth);
        }
        // Использование GetFirst:
        //var qu = iset_index.GetFirst(ent =>
        //{
        //    int v = (int)ent.Get();
        //    return v.CompareTo(sample);
        //});

        public PaEntry GetFirstFromByKey(PaCell i_cell, Tkey key)
        {
            if (table.Count() == 0) return PaEntry.Empty;
            PaEntry entry = table.Element(0);
            var candidate = i_cell.Root.BinarySearchFirst(ent =>
            {
                long off = (long)ent.Get();
                entry.offset = off;
                return ((IComparable)keyProducer(entry)).CompareTo(key);
            });
            if (candidate.IsEmpty) return PaEntry.Empty;
            entry.offset = (long)candidate.Get();
            return entry;
        }
        private PaEntry GetFirstFrom(PaCell i_cell, Func<PaEntry, int> elementDepth)
        {
            PaEntry entry = table.Element(0);
            PaEntry entry2 = table.Element(0); // сделан, потому что может entry во внешнем и внутренниц циклах проинтерферируют?
            var candidate = i_cell.Root.BinarySearchAll(ent =>
            {
                long off = (long)ent.Get();
                entry.offset = off;
                return elementDepth(entry);
            }) // здесь мы имеем множество найденных входов в ячейку i_cell
            .Select(ent =>
            {
                entry2.offset = (long)ent.Get(); // вход в запись таблицы
                return entry2;
            }) // множество входов, удовлетворяющих условиям
            .Where(t_ent => !(bool)t_ent.Field(0).Get()) // остаются только неуничтоженные
            .DefaultIfEmpty(PaEntry.Empty) // а вдруг не останется ни одного, тогда - пустышка
            .First(); // обязательно есть хотя бы пустышка
            return candidate;
        }

        public IEnumerable<PaEntry> GetAllByKey(Tkey key)
        {
            if (table.Count() == 0) return Enumerable.Empty<PaEntry>();
            var ents = GetAllFromByKey(index_cell, key)
                .Concat(GetAllFromByKey(index_cell_small, key));
            return ents;
        }
        // Возвращает множество входов в записи опорной таблицы, удовлетворяющие elementDepth == 0
        public IEnumerable<PaEntry> GetAll(Func<PaEntry, int> elementDepth)
        {
            if (table.Count() == 0) return Enumerable.Empty<PaEntry>();
            return GetAllFrom(index_cell, elementDepth).Concat(GetAllFrom(index_cell_small, elementDepth));
        }
        // Использование для поиска в строках русского языка:
        //  string ss = searchstring.ToLower();
        //  var query = GetAll(ent =>
        //  {
        //      string s = (string)ent.Field(1).Get(); // В предположении, что индексируется 1-е поле, которое является текстовым
        //      if (string.Compare(s, 0, ss, 0, ss.Length, StringComparison.OrdinalIgnoreCase) == 0) return 0;
        //      return string.Compare(s, ss, StringComparison.OrdinalIgnoreCase);
        //  });
        private IEnumerable<PaEntry> GetAllFromByKey(PaCell cell, Tkey key)
        {
            PaEntry entry = table.Element(0);
            var query = cell.Root.BinarySearchAll((PaEntry ent) =>
            {
                long off = (long)ent.Get();
                entry.offset = off;
                return ((IComparable)keyProducer(entry)).CompareTo(key);
            })
            .Select(en =>
            {
                entry.offset = (long)en.Get();
                return entry;
            })
            .Where(t_ent => !(bool)t_ent.Field(0).Get()); // остаются только неуничтоженные
            return query;
        }
        private IEnumerable<PaEntry> GetAllFromByKey0(PaCell cell, Tkey key)
        {
            PaEntry entry = table.Element(0);
            Diapason dia = cell.Root.BinarySearchDiapason((PaEntry ent) =>
            {
                long off = (long)ent.Get();
                entry.offset = off;
                return ((IComparable)keyProducer(entry)).CompareTo(key);
            });
            var query = cell.Root.Elements(dia.start, dia.numb)
                .Select(ent =>
                {
                    entry.offset = (long)ent.Get();
                    return entry;
                })
                .Where(t_ent => !(bool)t_ent.Field(0).Get()); // остаются только неуничтоженные
            return query;
        }
        private IEnumerable<PaEntry> GetAllFrom(PaCell cell, Func<PaEntry, int> elementDepth)
        {
            PaEntry entry = table.Element(0);
            Diapason dia = cell.Root.BinarySearchDiapason((PaEntry ent) =>
            {
                long off = (long)ent.Get();
                entry.offset = off;
                return elementDepth(entry);
            });
            var query = cell.Root.Elements(dia.start, dia.numb)
                .Select(ent =>
                {
                    entry.offset = (long)ent.Get();
                    return entry;
                })
                .Where(t_ent => !(bool)t_ent.Field(0).Get()); // остаются только неуничтоженные
            return query;
        }
        public IEnumerable<PaEntry> GetAll()
        {
            if (table.Count() == 0) return Enumerable.Empty<PaEntry>();
            return GetAllFrom(index_cell).Concat(GetAllFrom(index_cell_small));
        }
        private IEnumerable<PaEntry> GetAllFrom(PaCell cell)
        {
            PaEntry entry = table.Element(0);
            var query = cell.Root.Elements()
                .Select(ent =>
                {
                    entry.offset = (long)ent.Get();
                    return entry;
                })
                .Where(t_ent => !(bool)t_ent.Field(0).Get()); // остаются только неуничтоженные
            return query;
        }
    }
}
