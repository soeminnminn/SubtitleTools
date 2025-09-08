using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SubtitleTools
{
    public class SubViewerParser : ISubtitleParser
    {
        private const string FirstLine = "[INFORMATION]";
        private const short MaxLineNumberForItems = 20;
        private const char TimecodeSeparator = ',';

        private readonly Regex _timestampRegex =
            new Regex(@"\d{2}:\d{2}:\d{2}\.\d{2},\d{2}:\d{2}:\d{2}\.\d{2}", RegexOptions.Compiled);

        public string FileExtension { get; set; } = ".sub";

        public bool IsSupported(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            input = input.Trim();
            return input.StartsWith(FirstLine);
        }

        public bool Parse(string input, ref ISubtitle result)
        {
            using (var reader = new StringReader(input))
            {
                var firstLine = reader.ReadLine();
                if (firstLine == FirstLine)
                {
                    var line = reader.ReadLine();
                    var lineNumber = 2;
                    while (line != null && lineNumber <= MaxLineNumberForItems && !IsTimestampLine(line))
                    {
                        line = reader.ReadLine();
                        lineNumber++;
                    }

                    if (line != null && lineNumber <= MaxLineNumberForItems && IsTimestampLine(line))
                    {
                        var items = new List<Dialogue>();

                        var timeCodeLine = line;
                        var textLines = new List<string>();

                        while (line != null)
                        {
                            line = reader.ReadLine();
                            if (IsTimestampLine(line))
                            {
                                var timeCodes = ParseTimecodeLine(timeCodeLine);
                                var start = timeCodes.Item1;
                                var end = timeCodes.Item2;

                                if (start > 0 && end > 0 && textLines.Any())
                                {
                                    items.Add(new Dialogue($"{items.Count + 1}", start, end, string.Join("\r\n", textLines.ToArray()).Trim()));
                                }

                                timeCodeLine = line;
                                textLines = new List<string>();
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(line))
                                {
                                    textLines.Add(line);
                                }
                            }
                        }

                        var lastTimeCodes = ParseTimecodeLine(timeCodeLine);
                        var lastStart = lastTimeCodes.Item1;
                        var lastEnd = lastTimeCodes.Item2;
                        if (lastStart > 0 && lastEnd > 0 && textLines.Any())
                        {
                            items.Add(new Dialogue($"{items.Count + 1}", lastStart, lastEnd, string.Join("\r\n", textLines.ToArray()).Trim()));
                        }

                        if (items.Any())
                        {
                            var list = Utils.RemoveDuplicateItems(items);
                            foreach (var d in list)
                            {
                                result.Add(d);
                            }
                            return true;
                        }

                        return false;
                    }

                    return false;
                }
            }

            return false;
        }

        private Tuple<int, int> ParseTimecodeLine(string line)
        {
            var parts = line.Split(TimecodeSeparator);
            if (parts.Length == 2)
            {
                var start = ParseTimecode(parts[0]);
                var end = ParseTimecode(parts[1]);
                return new Tuple<int, int>(start, end);
            }

            var message = string.Format("Couldn't parse the timecodes in line '{0}'", line);
            throw new ArgumentException(message);
        }

        private int ParseTimecode(string s)
        {
            TimeSpan result;

            if (TimeSpan.TryParse(s, out result))
            {
                var nbOfMs = (int)result.TotalMilliseconds;
                return nbOfMs;
            }

            return -1;
        }

        private bool IsTimestampLine(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return false;
            }

            var isMatch = _timestampRegex.IsMatch(line);
            return isMatch;
        }
    }
}
