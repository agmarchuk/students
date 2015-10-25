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
        public static void RunTestORMInRAM(int N)
        {
            TestORMInRAM inRAM = new TestORMInRAM();

            Console.WriteLine("Создание БД inRAM c {1} записями. Время = {0}мс", inRAM.CreateDB(N), N);
            Console.WriteLine("Поиск первого строкового ключа в inRAM. Время = {0}мс", inRAM.FindFirst(1,"title"));
            Console.WriteLine("Поиск первого целого ключа в БД InRAM. Время = {0}мс", inRAM.FindFirst(1,"id_author"));

            int count;
            Console.WriteLine("Поиск всех целых ключей в БД InRAM. Время = {0}мс", inRAM.FindAll(1,"id_author"));
            Console.WriteLine("Поиск всех строковых ключей в БД InRAM. Время = {0}мс", inRAM.FindAll(1,"title"));


            Console.WriteLine("Поиск первого строкового ключа в БД InRAM 1000 раз. Время = {0}мс", inRAM.FindFirst(1000, "title"));
            Console.WriteLine("Поиск первого целого ключа в БД InRAM 1000 раз. Время = {0}мс", inRAM.FindFirst(1000, "id_author"));
            Console.WriteLine("Поиск всех целых ключей в БД InRAM 1000 раз. Время = {0}мс", inRAM.FindAll(1000, "id_author"));
            Console.WriteLine("Поиск всех строковых ключей в БД InRAM 1000 раз. Время = {0}мс", inRAM.FindAll(1000, "title"));
        }

        public static void RunTestORMEntityFramework(int N)
        {
            TestORMEntityFramework ormEF = new TestORMEntityFramework();

            Console.WriteLine("Создание БД ORMEF c {1} записями. Время = {0}мс", ormEF.CreateDB(N), N);
            Console.WriteLine("Поиск первого строкового ключа в ORMEF. Время = {0}мс", ormEF.FindFirst(1, "title"));
            Console.WriteLine("Поиск первого целого ключа в БД ORMEF. Время = {0}мс", ormEF.FindFirst(1, "id_author"));
            Console.WriteLine("Поиск всех целых ключей в БД ORMEF. Время = {0}мс", ormEF.FindAll(1, "id_author"));
            Console.WriteLine("Поиск всех строковых ключей в БД ORMEF. Время = {0}мс", ormEF.FindAll(1, "title"));

            Console.WriteLine("Поиск первого строкового ключа в БД ORMEF 1000 раз. Время = {0}мс", ormEF.FindFirst(1000, "title"));
            Console.WriteLine("Поиск первого целого ключа в БД ORMEF 1000 раз. Время = {0}мс", ormEF.FindFirst(1000, "id_author"));
            Console.WriteLine("Поиск всех целых ключей в БД ORMEF 1000 раз. Время = {0}мс", ormEF.FindAll(1000, "id_author"));
            Console.WriteLine("Поиск всех строковых ключей в БД ORMEF 1000 раз. Время = {0}мс", ormEF.FindAll(1000, "title"));

            ormEF.DeleteDB();
        }

        public static void RunTestORMPolarDB(int N)
        {
            Random rnd = new Random();

            TestORMPolarDB polar = new TestORMPolarDB();
            Console.WriteLine("Создание БД PolarDB c {1} записями. Время = {0}мс", polar.CreateDB(N), N);

            int r = rnd.Next(N);
            Console.WriteLine("Поиск первого строкового ключа {1} в БД PolarDB. Время = {0}мс", polar.FindFirst("title", "book" + r), "book" + r);
            Console.WriteLine("Поиск первого целого ключа {1} в БД PolarDB. Время = {0}мс", polar.FindFirst("id_author", r), r);

            r = rnd.Next(N);
            int count;
            Console.WriteLine("Поиск всех целых ключей {1} в БД PolarDB. Нашлось {2} ключей. Время = {0}мс", polar.FindAll("id_author", r, out count), r, count);
            Console.WriteLine("Поиск всех строковых ключей {1} в БД PolarDB. Нашлось {2} ключей. Время = {0}мс", polar.FindAll("title", "book" + r, out count), "book" + r, count);

            Console.WriteLine("Поиск первого строкового ключа в БД PolarDB 1000 раз. Время = {0}мс", polar.FindFirst(1000, "title"));
            Console.WriteLine("Поиск первого целого ключа в БД PolarDB 1000 раз. Время = {0}мс", polar.FindFirst(1000, "id_author"));
            Console.WriteLine("Поиск всех целых ключей в БД PolarDB 1000 раз. Время = {0}мс", polar.FindAll(1000, "id_author"));
            Console.WriteLine("Поиск всех строковых ключей в БД PolarDB 1000 раз. Время = {0}мс", polar.FindAll(1000, "title"));

            polar.DeleteDB();
        }

        public static void RunTestORMNHiberante(int N)
        {
            TestORMNHibernate hib = new TestORMNHibernate();
            Console.WriteLine("Создание БД NHibernate c {1} записями. Время = {0}мс", hib.CreateDB(N), hib.GetCountBooks());

            Console.WriteLine("Поиск первого строкового ключа в NHibernate. Время = {0}мс", hib.FindFirst(1, "title"));
            Console.WriteLine("Поиск первого целого ключа в БД NHibernate. Время = {0}мс", hib.FindFirst(1, "id_author"));
            Console.WriteLine("Поиск всех целых ключей в БД NHibernate. Время = {0}мс", hib.FindAll(1, "id_author"));
            Console.WriteLine("Поиск всех строковых ключей в БД NHibernate. Время = {0}мс", hib.FindAll(1, "title"));

            Console.WriteLine("Поиск первого строкового ключа в БД NHibernate 1000 раз. Время = {0}мс", hib.FindFirst(1000, "title"));
            Console.WriteLine("Поиск первого целого ключа в БД NHibernate 1000 раз. Время = {0}мс", hib.FindFirst(1000, "id_author"));
            Console.WriteLine("Поиск всех целых ключей в БД NHibernate 1000 раз. Время = {0}мс", hib.FindAll(1000, "id_author"));
            Console.WriteLine("Поиск всех строковых ключей в БД NHibernate 1000 раз. Время = {0}мс", hib.FindAll(1000, "title"));

            hib.DeleteDB();
        }

        public static void Main(string[] arg)
        {
            int N = 1000000;

            //RunTestORMPolarDB(N);
            //Console.WriteLine();
            //RunTestORMInRAM(N);
            //Console.WriteLine();
            //RunTestORMEntityFramework(N);
            //Console.WriteLine();
            RunTestORMNHiberante(N);

            Console.ReadKey();
        }

    }
}
