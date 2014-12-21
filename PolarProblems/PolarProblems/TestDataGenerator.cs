using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;


namespace PolarProblems
{
    class TestDataGenerator
    {
        private int npersons;
        private int seed;

        public TestDataGenerator(int npersons, int seed)
        {
            this.npersons = npersons;
            this.seed = seed;
        }

        public IEnumerable<XElement> Generate()
        {
            Random rnd = new Random(seed);
            DateTime dt = new DateTime(2000, 10, 12);
            for (int i = 0; i < npersons; ++i)
            {
                yield return new XElement("birthdates", new XAttribute("id", i),
                            new XElement("name", "Вася" + rnd.Next(npersons)),
                            new XElement("birth", dt.AddDays(rnd.Next(5000)).ToBinary())
                            );
            }
        }

        public struct Birthdates
        {
            public string name;
            public long birthdate;
        }

        public IEnumerable<Birthdates> Generate2()
        {
            Random rnd = new Random(seed);
            DateTime dt = new DateTime(2000, 10, 12);
            Birthdates bds;
           
            for (int i = 0; i < npersons; ++i)
            {
                bds.name = "Вася" + rnd.Next(npersons);
                bds.birthdate = dt.AddDays(rnd.Next(5000)).ToBinary();
                yield return bds;
            }
        }
    }
}
