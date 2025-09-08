using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SubtitleTools
{
    public class SSAParser : ISubtitleParser
    {
        private static readonly Regex newLineRe = new Regex(@"\r?\n");
        private const string ScriptInfoLine = "[Script Info]";
        private const string EventLine = "[Events]";
        private const char Separator = ',';

        private const string StartColumn = "Start";
        private const string EndColumn = "End";
        private const string TextColumn = "Text";
        public string FileExtension { get; set; } = ".ass|.ssa";

        public bool IsSupported(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            input = input.Trim();
            if (input.StartsWith(ScriptInfoLine))
            {
                string[] lines = newLineRe.Split(input);
                int eventIndx = Array.IndexOf(lines, EventLine);
                return eventIndx > 1;
            }
            return false;
        }

        public bool Parse(string input, ref ISubtitle result)
        {
            using (var reader = new StringReader(input))
            {
                var line = reader.ReadLine();
                var lineNumber = 1;
                bool hasInfo = line == ScriptInfoLine;

                while (line != null && line != EventLine)
                {
                    line = reader.ReadLine();
                    lineNumber++;

                    if (!string.IsNullOrEmpty(line))
                    {
                        var trimedLine = line.Trim();
                        if (trimedLine.StartsWith('[') && trimedLine.EndsWith(']'))
                        {
                            hasInfo = false;
                        }

                        if (hasInfo && !trimedLine.StartsWith(';') && trimedLine.Contains(':'))
                        {
                            var idx = trimedLine.IndexOf(':');
                            var key = trimedLine.Substring(0, idx);
                            var val = trimedLine.Substring(idx + 1);

                            result.Headers.Meta.Add(key.Trim(), val.Trim());
                        }
                    }
                }

                if (line != null)
                {
                    var headerLine = reader.ReadLine();
                    if (!string.IsNullOrEmpty(headerLine))
                    {
                        var columnHeaders = headerLine.Split(Separator).Select(head => head.Trim()).ToList();

                        var startIndexColumn = columnHeaders.IndexOf(StartColumn);
                        var endIndexColumn = columnHeaders.IndexOf(EndColumn);
                        var textIndexColumn = columnHeaders.IndexOf(TextColumn);

                        if (startIndexColumn > 0 && endIndexColumn > 0 && textIndexColumn > 0)
                        {
                            var items = new List<Dialogue>();

                            line = reader.ReadLine();
                            while (line != null)
                            {
                                if (!string.IsNullOrEmpty(line))
                                {
                                    var columns = line.Split(Separator);
                                    var startText = columns[startIndexColumn];
                                    var endText = columns[endIndexColumn];

                                    var textLine = string.Join(",", columns.Skip(textIndexColumn));

                                    var start = ParseSsaTimecode(startText);
                                    var end = ParseSsaTimecode(endText);

                                    if (start > 0 && end > 0 && !string.IsNullOrEmpty(textLine))
                                    {
                                        var item = new Dialogue($"{items.Count + 1}", start, end, textLine.Trim());
                                        items.Add(item);
                                    }
                                }

                                line = reader.ReadLine();
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

                    return false;
                }
            }

            return false;
        }

        private int ParseSsaTimecode(string s)
        {
            TimeSpan result;

            if (TimeSpan.TryParse(s, out result))
            {
                var nbOfMs = (int)result.TotalMilliseconds;
                return nbOfMs;
            }

            return -1;
        }
    }
}
