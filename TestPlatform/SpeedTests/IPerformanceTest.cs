using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestPlatform.SpeedTests
{
    public interface IPerformanceTest
    {
        long CreateDB(int size);
        void DeleteDB();

        //long FindFirst(string fieldName, object obj);
        long FindFirst(int repeats, string fieldName);
        //long FindAll(string fieldName, object obj, out int count);
        long FindAll(int repeats, string fieldName);
    }
}
