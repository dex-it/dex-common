using System;

namespace Dex.Types
{
    public class RangeValues<T> where T : IComparable<T>
    {
        public T Start { get; }
        public T End { get; }

        public RangeValues(T start, T end)
        {
            if (start == null) throw new ArgumentNullException(nameof(start));
            if (end == null) throw new ArgumentNullException(nameof(end));

            if (start.CompareTo(end) > 0)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "start > end");
            }

            Start = start;
            End = end;
        }

        public bool Overlap(RangeValues<T> item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            if (Start.CompareTo(item.Start) == 0)
            {
                return true;
            }

            if (Start.CompareTo(item.Start) > 0)
            {
                return Start.CompareTo(item.End) < 0;
            }

            return item.Start.CompareTo(End) < 0;
        }

        public bool Contains(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            return item.CompareTo(Start) > 0 && item.CompareTo(End) < 0;
        }
    }
}