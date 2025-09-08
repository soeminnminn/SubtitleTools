using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;

namespace SubtitleTools
{
    public class YtXmlParser : ISubtitleParser
    {
        public string FileExtension { get; set; } = ".xml";

        public bool IsSupported(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            input = input.Trim();

            try
            {
                using (var textReader = new StringReader(input))
                {
                    var xmlDoc = new XmlDocument();
                    xmlDoc.Load(textReader);
                    if (xmlDoc.DocumentElement != null)
                    {
                        var nodeList = xmlDoc.DocumentElement.SelectNodes("//text");
                        return nodeList != null && nodeList.Count > 0;
                    }
                }
            }
            catch { }

            return false;
        }

        public bool Parse(string input, ref ISubtitle result)
        {
            var items = new List<Dialogue>();

            using (var textReader = new StringReader(input))
            {
                // parse xml stream
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(textReader);

                if (xmlDoc.DocumentElement != null)
                {
                    var nodeList = xmlDoc.DocumentElement.SelectNodes("//text");

                    if (nodeList != null)
                    {
                        for (var i = 0; i < nodeList.Count; i++)
                        {
                            var node = nodeList[i];
                            try
                            {
                                var startString = node.Attributes["start"].Value;
                                var start = float.Parse(startString, CultureInfo.InvariantCulture);
                                var durString = node.Attributes["dur"].Value;
                                var duration = float.Parse(durString, CultureInfo.InvariantCulture);
                                var text = node.InnerText;

                                items.Add(new Dialogue($"{items.Count + 1}", (int)(start * 1000), (int)((start + duration) * 1000), text.Trim()));
                            }
                            catch
                            {
                                return false;
                            }
                        }
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
    }
}
