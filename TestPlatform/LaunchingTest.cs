using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestPlatform.SpeedTests;

namespace TestPlatform
{
    class LaunchingTest
    {
        public static void Main(string[] arg)
        {
            Random rnd = new Random();
            int N = 1000000;

            TestORMPolarDB polar = new TestORMPolarDB();
            Console.WriteLine("Создание БД PolarDB c {1} записями. Время = {0}мс", polar.CreateDB(N), N);

            int r = rnd.Next(N);
            Console.WriteLine("Поиск первого строкового ключа {1} в БД PolarDB. Время = {0}мс", polar.FindFirst("title","book"+r),r);
            Console.WriteLine("Поиск первого целого ключа {1} в БД PolarDB. Время = {0}мс", polar.FindFirst("id_author", r), r);

            r = rnd.Next(N);
            int count;
            Console.WriteLine("Поиск всех ключей {1} в БД PolarDB. Нашлось {2} ключей. Время = {0}мс", polar.FindAll("id_author", r, out count), r, count);

            long time = 0, time1 = 0, time2 = 0;
            for(int i=0; i<1000; ++i)
            {
                r = rnd.Next(N);
                time  += polar.FindFirst("title", "book" + r);
                time1 += polar.FindFirst("id_author", r);
                time2 += polar.FindAll("title", "book" + r, out count);
            }
            
            Console.WriteLine("Поиск первого строкового ключа в БД PolarDB 1000 раз. Время = {0}мс", time);
            Console.WriteLine("Поиск первого целого ключа в БД PolarDB 1000 раз. Время = {0}мс", time1);
            Console.WriteLine("Поиск всех ключей в БД PolarDB 1000 раз. Время = {0}мс", time2);


            Console.ReadKey();
            polar.DeleteDB();
        }

    }
}
