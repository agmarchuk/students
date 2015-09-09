using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ORMPolar
{
    class IndexAttribute: System.Attribute
    {
        public string Name { get; set; }

        public IndexAttribute(){ }

        public IndexAttribute(string name)
        {
            Name = name;
        }
    }
}
