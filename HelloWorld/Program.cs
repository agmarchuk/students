using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace HelloWorld
{
    public class Program
    {
        public static void Main()
        {
            Console.WriteLine("Hello World!");
            PType tp = new PTypeRecord(
                new NamedType("text", new PType(PTypeEnumeration.sstring)),
                new NamedType("longintdate", new PType(PTypeEnumeration.longinteger)));
            string path = "../../../Databases/";
            PaCell cell = new PaCell(tp, path + "cell.pac", false);
            //cell.Clear();
            if (cell.IsEmpty)
                cell.Fill(new object[] { "Hello World!", DateTime.Now.ToBinary() });
            else
            {
                object value = cell.Root.Get();
                Console.WriteLine("Результат чтения из базы данных: {0}", tp.Interpret(value));
                Console.WriteLine("Дата: {0}", DateTime.FromBinary(((long)((object[])value)[1])).ToString());
            }

        }
    }
}
