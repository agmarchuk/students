using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace TableOfNames
{
    class TestingTableOfNames
    {
        //путь до базы
        private const string path = "../../../Databases/";

        //private void TestGenerator()
        //{
        //    //Проверка генерации строк
        //    TestDataGenerator tdg = new TestDataGenerator(10);

        //    foreach( string str in tdg.Generate() )
        //    {
        //        Console.WriteLine(str);
        //    }
        //}

        //private static string GetStringById(PaCell baseTable,PaCell indexTable, long id)
        //{
        //    return (string)baseTable.Root.Element(id).Get();
        //}

        //private static PaEntry TestSearch(PaCell baseTable, BinaryTreeIndex cell, object ob)
        //{
        //    PaEntry entry = baseTable.Root.Element(0);
        //    bool Founded = false;

        //    PxEntry found = cell.BinarySearch(pe =>
        //    {
        //        entry.offset = (long)pe.Get();

        //        object get = entry.Get();

        //        int rezcmp = cell.elementCompare(ob, get);
        //        if (rezcmp == 0) Founded = true;

        //        return rezcmp;
        //    });
        //    if (Founded == true) entry.offset = (long)found.Get();
        //    else entry.offset = 0;

        //    return entry;
        //}

        //private static long GetIdByString(PaCell baseTable, BinaryTreeIndex indexTable, string srchStr)
        //{
        //    PaEntry entt = baseTable.Root.Element(0);

        //    entt = TestSearch(baseTable, indexTable, srchStr);//вернется офсет на первый найденный элемент в БД

        //    //TODO: исправить костыль
        //    if (entt.offset != 0) return (long)((entt.offset - baseTable.Root.Element(0).offset) / 8 - 1);
        //    else return 0L;
        //}

        static void Main(string[] args)
        {
            TableOfNames ton = new TableOfNames(path);

            UInt16 portion = 10;
            TestDataGenerator tdg = new TestDataGenerator(portion);

            for (uint i = 0; i < 10; i++)
            {
                string[] arr = tdg.Generate().ToArray();
                Array.Sort<string>(arr);
                ton.InsertPortion(arr);
            }


            //ton.CreateIndex();

            Console.ReadKey();
        }
    }
}
