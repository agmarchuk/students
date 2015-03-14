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

    class SQLDB<TypeDB> :IDisposable where TypeDB : ISQLDB
    {
        private TypeDB dbsql;

        public SQLDB(TypeDB db)
        {
            this.dbsql = db;
        }

        public void InitSQLDB()
        {
            dbsql.PrepareToLoad();
        }

        public void ClearSQLDB()
        {
            dbsql.PrepareToLoad();
        }

        public void DeleteSQLDB()
        {
            dbsql.Delete();
        }

        public void Dispose()
        {
            dbsql.Dispose();
        }

        public void LoadSQL(int NumberData)
        {
            dbsql.LoadElementFlow(NumberData);
        }
        public void CreateIndexSQL()
        {
            dbsql.MakeIndexes();
        }
        public bool SearchByStringSQLFirst(string srch)
        {
            return dbsql.SearchByNameFirst(srch, "TestStrings");
        }
        public bool SearchByIDSQLFirst(int id)
        {
            return dbsql.SelectByIdFirst(id, "TestStrings");
        }
        public void SearchByStringSQLAll(string srch)
        {
            dbsql.SearchByNameAll(srch, "TestStrings");
        }
        public void SearchByIDSQLAll(int id)
        {
            dbsql.SelectByIdAll(id, "TestStrings");
        }
    }
}
