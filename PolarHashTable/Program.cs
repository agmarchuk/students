using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace PolarHashTable
{
    class Program
    {
        private const string path = "../../../Databases/";
        
        static bool CheckUniqElementsInSet(HashSet<ulong> seq, uint expectedSizeSeq)
        {
            return (seq.Count == expectedSizeSeq) ? true : false;
        }

        static void TestHashFunctions(uint tableSize, string hashName = "PJW")
        {
            string[] hashNames = { "PJW", "ELF", "Additive", "Xor" };

            if ((tableSize == 0) || (!hashNames.Contains(hashName))) { Console.WriteLine("Переданы неверные параметры"); return; }

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            HashFunctions hf = new HashFunctions();
            HashSet<ulong> sequence = new HashSet<ulong>();

            uint[] rand8 = new uint[tableSize];
            Random rnd = new Random();

            for (int i = 0; i < rand8.Length; ++i)
            {
                rand8[i] = (uint)rnd.Next((int)tableSize);
            }

            sw.Reset();
            for (uint i = 0; i < tableSize; ++i)
            {
                string temp = i.ToString();
                ulong hash = ulong.MinValue;
                if (hashName == "PJW")
                {
                    sw.Start();
                        hash = hf.GetHashPJW(temp);
                    sw.Stop();
                }
                else if (hashName == "ELF")
                {
                    sw.Start();
                        hash = hf.GetHashELF(temp);
                    sw.Stop();
                }
                else if (hashName == "Additive")
                {
                    sw.Start();
                        hash = hf.GetHashAdd(temp);
                    sw.Stop();
                }
                else if (hashName == "Xor")
                {
                    sw.Start();
                        hash = hf.GetHashXor(temp, rand8);
                    sw.Stop();
                }
                sequence.Add(hash);
            }

            var answer = CheckUniqElementsInSet(sequence, tableSize) ? "да" : "нет";
            Console.WriteLine("Тест хеша \"{0}\":", hashName);
            Console.WriteLine("Все {2} элементов уникальны - {0}, время генерации всех хешей - {1} мс", answer, sw.ElapsedMilliseconds, tableSize);
            if (tableSize - sequence.Count != 0)
                Console.WriteLine("Количество неуникальных элементов {0}", tableSize - sequence.Count);
            Console.WriteLine();
        }

        static void TestHTIndex()
        {
            PType type = new PTypeRecord(
                new NamedType("offset", new PType(PTypeEnumeration.longinteger)),//указатель на определенный лист в листе оффсетов на ключи опорной таблицы
                new NamedType("hash", new PType(PTypeEnumeration.integer))
            );
            //HTIndex ht = new HTIndex(type, path);
        }

        static void Main(string[] args)
        {
            uint N = 10000000;
            TestHashFunctions(N, "PJW");
            TestHashFunctions(N, "ELF");
            TestHashFunctions(N, "Additive");
            TestHashFunctions(N, "Xor");
            //TestHTIndex();

            Console.ReadKey();
        }
    }
}
