using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TableOfNames
{
    class TestDataGenerator
    {
        private UInt32 count;
        private char [] mas = {'a','b','c','d','e','f'};

        public TestDataGenerator(UInt32 count)
        {
            this.count = count;
        }

        public IEnumerable<string> Generate()
        {
            Random rnd = new Random();
            StringBuilder str=new StringBuilder();
            UInt32 N=0;

            for (UInt32 i = 0; i < count; ++i)
            {
                str.Clear();
                N=(UInt16)(rnd.Next(10)+1);

                for (UInt16 j = 0; j < N; ++j)
                    str.Append(mas[rnd.Next(mas.Length)]);

                yield return str.ToString();
            }
        }
    }
}
