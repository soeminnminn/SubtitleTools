﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SubtitleTools
{
    public class SAMIParser : ISubtitleParser
    {
        public string FileExtension { get; set; } = ".smi";

        public bool Parse(Stream stream, out Subtitle result)
        {
            var items = new List<Dialogue>();
            var sr = new StreamReader(stream);

            var line = sr.ReadLine();
            if (line == null || !line.Equals("<SAMI>"))
            {
                sr.Close();
                result = null;
                return false;
            }

            while ((line = sr.ReadLine()) != null)
            {
                if (line.Equals("<BODY>"))
                {
                    break;
                }
            }

            if (string.IsNullOrEmpty(line))
            {
                sr.Close();
                result = null;
                return false;
            }

            var check = false;
            var miClassString = new string[2];
            var sb = new StringBuilder();
            var sbComment = false;

            while (string.IsNullOrEmpty(line) != true)
            {
                if (check == false)
                {
                    line = sr.ReadLine();

                    while (true)
                    {
                        if (string.IsNullOrEmpty(line))
                        {
                            line = sr.ReadLine();
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else
                {
                    check = false;
                }

                if (line.Contains("<--") && line.Contains("-->"))
                {
                    continue;
                }

                if (line.Contains("<!--") && line.Contains("-->"))
                {
                    continue;
                }

                if (line.Contains("<!--"))
                {
                    sbComment = true;
                }

                if (line.Contains("-->"))
                {
                    sbComment = false;
                }

                if (sbComment)
                {
                    continue;
                }

                if (line.Contains("</BODY>"))
                {
                    break;
                }

                if (line.Contains("</SAMI>"))
                {
                    break;
                }

                if (line[0].Equals('<'))
                {
                    var length = line.IndexOf('>');
                    miClassString[0] = line.Substring(1, length - 1);
                    miClassString[1] = line.Substring(length + 2);
                    var splitIndex = miClassString[1].IndexOf('>');
                    miClassString[1] = miClassString[1].Remove(splitIndex);
                    var miSync = miClassString[0].Split('=');

                    while ((line = sr.ReadLine())?.ToUpper().Contains("<SYNC", StringComparison.OrdinalIgnoreCase) ==
                           false)
                    {
                        sb.Append(line);
                    }

                    items.Add(new Dialogue(int.Parse(miSync[1]), ConvertString(sb.ToString())));

                    sb = new StringBuilder();

                    check = true;
                }
            }

            sr.Close();

            for (var i = 0; i < items.Count; i++)
            {
                var endTime = i == items.Count - 1
                    ? items[i].StartTime + 1000
                    : items[i + 1].StartTime;

                items[i].EndTime = endTime;
            }

            result = Utils.RemoveDuplicateItems(items);
            return true;
        }

        private string ConvertString(string str)
        {
            str = str.Replace("<br>", "\n");
            str = str.Replace("<BR>", "\n");
            str = str.Replace("&nbsp;", "");
            str = str.Replace("<--", "");
            str = str.Replace("<!--", "");
            str = str.Replace("-->", "");

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
    }
}
