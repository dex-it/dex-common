using System;

namespace Dex.Extensions
{
    public static class IntToTimeSpanExtensions
    {
        public static TimeSpan MilliSeconds(this int seconds)
        {
            return TimeSpan.FromMilliseconds(seconds);
        }

        public static TimeSpan Seconds(this int seconds)
        {
            return TimeSpan.FromSeconds(seconds);
        }

        public static TimeSpan Minutes(this int minutes)
        {
            return TimeSpan.FromMinutes(minutes);
        }

        public static TimeSpan Hours(this int hours)
        {
            return TimeSpan.FromHours(hours);
        }

        public static TimeSpan Days(this int hours)
        {
            return TimeSpan.FromDays(hours);
        }
    }
}