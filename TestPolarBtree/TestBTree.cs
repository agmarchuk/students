using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;
using System.IO;

namespace TestPolarBtree
{
    class TestBTree
    {
        static void Main(string[] args)
        {
            const string path = @"../../../Databases/";

            Func<object, object, int> compareName = (object ob1, object ob2) =>
            {
                return String.Compare(ob1.ToString(), ob2.ToString(), StringComparison.Ordinal);//вернётся: -1,0,1
            };

            BTree btree = new BTree(50, compareName, path + "btree.pax");

            btree.Clear();

            LoadTree(btree, 1000);

            //btree.WriteTreeInFile("../../Results/Tree.txt");

            SearchKey(btree, 1000);
            SearchRandomKey1000(btree);

            btree.Close();
            File.Delete(path + "btree.pax");
            Console.ReadKey();
        }

        private static void LoadTree(BTree btree, int sizeFlow)
        {
            Console.WriteLine("Добавление данных в дерево...");
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            //Рандомное заполнение B-дерева
            Random rnd = new Random();
            for (int i = 0; i < sizeFlow; i++)
                btree.Add(rnd.Next(sizeFlow));

            //Последовательное заполнение натуральными числами
            //for (int i = 1; i <= sizeFlow; i++)
            //{
            //    btree.Add(i);
            //}
            sw.Stop();
            Console.WriteLine("Время добавления: {0} мс", sw.ElapsedMilliseconds);
        }

        private static void SearchKey(BTree btree, long key)
        {
            Console.Write("Поиск ключа {0}",key);
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            if (btree.Search(btree.Root, key)) Console.WriteLine(" - ключ найден"); 
                else Console.WriteLine(" - ключ не найден");

            sw.Stop();
            Console.WriteLine("Время поиска : {0} мс", sw.ElapsedMilliseconds);
        }

        private static void SearchRandomKey1000(BTree btree)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            Random rnd = new Random();
            sw.Start();

            for (int i = 0; i < 1000; ++i)
                btree.Search(btree.Root, rnd.Next(1000)); 

            sw.Stop();
            Console.WriteLine("\nВремя рандомного поиска 1000раз: {0} мс", sw.ElapsedMilliseconds);
        }
    }
}
