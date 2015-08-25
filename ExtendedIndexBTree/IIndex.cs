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
        void OnAppendElement(object key);
        void OnDeleteElement(object key);
        void DropIndex();

        PxCell IndexCell { get; }
    }
}