using System;


namespace TestPlatform
{
    public class Test
    {

    }

    public class CreateDBTest
    {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

        public long CreateDB()
        {
            sw.Start();



            sw.Stop();
            return sw.ElapsedMilliseconds;
        }
    }
}
