using System;
using System.Globalization;
using System.Linq;
using InfluxDB.Net.Models;
using InfluxDB.Net.Contracts;
using InfluxDB.Net.Infrastructure.Validation;
using InfluxDB.Net.Helpers;

namespace InfluxDB.Net.Infrastructure.Formatters
{
    internal class FormatterV09x : IFormatter
    {
        public string PointToString(Point point)
        {
            Validate.NotNullOrEmpty(point.Measurement, "name");
            Validate.NotNull(point.Tags, "tags");
            Validate.NotNull(point.Fields, "fields");

            var tags = string.Join(",", point.Tags.Select(t => string.Join("=", t.Key, EscapeTagValue(t.Value.ToString()))));
            var fields = string.Join(",", point.Fields.Select(t => Format(t.Key, t.Value)));

            var key = string.IsNullOrEmpty(tags) ? Escape(point.Measurement) : string.Join(",", Escape(point.Measurement), tags);
            var ts = point.Timestamp.HasValue ? point.Timestamp.Value.ToUnixTime().ToString() : string.Empty;

            var result = string.Format(GetLineTemplate(), key, fields, ts);

            return result;
        }

        protected virtual string Format(string key, object value)
        {
            Validate.NotNullOrEmpty(key, "key");
            Validate.NotNull(value, "value");

            // Format and escape the values
            var result = value.ToString();

            // surround strings with quotes
            if (value.GetType() == typeof(string))
            {
                result = Quote(Escape(value.ToString()));
            }
            // api needs lowercase booleans
            else if (value.GetType() == typeof(bool))
            {
                result = value.ToString().ToLower();
            }
            // InfluxDb does not support a datetime type for fields or tags
            // convert datetime to unix long
            else if (value.GetType() == typeof(DateTime))
            {
                result = ((DateTime)value).ToUnixTime().ToString();
            }
            // For cultures using other decimal characters than '.'
            else if (value.GetType() == typeof(decimal))
            {
                result = ((decimal)value).ToString("0.0###################", CultureInfo.InvariantCulture);
            }
            else if (value.GetType() == typeof(float))
            {
                result = ((float)value).ToString("0.0###################", CultureInfo.InvariantCulture);
            }
            else if (value.GetType() == typeof(double))
            {
                result = ((double)value).ToString("0.0###################", CultureInfo.InvariantCulture);
            }
            else if (value.GetType() == typeof(long) || value.GetType() == typeof(int))
            {
                result = ToInt(result);
            }

            return string.Join("=", Escape(key), result);
        }

        protected virtual string ToInt(string result)
        {
            return result + "i";
        }

        public string GetLineTemplate()
        {
            return "{0} {1} {2}"; // [key] [fields] [timestamp]
        }

        protected virtual string Escape(string value)
        {
            Validate.NotNull(value, "value");

            var result = value
                // literal backslash escaping is broken
                // https://github.com/influxdb/influxdb/issues/3070
                //.Replace(@"\", @"\\")
                .Replace(@"""", @"\""")
                .Replace(@" ", @"\ ")
                .Replace(@"=", @"\=")
                .Replace(@",", @"\,");

            return result;
        }

        protected virtual string EscapeTagValue(string value)
        {
            Validate.NotNull(value, "value");

            var result = value
                .Replace(@" ", @"\ ")
                .Replace(@"=", @"\=")
                .Replace(@",", @"\,");

            return result;
        }

        protected virtual string Quote(string value)
        {
            Validate.NotNull(value, "value");

            return "\"" + value + "\"";
        }

        public Serie PointToSerie(Point point)
        {
            var s = new Serie
            {
                Name = point.Measurement
            };

            foreach (var key in point.Tags.Keys.ToList())
            {
                // 0.9.5 does not return tags separately, they are treated like fields.
                //s.Tags.Add(key, point.Tags[key]);
            }

            var sortedFields = point.Fields.OrderBy(k => k.Key).ToDictionary(x => x.Key, x => x.Value);

            s.Columns = new string[] { "time" }.Concat(sortedFields.Keys).ToArray();

            s.Values = new object[][]
            {
                new object[] { point.Timestamp }.Concat(sortedFields.Values).ToArray()
            };

            return s;
        }
    }
}