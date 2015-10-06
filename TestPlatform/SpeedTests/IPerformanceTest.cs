using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestPlatform.SpeedTests
{
    public interface IPerformanceTest
    {
        long CreateDB();
        long FindFirst();
        long FindAll();
        long Reading();
    }
}
