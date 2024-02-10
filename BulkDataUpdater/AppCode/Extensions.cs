using System;
using System.Xml;

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
            if (span.TotalSeconds >= 1)
            {
                return $"{span.Seconds:0}.{span:fff} secs";
            }
            return $"{span.TotalMilliseconds:0} ms";
        }

        internal static string AttributeValue(this XmlNode node, string key)
        {
            if (node != null && node.Attributes != null && node.Attributes[key] is XmlAttribute attr)
            {
                return attr.Value;
            }
            return string.Empty;
        }
    }
}