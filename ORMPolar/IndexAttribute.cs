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

    class RelationAttribute : System.Attribute
    {
        public string sourceField { get; set; }
        public string targetField { get; set; }

        public RelationAttribute(string source, string target)
        {
            this.sourceField = source;
            this.targetField = target;
        }
    }

    class OneToManyAttribute : RelationAttribute
    {
        public OneToManyAttribute(string source, string target) : base(source, target) { }
    }

    class ManyToManyAttribute : RelationAttribute
    {
        public ManyToManyAttribute(string source, string target) : base(source, target) { }
    }

    class OneToOneAttribute : RelationAttribute
    {
        public OneToOneAttribute(string source, string target) : base(source, target) { }
    }
}
