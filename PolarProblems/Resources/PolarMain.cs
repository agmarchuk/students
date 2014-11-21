using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;

namespace PolarProblems
{
    class PolarMain
    {
       
        public string Load()
        {
            string path = "../../../Databases/";

            PType tp_bd = new PTypeRecord(
                new NamedType("name", new PType(PTypeEnumeration.sstring)),
                new NamedType("birthdate", new PType(PTypeEnumeration.longinteger))
            );
            
            //Таблица с именами и датами рождения
            PaCell table_bd = new PaCell(tp_bd, path + "birthdates.pac", false);

            //if (table_bd.IsEmpty)
            {
                table_bd.Clear();
                table_bd.Fill(new object[0]);
            }


            return "OK!";
        }
    }
}
