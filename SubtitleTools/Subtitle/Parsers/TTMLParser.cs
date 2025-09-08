using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace SubtitleTools
{
    public class TTMLParser : ISubtitleParser
    {
        public string FileExtension { get; set; } = ".xml|.ttml";

        public bool IsSupported(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            input = input.Trim();

            Regex re = new Regex(@"^<tt\s[a-z:=\""\s]*xmlns=\""http:\/\/www\.w3\.org\/ns\/ttml\""[a-z:=\""\s]*>", RegexOptions.IgnoreCase);
            return re.IsMatch(input);
        }

        public bool Parse(string input, ref ISubtitle result)
        {
            var items = new List<Dialogue>();

            using (var textReader = new StringReader(input))
            {
                var xElement = XElement.Load(textReader);
                var tt = xElement.GetNamespaceOfPrefix("tt") ?? xElement.GetDefaultNamespace();

                var nodeList = xElement.Descendants(tt + "p").ToList();
                foreach (var node in nodeList)
                {
                    try
                    {
                        var reader = node.CreateReader();
                        reader.MoveToContent();
                        var beginString = node.Attribute("begin").Value.Replace("t", "");
                        var startTicks = ParseTimecode(beginString);
                        var endString = node.Attribute("end").Value.Replace("t", "");
                        var endTicks = ParseTimecode(endString);
                        var text = reader.ReadInnerXml()
                            .Replace("<tt:", "<")
                            .Replace("</tt:", "</")
                            .Replace(string.Format(@" xmlns:tt=""{0}""", tt), "")
                            .Replace(string.Format(@" xmlns=""{0}""", tt), "");

                        items.Add(new Dialogue($"{items.Count + 1}", startTicks, endTicks, text.Trim()));
                    }
                    catch
                    {
                        result = null;
                        return false;
                    }
                }
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

        private static long ParseTimecode(string s)
        {
            TimeSpan result;
            if (TimeSpan.TryParse(s, out result))
            {
                return (long)result.TotalMilliseconds;
            }

            long ticks;
            if (long.TryParse(s.TrimEnd('t'), out ticks))
            {
                return ticks / 10000;
            }

            return -1;
        }
    }
}
