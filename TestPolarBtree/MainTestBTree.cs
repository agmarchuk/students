using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;
using System.IO;

namespace TestPolarBtree
{
    class MainTest
    {
        static void Main(string[] args)
        {
            const string path = @"../../../Databases/";

            Func<object, object, int> compareName = (object ob1, object ob2) =>
            {
                return String.Compare(ob1.ToString(), ob2.ToString(), StringComparison.Ordinal);//вернётся: -1,0,1
            };

            BTree btree = new BTree(compareName, path + "btree.pcx");
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            btree.Clear();
            //Random rnd = new Random();
            //for (int i = 0; i < 4000; i++)
            //    btree.Add(rnd.Next(4000));

            btree.Add(111);
            btree.Add(222);
            btree.Add(444);
            btree.Add(555);
            btree.Add(666);

            StreamWriter swriter = File.CreateText("../../Results/result.txt");
            var res = btree.Root.GetValue();
            swriter.WriteLine(res.Type.Interpret(res.Value));
            swriter.Close();


            //проверка вставки дочернего узла
            //var res = btree.TestFillTree();
            //Console.WriteLine(res.Type.Interpret(res.Value));

            //object[] ch = new object[]{
            //    1,
            //    new object[]{
            //        1,
            //        new object[] { 987L},
            //        true,
            //        new object[0]
            //        }
            //};
            //btree.InsertChild(btree.Root, ch, 2);
            //var res2 = btree.Root.GetValue();
            //Console.WriteLine(res2.Type.Interpret(res2.Value));


            
            //btree.TestFillTree();

            //btree.Add(330L);


            //Поиск ключа в дереве
            //long key = 666L;
            //Console.WriteLine(btree.Search(btree.Root, key).ToString());

            //Console.WriteLine("Time: {0}", sw.ElapsedMilliseconds);
            Console.ReadKey();
        }
    }
}
