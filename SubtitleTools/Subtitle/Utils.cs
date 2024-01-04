using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SubtitleTools
{
    public static class Utils
    {
        #region Milliseconds
        /// <summary>
        /// The duration in milliseconds
        /// </summary>
        /// <param name="str">The duration</param>
        /// <returns>milliseconds</returns>
        public static double DurationMs(string str)
        {
            if (int.TryParse(str, out int num))
                return num;

            var msMap = new (string, double)[]
            {
                ("years|year|yrs|yr|y", 365.25 * 7 * 24 * 60 * 60 * 1000),
                ("weeks|week|w", 7 * 24 * 60 * 60 * 1000),
                ("days|day|d", 24 * 60 * 60 * 1000),
                ("hours|hour|hrs|hr|h", 60 * 60 * 1000),
                ("minutes|minute|mins|min|m", 60 * 1000),
                ("seconds|second|secs|sec|s", 1000),
                ("milliseconds|millisecond|msecs|msec|ms", 1)
            };

            var msRe = new Regex(@"^(-?(?:\d+)?\.?\d+) *(milliseconds?|msecs?|ms|seconds?|secs?|s|minutes?|mins?|m|hours?|hrs?|h|days?|d|weeks?|w|years?|yrs?|y)?$", RegexOptions.IgnoreCase);
            var matches = msRe.Match(str);

            if (matches.Success)
            {
                float val = 0;
                if (!float.TryParse(matches.Groups[1].Value, out val))
                    return 0;

                var unit = matches.Groups[2].Value;
                unit = (string.IsNullOrEmpty(unit) ? "ms" : unit).ToLower();

                var ms = msMap.First(x => x.Item1.Split('|').Contains(unit));
                return val * ms.Item2;
            }

            return 0;
        }

        /// <summary>
        /// Time string to milliseconds
        /// </summary>
        /// <param name="str">The time formated string</param>
        /// <returns>Time in milliseconds</returns>
        public static double TimeMs(string str)
        {
            if (string.IsNullOrEmpty(str)) return 0;

            var intRe = new Regex(@"^(-?\d+)$");
            var floatRe = new Regex(@"^(-?)((?:\d+)?)\.(\d+)$");
            var timeRe = new Regex(@"^(?:(\d+):)?(\d{2}):(\d{2}[,\.]\d{2,})$");

            string val = str.Trim();
            if (intRe.IsMatch(val) && int.TryParse(val, out int iNum))
            {
                return Math.Abs(iNum);
            }
            else if (floatRe.IsMatch(val) && float.TryParse(val, out float fNum))
            {
                var num = fNum * 1000;
                return Math.Abs(Math.Round(num));
            }
            else
            {
                var match = timeRe.Match(val);
                if (match.Success)
                {
                    var v1 = string.IsNullOrEmpty(match.Groups[1].Value) ? "00" : match.Groups[1].Value;
                    var v2 = string.IsNullOrEmpty(match.Groups[2].Value) ? "00" : match.Groups[2].Value;
                    var v3 = string.IsNullOrEmpty(match.Groups[3].Value) ? "00" : match.Groups[3].Value;
                    v3 = v3.Replace(',', '.');

                    double num = 0;

                    if (int.TryParse(v1, out int num1))
                        num += num1 * 3600000;

                    if (int.TryParse(v2, out int num2))
                        num += num2 * 60000;

                    if (float.TryParse(v3, out float num3))
                        num += num3 * 1000;

                    return Math.Abs(Math.Round(num));
                }
            }

            return 0;
        }

        /// <summary>
        /// Time in milliseconds to the time formated string
        /// </summary>
        /// <param name="val">Time in milliseconds</param>
        /// <param name="decimalSep">Decimal separator ['.', ',']</param>
        /// <returns>The time formated string</returns>
        public static string MsTime(double val, char decimalSep = '.')
        {
            var measures = new uint[] { 3600000, 60000, 1000 };

            var num = val;
            var time = new List<string>();
            foreach (var i in measures)
            {
                var res = ((uint)(num / i) >> 0).ToString();
                if (res.Length < 2) res = $"0{res}";
                num %= i;
                time.Add(res);
            }

            var ms = num.ToString();
            if (ms.Length < 3)
            {
                for (var i = 0; i <= 3 - ms.Length; i++)
                {
                    ms = $"0{ms}";
                }
            }

            return $"{string.Join(":", time)}{decimalSep}{ms}";
        }
        #endregion

        #region Extensions
        public static string[] ToArray(this Match match)
        {
            if (match == null) return new string[0];
            if (!match.Success) return new string[0];

            int count = match.Groups.Count;
            var arr = new string[count];

            for (int i = 0; i < match.Groups.Count; i++)
            {
                arr[i] = match.Groups[i].Value;
            }
            return arr;
        }

        public static bool IsNumber(this string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            return Regex.IsMatch(value, @"^-?\d+(?:\.\d+)?$");
        }

        public static void ForEach<T>(this IEnumerable<T> arr, Action<T> action)
        {
            using (var e = arr.GetEnumerator())
            {
                while(e.MoveNext())
                {
                    action(e.Current);
                }
            }
        }

        public static string Join(this IEnumerable<string> arr, string separator)
            => string.Join(separator, arr);

        public static string ReplaceRegex(this string str, string pattern, string replacement)
            => Regex.Replace(str, pattern, replacement);

        public static string ReplaceRegex(this string str, string pattern, string replacement, RegexOptions options)
            => Regex.Replace(str, pattern, replacement, options);

        public static string Replace(this string str, Regex regex, string replacement)
            => regex.Replace(str, replacement);

        public static string[] Split(this string str, Regex regex)
            => regex.Split(str);

        public static string[] SplitRegex(this string str, string pattern)
            => Regex.Split(str, pattern);

        public static string[] SplitRegex(this string str, string pattern, RegexOptions options)
            => Regex.Split(str, pattern, options);
        #endregion

        #region Helper
        public static string SubTimeToSSA(string str)
        {
            bool first = true;
            var parts = str.Split('.');

            return parts.Select(x => 
            { 
                if (first)
                {
                    first = false;
                    int idx = x.IndexOf(':');
                    int h = int.Parse(x.Substring(0, idx));
                    return (h >= 60 ? h - 60 : h) + x.Substring(idx);
                }

                var v = Math.Round(int.Parse(x) * 0.1);
                var sv = v.ToString();
                if (sv.Length < 2)
                {
                    return $"0{sv}";
                }
                return sv;

            }).Join(".");
        }

        public static Subtitle RemoveDuplicateItems(IEnumerable<Dialogue> data)
        {
            var filteredItems = new Subtitle();
            var previousItem = new Dialogue();

            foreach (var d in data.Where(d =>
                previousItem.StartTime != d.StartTime || previousItem.EndTime != d.EndTime ||
                previousItem.Text != d.Text))
            {
                previousItem = d;
                filteredItems.Add(d);
            }

            return filteredItems;
        }

        public static List<Dialogue> AdjustSyncTime(double seconds, IEnumerable<Dialogue> data)
        {
            var fixedItems = new List<Dialogue>();

            var convertedSeconds = TimeSpan.FromSeconds(seconds);

            foreach (var f in data)
            {
                f.StartTime = new TimeSpan((long)(f.StartTime * 10000)).Add(convertedSeconds).Ticks / 10000;
                f.EndTime = new TimeSpan((long)(f.StartTime * 10000)).Add(convertedSeconds).Ticks / 10000;

                fixedItems.Add(f);
            }

            return fixedItems;
        }
        #endregion
    }
}
