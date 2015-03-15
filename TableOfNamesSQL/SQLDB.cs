using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TableOfNamesSQL
{
    public interface ISQLDB
    {
        void PrepareToLoad();
        void LoadElementFlow(int N);
        void MakeIndexes();
        void Count(string table);
        bool SearchByNameFirst(string srch, string table);
        bool SelectByIdFirst(int id, string table);
        void SearchByNameAll(string srch, string table);
        void SelectByIdAll(int id, string table);
        void Delete();
        void Dispose();
    }
}
