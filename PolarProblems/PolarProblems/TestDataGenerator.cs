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
            for (int i = 0; i < npersons; ++i)
            {
                yield return new XElement("birthdates", new XAttribute("id", i),
                            new XElement("name", "Вася" + rnd.Next(npersons)),
                            new XElement("birth", (long)( 20 + rnd.Next(80) ) )
                            );
            }
        }
    }
}
