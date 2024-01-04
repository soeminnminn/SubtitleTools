﻿using System;
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

        public bool Parse(Stream stream, out Subtitle result)
        {
            var subStream = new StreamReader(stream).BaseStream;

            if (!subStream.CanRead || !subStream.CanSeek)
            {
                result = null;
                return false;
            }

            subStream.Position = 0;
            var reader = new StreamReader(subStream, true);

            var items = new List<Dialogue>();
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

            if (items.Any())
            {
                result = Utils.RemoveDuplicateItems(items);
                return true;
            }

            result = null;
            return false;
        }

        private string ConvertString(string str)
        {
            str = str.Replace("<br>", "\n");
            str = str.Replace("<BR>", "\n");
            str = str.Replace("&nbsp;", "");
            try
            {
                while (str.IndexOf("<", StringComparison.Ordinal) != -1)
                {
                    var i = str.IndexOf("<", StringComparison.Ordinal);
                    var j = str.IndexOf(">", StringComparison.Ordinal);
                    str = str.Remove(i, j - i + 1);
                }

                return str;
            }
            catch
            {
                return str;
            }
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
            var item = new Dialogue("", start, end, ConvertString(string.Join("\r\n", nonEmptyLines.ToArray())));

            return item;
        }

        private bool TryExtractFrameRate(string text, out float frameRate)
        {
            if (!string.IsNullOrEmpty(text))
            {
                var success = float.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture,
                    out frameRate);
                return success;
            }

            frameRate = DefaultFrameRate;
            return false;
        }
    }
}
