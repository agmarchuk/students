using PolarDB;
using System;
using System.Collections.Generic;

namespace Common
{
    public interface IIndex: IDisposable
    {
        IEnumerable<long> FindAll(object key);
        long FindFirst(object key);
        void AppendElement(object key);
        void DeleteElement(object key);

        PCell IndexCell { get; }
    }
}