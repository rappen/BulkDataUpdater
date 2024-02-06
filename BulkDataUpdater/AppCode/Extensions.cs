using System;

namespace Cinteros.XTB.BulkDataUpdater.AppCode
{
    public static class Extensions
    {
        public static string SmartToString(this TimeSpan span)
        {
            if (span.TotalDays >= 1)
            {
                return $"{span.TotalDays:0}d {span.Hours:00}:{span.Minutes:00} (d hh:mm)";
            }
            if (span.TotalHours >= 1)
            {
                return $"{span.Hours:0}:{span.Minutes:00} (hh:mm)";
            }
            if (span.TotalMinutes >= 1)
            {
                return $"{span.Minutes:0}:{span.Seconds:00} (mm:ss)";
            }
            return $"{span.Seconds:0}.{span:fff} (ss.fff)";
        }
    }
}