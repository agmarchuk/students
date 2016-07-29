using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ORMPolar
{
    public class IndexAttribute: System.Attribute
    {
        public string Name { get; set; }

        public IndexAttribute(){ }

        public IndexAttribute(string name)
        {
            Name = name;
        }
    }

    public class RelationAttribute : System.Attribute
    {
        public string source { get; set; }
        public string target { get; set; }
        public string foreighnField { get; set; }

        public RelationAttribute(string source, string target, string foreighnField)
        {
            this.source = source;
            this.target = target;
            this.foreighnField = foreighnField;
        }

        public RelationAttribute(string target, string foreighnField)
        {
            this.target = target;
            this.foreighnField = foreighnField;
        }
    }

    public class OneToManyAttribute : RelationAttribute
    {
        public OneToManyAttribute(string target, string foreighnField) : base(target, foreighnField) { }
    }

    public class ManyToOneAttribute : RelationAttribute
    {
        public ManyToOneAttribute(string target, string foreighnField) : base(target, foreighnField) { }
    }

    public class OneToOneAttribute : RelationAttribute
    {
        public OneToOneAttribute(string source, string target, string foreighnField) : base(source, target, foreighnField) { }
    }
}
