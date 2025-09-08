using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SubtitleTools
{
    public class SAMIParser : ISubtitleParser
    {
        public string FileExtension { get; set; } = ".smi";

        public bool IsSupported(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            input = input.Trim();
            return input.StartsWith("<SAMI>") && input.EndsWith("</SAMI>");
        }

        public bool Parse(string input, ref ISubtitle result)
        {
            var items = new List<Dialogue>();
            var tokenizer = new TagTokenizer(input);

            bool syncFound = false;
            var tokens = new List<TagTokenizer.TokenResult>();
            int start = 0;

            while (tokenizer.NextToken())
            {
                var token = tokenizer.Current;
                if (token.IsEmpty) continue;

                if (token.TagName == "sync")
                {
                    if (!token.IsCloseTag && token.Attributes != null && token.Attributes.TryGetValue("start", out string val))
                    {
                        start = int.Parse(val);
                        if (items.Count > 0)
                        {
                            var prev = items[items.Count - 1];
                            var end = CalculateEndTime(prev.StartTime, start, prev.Length);
                            prev.EndTime = end;
                        }
                    }

                    if (syncFound)
                    {
                        var striped = TokensToStripString(tokens);
                        if (!string.IsNullOrWhiteSpace(striped))
                        {
                            var text = TokensToString(tokens);
                            var end = start + (striped.Length * 200);
                            items.Add(new Dialogue("", start, end, text));
                        }
                        tokens.Clear();
                    }

                    syncFound = token.IsCloseTag == false;
                    continue;
                }

                if (syncFound)
                {
                    tokens.Add(token);
                }
            }

            if (tokens.Count > 0)
            {
                var striped = TokensToStripString(tokens);
                if (!string.IsNullOrWhiteSpace(striped))
                {
                    var text = TokensToString(tokens);
                    var end = start + (striped.Length * 200);
                    items.Add(new Dialogue("", start, end, text));
                }
            }

            if (items.Any())
            {
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

            return false;
        }

        private string TokensToStripString(List<TagTokenizer.TokenResult> tokens)
        {
            if (tokens.Count == 0) return string.Empty;
            var text = tokens.Where(x => !x.IsTag).Select(x => x.InnerText).Join(" ");
            return HtmlUtils.DecodeHtml(text).Trim();
        }

        private string TokensToString(List<TagTokenizer.TokenResult> tokens)
        {
            if (tokens.Count == 0) return string.Empty;
            var text = string.Empty;
            foreach(var token in tokens)
            {
                if (!string.IsNullOrEmpty(text) && token.TagName == "p")
                {
                    if (!token.IsCloseTag)
                    {
                        text += "\n";
                    }
                }
                else if (token.TagName != "p")
                {
                    text += token.ToString();
                }
            }
            return HtmlUtils.DecodeHtml(text);
        }

        private double CalculateEndTime(double currentStart, double nextStart, int textLength)
        {
            var end = currentStart + (textLength == 0 ? 1000 : textLength * 200);
            end = Math.Min(end, nextStart - 200);
            return Math.Max(currentStart + 800, end);
        }
    }
}
