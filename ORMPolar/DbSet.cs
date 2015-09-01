using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PolarDB;
using System.Collections;
using System.Reflection;

namespace ORMPolar
{
    class DbSet<TEntity>: IEnumerable<TEntity> where TEntity:new()
    {
        private PaCell _cell;

        public DbSet()
        {
            TEntity entity = new TEntity();
            Type t = entity.GetType();
            DbContext.stables.TryGetValue(entity.GetType(), out _cell);
        }

        IEnumerator<TEntity> IEnumerable<TEntity>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            _cell.Clear();
            _cell.Fill(new object[0]);
        }
        public void Append(TEntity entity)
        {
            var fields = entity.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            
            _cell.Root.AppendElement(
                fields
                    .Select<FieldInfo, object>(fieldInfo => fieldInfo.GetValue(entity))
                    .ToArray<object>()
            );
        }
        public void Flush()
        {
            _cell.Flush();
        }
        public PaCell Get()
        {
            return _cell;
        }
        //public IEnumerable<TEntity> Elements()
        //{
        //    //return _cell.Root.Elements()
        //}
    }
}
