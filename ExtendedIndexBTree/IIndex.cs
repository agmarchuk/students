using PolarDB;
using System;
using System.Collections.Generic;

namespace ExtendedIndexBTree
{
    public interface IIndex
	{
        //Func<object, object> KeyProducer { get; set; }
        IEnumerable<long> FindAll(object key);
        long FindFirst(object key);
        //void Build();
        //long Count();
        void AppendElement(object key);
        void DeleteElement(object key);

        PxCell IndexCell { get; }
    }
}