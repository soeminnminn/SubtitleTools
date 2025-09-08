using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SubtitleTools
{
    public class SRTParser : ISubtitleParser
    {
        private static readonly Regex newLineRe = new Regex(@"\r?\n");
        private readonly string[] _delimiters = { "-->", "- >", "->" };
        public string FileExtension { get; set; } = ".srt";

        public bool IsSupported(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            input = input.Trim();
            input = Utils.ReplaceNewLine(input);

            Regex re = new Regex(@"(\d+)\n(\d{2}:\d{2}:\d{2},\d{3}) [-]{1,2}\s?> (\d{2}:\d{2}:\d{2},\d{3})\n");
            Match match = re.Match(input);
            int i = 0;
            while (match.Success)
            {
                i++;
                if (i == 3) break;
                match = match.NextMatch();
            }
            return i == 3;
        }

        public bool Parse(string input, ref ISubtitle result)
        {
            var items = new List<Dialogue>();

            using (var reader = new StringReader(input))
            {
                var srtSubParts = GetSrtSubTitleParts(reader).ToList();
                if (srtSubParts.Any())
                {
                    foreach (var srtSubPart in srtSubParts)
                    {
                        var lines = newLineRe.Split(srtSubPart)
                                    .Select(s => s.Trim())
                                    .Where(l => !string.IsNullOrEmpty(l))
                                    .ToList();

                        var item = new Dialogue();
                        string text = string.Empty;
                        foreach (var line in lines)
                        {
                            if (item.StartTime == 0 && item.EndTime == 0)
                            {
                                int startTc;
                                int endTc;
                                var success = TryParseTimecodeLine(line, out startTc, out endTc);
                                if (success)
                                {
                                    int idx = lines.IndexOf(line);
                                    if (idx > 0 && lines[idx - 1].IsNumber())
                                    {
                                        item.Id = lines[idx - 1].Trim();
                                    }

                                    item.StartTime = startTc;
                                    item.EndTime = endTc;
                                }
                            }
                            else if (string.IsNullOrEmpty(text))
                            {
                                text += line.Trim();
                            }
                            else
                            {
                                text += "\n" + line.Trim();
                            }

                            text = string.IsNullOrEmpty(text) ? "" : text;
                        }

                        if ((item.StartTime != 0 || item.EndTime != 0) && text.Any())
                        {
                            item.Text = text.Trim();
                            items.Add(item);
                        }
                    }

                    if (items.Any())
                    {
                        var list = Utils.RemoveDuplicateItems(items);
                        foreach(var d in list)
                        {
                            result.Add(d);
                        }
                        return true;
                    }

                    return false;
                }
            }

            return false;
        }

        private IEnumerable<string> GetSrtSubTitleParts(TextReader reader)
        {
            string line;
            var sb = new StringBuilder();

            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrEmpty(line.Trim()))
                {
                    var res = sb.ToString().TrimEnd();
                    if (!string.IsNullOrEmpty(res))
                    {
                        yield return res;
                    }

                    sb = new StringBuilder();
                }
                else
                {
                    sb.AppendLine(line);
                }
            }

            if (sb.Length > 0)
            {
                yield return sb.ToString();
            }
        }

        private bool TryParseTimecodeLine(string line, out int startTc, out int endTc)
        {
            var parts = line.Split(_delimiters, StringSplitOptions.None);
            if (parts.Length != 2)
            {
                startTc = -1;
                endTc = -1;
                return false;
            }

            startTc = ParseSrtTimecode(parts[0]);
            endTc = ParseSrtTimecode(parts[1]);
            return true;
        }

        private static int ParseSrtTimecode(string s)
        {
            var match = Regex.Match(s, "[0-9]+:[0-9]+:[0-9]+([,\\.][0-9]+)?");
            if (match.Success)
            {
                s = match.Value;
                TimeSpan result;
                if (TimeSpan.TryParse(s.Replace(',', '.'), out result))
                {
                    var nbOfMs = (int)result.TotalMilliseconds;
                    return nbOfMs;
                }
            }

            return -1;
        }
    }
}
