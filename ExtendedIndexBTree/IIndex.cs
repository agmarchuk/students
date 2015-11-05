using PolarDB;
using System;
using System.Collections.Generic;

namespace ExtendedIndexBTree
{
    public interface IIndex: IDisposable
    {
        IEnumerable<long> FindAll(object key);
        long FindFirst(object key);
        void AppendElement(object key);
        void DeleteElement(object key);

        PxCell IndexCell { get; }
    }
}