using System;

namespace Dex.Types
{
    public class DateTimeRangeValues : RangeValues<DateTime>
    {
        public DateTimeRangeValues(DateTime start, DateTime end)
            : base(start, end)
        {
        }

        public TimeSpan Length => End - Start;

        public TimeSpan Intersect(DateTimeRangeValues item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            if (Start < item.Start && End > item.End)
            {
                return item.Length;
            }

            if (Start > item.Start && End < item.End)
            {
                return Length;
            }

            if (Overlap(item))
            {
                if (Contains(item.Start))
                {
                    return End - item.Start;
                }

                // End contains
                return item.End - Start;
            }

            return TimeSpan.Zero;
        }
    }
}