using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SubtitleTools
{
    public class MicroDVDParser : ISubtitleParser
    {
        private const string LineRegex = @"^[{\[](-?\d+)[}\]][{\[](-?\d+)[}\]](.*)";

        private readonly char[] _lineSeparators = { '|' };

        public float DefaultFrameRate { get; set; } = 23.976f;

        public string FileExtension { get; set; } = ".sub";

        public bool IsSupported(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            input = input.Trim();
            input = Utils.ReplaceNewLine(input);

            Regex re = new Regex(@"\{(\d+)\}\{(\d+)\}([^\r\n]+)");
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
                var line = reader.ReadLine();
                while (line != null && !IsMicroDvdLine(line))
                {
                    line = reader.ReadLine();
                }

                if (line != null)
                {
                    float frameRate;
                    var firstItem = ParseLine(line, DefaultFrameRate);
                    if (firstItem.Text != null && firstItem.Text.Any())
                    {
                        var success = TryExtractFrameRate(firstItem.Text, out frameRate);
                        if (!success)
                        {
                            frameRate = DefaultFrameRate;
                            items.Add(firstItem);
                        }
                    }
                    else
                    {
                        frameRate = DefaultFrameRate;
                    }

                    line = reader.ReadLine();
                    while (line != null)
                    {
                        if (!string.IsNullOrEmpty(line))
                        {
                            var item = ParseLine(line, frameRate);
                            item.Id = $"{items.Count + 1}";
                            items.Add(item);
                        }

                        line = reader.ReadLine();
                    }
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

        private bool IsMicroDvdLine(string line)
        {
            return Regex.IsMatch(line, LineRegex);
        }

        private Dialogue ParseLine(string line, float frameRate)
        {
            var match = Regex.Match(line, LineRegex);
            if (!match.Success || match.Groups.Count <= 2)
            {
                return null;
            }

            var startFrame = match.Groups[1].Value;
            var start = (int)(1000 * double.Parse(startFrame) / frameRate);
            var endTime = match.Groups[2].Value;
            var end = (int)(1000 * double.Parse(endTime) / frameRate);
            var text = match.Groups[^1].Value;
            var lines = text.Split(_lineSeparators);
            var nonEmptyLines = lines.Where(l => !string.IsNullOrEmpty(l)).ToList();
            var item = new Dialogue("", start, end, string.Join("\r\n", nonEmptyLines.ToArray()).Trim());

            return item;
        }

        private bool TryExtractFrameRate(string text, out float frameRate)
        {
            if (!string.IsNullOrEmpty(text))
            {
                var success = float.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out frameRate);
                return success;
            }

            frameRate = DefaultFrameRate;
            return false;
        }
    }
}
