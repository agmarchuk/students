using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace TestPolarInOtherOS
{
    class Testing
    {
        static void Main(string[] args)
        {
            string path = @"../../../../Databases/";
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            Random rnd = new Random();

            PType tp_bd = new PTypeRecord(
                new NamedType("strings", new PType(PTypeEnumeration.sstring)),
                new NamedType("id", new PType(PTypeEnumeration.longinteger))
            );

            PaCell table_bd = new PaCell(new PTypeSequence(tp_bd), path + "DataTable.pac", false);
            table_bd.Clear();
            table_bd.Fill(new object[0]);

            int maxCount = 1000000;
            Console.WriteLine("Заполняем таблицу...");
            sw.Start();
                for (int i = 0; i < maxCount; ++i )
                {
                    string s = "s" + rnd.Next(maxCount*10);
                    table_bd.Root.AppendElement(new object[]{s,(long)i});
                }
                table_bd.Flush();
            sw.Stop();
            Console.WriteLine("Кол-во данных {1}, время: {0}мс", sw.ElapsedMilliseconds, maxCount);

            Console.WriteLine("Последовательно пробегаем по массиву данных...");
            sw.Restart();
                foreach (var v in table_bd.Root.ElementValues()) ;
            sw.Stop();
            Console.WriteLine("Время: {0}мс", sw.ElapsedMilliseconds);

            Console.WriteLine("Случайно 1000 раз прыгаем по массиву данных...");
            sw.Restart();
                for(int i=0; i<1000; ++i)
                {
                    table_bd.Root.Element(rnd.Next(0, 1000)).Get();
                }
            sw.Stop();
            Console.WriteLine("Время: {0}мс", sw.ElapsedMilliseconds);

            Console.WriteLine("Сортировка массива...");
            int [] mas = new int[maxCount];
            for (int i = 0; i < maxCount;++i)
            {
                mas[i] = rnd.Next(0, maxCount);
            }
            sw.Restart();
                Array.Sort(mas);
            sw.Stop();
            Console.WriteLine("Время: {0}мс", sw.ElapsedMilliseconds);
            
            Console.ReadKey();

            table_bd.Close();
            System.IO.File.Delete(path+"DataTable.pac");
        }
    }
}
