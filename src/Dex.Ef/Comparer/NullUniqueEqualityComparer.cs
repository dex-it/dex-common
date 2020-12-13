using System.Collections.Generic;

namespace Dex.Ef.Comparer
{
    internal class NullUniqueEqualityComparer<T> : IEqualityComparer<T>
    {
        public static NullUniqueEqualityComparer<T> Get() => new NullUniqueEqualityComparer<T>();
        
        private NullUniqueEqualityComparer()
        {
        }

        public bool Equals(T x, T y)
        {
            if (x == null || y == null) return false;
            return x.Equals(y);
        }

        public int GetHashCode(T obj)
        {
            return obj == null ? 0 : obj.GetHashCode();
        }
    }
}