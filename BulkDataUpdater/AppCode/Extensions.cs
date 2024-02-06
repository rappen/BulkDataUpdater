using System;

namespace Cinteros.XTB.BulkDataUpdater.AppCode
{
    public static class Extensions
    {
        public static string SmartToString(this TimeSpan span)
        {
            if (span.TotalDays >= 1)
            {
                return $"{span.TotalDays:0} {span.Hours:00}:{span.Minutes:00} days";
            }
            if (span.TotalHours >= 1)
            {
                return $"{span.Hours:0}:{span.Minutes:00} hrs";
            }
            if (span.TotalMinutes >= 1)
            {
                return $"{span.Minutes:0}:{span.Seconds:00} mins";
            }
            return $"{span.Seconds:0}.{span:fff} secs";
        }
    }
}