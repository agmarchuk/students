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
            string path = @"../../../Databases/";

            Func<object, object, int> compare_name = (object ob1, object ob2) =>
            {
                return String.Compare(ob1.ToString(), ob2.ToString(), StringComparison.Ordinal);//вернётся: -1,0,1
            };

            BTree btree = new BTree(compare_name, path + "btree.pcx");
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            btree.Clear();
            //Random rnd = new Random();
            //for (int i = 0; i < 4000; i++)
            //    btree.Add(rnd.Next(4000));

            /*btree.Add(111);
            btree.Add(222);
            btree.Add(444);
            btree.Add(555);
            btree.Add(666);*/

            StreamWriter swriter = File.CreateText("../../Results/result.txt");

            var res = btree.TestFillTree();
            swriter.WriteLine(res.Type.Interpret(res.Value));
            swriter.Close();


            Console.WriteLine(btree.Search(btree.Root, 333L).ToString());
            //var res3 = btree.Root.GetValue();
            //Console.WriteLine(res3.Type.Interpret(res3.Value));
            //Console.WriteLine("Time: {0}", sw.ElapsedMilliseconds);
            Console.ReadKey();
        }
    }
}
