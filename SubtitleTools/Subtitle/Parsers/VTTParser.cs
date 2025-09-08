using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SubtitleTools
{
    public class VTTParser : ISubtitleParser
    {
        private static readonly Regex newLineRe = new Regex(@"\r?\n");
        private readonly string[] _delimiters = { "-->", "- >", "->" };
        public string FileExtension { get; set; } = ".vtt";

        public bool IsSupported(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            input = input.Trim();

            if (input.StartsWith("WEBVTT"))
            {
                input = Utils.ReplaceNewLine(input);

                Regex re = new Regex(@"((\d{2}:)?\d{2}:\d{2}[\.,]\d{3}) [-]{1,2}\s?> ((\d{2}:)?\d{2}:\d{2}[\.,]\d{3})\n");
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
            return false;
        }

        public bool Parse(string input, ref ISubtitle result)
        {
            var items = new List<Dialogue>();
            
            using (var reader = new StringReader(input))
            {
                var vttSubParts = GetVttSubTitleParts(reader).ToList();
                if (vttSubParts.Any())
                {
                    foreach (var vttSubPart in vttSubParts)
                    {
                        var lines = newLineRe.Split(vttSubPart)
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
                                    if (idx > 0 && lines[idx - 1].IndexOf(' ') == -1)
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

                    var list = Utils.RemoveDuplicateItems(items);
                    for (var i = 0; i < list.Count; i++)
                    {
                        var d = list[i];
                        if (string.IsNullOrEmpty(d.Id))
                        {
                            d.Id = $"{i + 1}";
                        }
                        result.Add(d);
                    }
                    return true;
                }
            }

            return false;
        }

        private IEnumerable<string> GetVttSubTitleParts(TextReader reader)
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

            startTc = ParseVttTimecode(parts[0]);
            endTc = ParseVttTimecode(parts[1]);
            return true;
        }

        private int ParseVttTimecode(string s)
        {
            var timeString = string.Empty;
            var match = Regex.Match(s, "[0-9]+:[0-9]+:[0-9]+[,\\.][0-9]+");
            if (match.Success)
            {
                timeString = match.Value;
            }
            else
            {
                match = Regex.Match(s, "[0-9]+:[0-9]+[,\\.][0-9]+");
                if (match.Success)
                {
                    timeString = "00:" + match.Value;
                }
            }

            if (!string.IsNullOrEmpty(timeString))
            {
                timeString = timeString.Replace(',', '.');
                TimeSpan result;
                if (TimeSpan.TryParse(timeString, out result))
                {
                    var nbOfMs = (int)result.TotalMilliseconds;
                    return nbOfMs;
                }
            }

            return -1;
        }
    }
}
