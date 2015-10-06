using System;


namespace TestPlatform.SpeedTests
{
    public class TestORMPolarDB:IPerformanceTest
    {
        
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        public long CreateDB()
        {
            sw.Start();

            //TODO: одинаковые структуры

            sw.Stop();
            return sw.ElapsedMilliseconds;
        }

        public long FindAll()
        {
            throw new NotImplementedException();
        }

        public long FindFirst()
        {
            throw new NotImplementedException();
        }

        public long Reading()
        {
            throw new NotImplementedException();
        }
    }
}
