using PolarDB;
using System;
using System.Collections.Generic;

namespace ExtendedIndexBTree
{
    public interface IIndex<Tkey>
	{
        Func<object, Tkey> KeyProducer { get; set; }
        IEnumerable<PaEntry> GetAllByKey(Tkey key);
        void Build();
        //long Count();
        void AppendElement(object key);
        void DeleteElement(object key);

        PxCell IndexCell { get; }
    }
}