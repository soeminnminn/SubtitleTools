using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SubtitleTools
{
    [Flags]
    public enum StyleTypes : uint
    {
        None = 0,
        Any = 1 << 1,
        ssaTag = 1 << 2,
        ssaBlock = 1 << 3,
        ssaStyle = 1 << 4,
        ssaFunction = 1 << 5,
        htmlTag = 1 << 6,
        htmlStartTag = 1 << 7,
        htmlEndTag = 1 << 8
    }

    public struct StyleTag
    {
        #region Variables
        public string name;
        public StyleTypes type;
        public Dictionary<string, string> attrs;
        #endregion

        #region Constructor
        public StyleTag(string name, StyleTypes type)
        {
            this.name = name;
            this.type = type;
            this.attrs = new Dictionary<string, string>();
        }
        #endregion

        #region Methods
        public override string ToString()
        {
            return $"name = {name}, type = {type}";
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            return obj.GetType() == GetType() && ToString() == obj.ToString();
        }

        public override int GetHashCode()
        {
            return $"{name}{type}".GetHashCode();
        }
        #endregion
    }

    internal static class Styler
    {
        #region Variables
        internal static readonly Regex ssaTagRe = new Regex(@"^\{(\\[\S\s]{1,})\}$");
        internal static readonly Regex ssaSingleRe = new Regex(@"^\\([a-z]+)(.*)$");
        internal static readonly Regex ssaBlockRe = new Regex(@"^\{\\([a-z]+)([^\s]+)\}(.{1,})\{\\[a-z]+(0)\}$");
        internal static readonly Regex ssaFnRe = new Regex(@"(\\[a-z]+\([^\)]+\))");

        internal static readonly Regex htmlTagRe = new Regex(@"<\/?[-A-Za-z0-9_]+[^>]*>");
        internal static readonly Regex htmlStartTagRe = new Regex(@"^<([-A-Za-z0-9_]+)((?:\s+[a-zA-Z_:][-a-zA-Z0-9_:.]*(?:\s*=\s*(?:(?:""[^""]*"")|(?:'[^']*')|[^>\s]+))?)*)\s*(\/?)>");
        internal static readonly Regex htmlEndTagRe = new Regex(@"^<\/([-A-Za-z0-9_]+)[^>]*>");
        internal static readonly Regex htmlAttrRe = new Regex(@"([a-zA-Z_:][-a-zA-Z0-9_:.]*)(?:\s*=\s*(?:(?:""((?:\\.|[^""])*)"")|(?:'((?:\\.|[^'])*)')|([^>\s]+)))?");
        #endregion

        #region Methods
        public static Tuple<string, float[]>[] ParseSsaDrawing(string text)
        {
            var list = new List<Tuple<string, float[]>>();
            if (string.IsNullOrEmpty(text)) return list.ToArray();

            var commands = text.ToLowerInvariant()
                // numbers
                .ReplaceRegex(@"([+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:e[+-]?\d+)?)", "$1")
                // commands
                .ReplaceRegex(@"([mnlbspc])", "$1")
                .Trim()
                .ReplaceRegex(@"\s+", " ")
                .SplitRegex(@"\s(?=[mnlbspc])");

            foreach(var cmd in commands)
            {
                var arr = cmd.Split(' ');
                var c = arr[0];
                var values = new float[arr.Length - 1];
                for (int i = 1; i < arr.Length; i++)
                {
                    if (float.TryParse(arr[i], out float val))
                        values[i - 1] = val;
                }
                list.Add(new Tuple<string, float[]>(c, values));
            }

            return list.ToArray();
        }

        internal static StyleTag[] ParseSsaTag(string style)
        {
            var result = new List<StyleTag>();

            var match = ssaBlockRe.Match(style);
            if (match.Success)
            {
                var tag = new StyleTag(match.Groups[1].Value, StyleTypes.ssaTag | StyleTypes.ssaBlock);
                tag.attrs.Add("start", match.Groups[2].Value);
                tag.attrs.Add("end", match.Groups[4].Value);
                tag.attrs.Add("value", match.Groups[3].Value);
                return result.ToArray();
            }

            match = ssaTagRe.Match(style);
            if (match.Success)
            {
                var splitRe = new Regex(@"(\\[^\\]+)");
                var temp = new List<string>();

                var text = match.Groups[1].Value.Trim();
                ssaFnRe.Split(text).Where(x => !string.IsNullOrWhiteSpace(x)).ForEach(x => 
                {
                    if (ssaFnRe.IsMatch(x))
                        temp.Add(x);
                    else
                        splitRe.Split(x).ForEach(y =>
                        {
                            if (!string.IsNullOrWhiteSpace(y))
                                temp.Add(y);
                        });
                });

                foreach (var x in temp)
                {
                    var m = ssaSingleRe.Match(x);
                    if (m.Success)
                    {
                        StyleTypes type = StyleTypes.ssaTag | StyleTypes.ssaStyle;
                        string val = m.Groups[2].Value.Trim();
                        if (val.StartsWith("(") && val.EndsWith(")"))
                        {
                            type = StyleTypes.ssaTag | StyleTypes.ssaFunction;
                            val = val.Substring(1, val.Length - 2);
                        }
                        var tag = new StyleTag(m.Groups[1].Value, type);
                        tag.attrs.Add("value", val);
                        result.Add(tag);
                    }
                }
            }

            return result.ToArray();
        }

        internal static StyleTag[] ParseHtmlTag(string style)
        {
            var result = new List<StyleTag>();

            var matches = htmlStartTagRe.Match(style);
            if (matches.Success)
            {
                var tag = new StyleTag(matches.Groups[1].Value, StyleTypes.htmlTag | StyleTypes.htmlStartTag);

                var attrVal = matches.Groups[2].Value.Trim();
                if (!string.IsNullOrEmpty(attrVal))
                {
                    var attrs = attrVal.Split(htmlAttrRe).Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                    for (int i = 0; i < attrs.Length; i += 2)
                    {
                        var key = attrs[i].Trim();
                        var val = attrs[i + 1].Trim();
                        tag.attrs.Add(key, val);
                    }
                }
                result.Add(tag);
            }
            else
            {
                matches = htmlEndTagRe.Match(style);
                if (matches.Success)
                    result.Add(new StyleTag(matches.Groups[1].Value, StyleTypes.htmlTag | StyleTypes.htmlEndTag));
            }

            return result.ToArray();
        }

        public static StyleTag[] Parse(string style)
        {
            if (string.IsNullOrEmpty(style)) return new StyleTag[0];

            if (ssaTagRe.IsMatch(style))
                return ParseSsaTag(style);
            else if (htmlTagRe.IsMatch(style))
                return ParseHtmlTag(style);

            return new StyleTag[0];
        }

        internal static string ApplySsaStyle(string text, params StyleTag[] styles)
        {
            if (text == null || styles.Length == 0) return text;
            var temp = text.Trim();

            var ssaBlocks = styles.Where(x => x.type.HasFlag(StyleTypes.ssaBlock)).ToArray();
            foreach (var block in ssaBlocks)
            {
                temp = "{" + $"\\{block.name}1" + "}" + block.attrs["value"] + "{" + $"\\{block.name}0" + "}" + temp;
            }

            string start = string.Empty;
            string end = string.Empty;

            var ssaStyles = styles.Where(x => 
            {
                if (x.type.HasFlag(StyleTypes.ssaStyle) && x.attrs.TryGetValue("value", out string val))
                    return val != "0";
                return x.type.HasFlag(StyleTypes.ssaStyle);

            }).Distinct().ToArray();
            
            foreach (var s in ssaStyles)
            {
                if (string.IsNullOrEmpty(start)) start = "{";
                if (string.IsNullOrEmpty(end)) end = "{";

                string val = "1";
                if (!s.attrs.TryGetValue("value", out val))
                    val = string.Empty;

                string name = s.name.ToLowerInvariant();
                start += "\\" + name + val;
                end += "\\" + name + "0";
            }

            var ssaFuns = styles.Where(x => x.type.HasFlag(StyleTypes.ssaFunction)).Distinct().ToArray();
            foreach(var fn in ssaFuns)
            {
                if (string.IsNullOrEmpty(start)) start = "{";

                string val = string.Empty;
                if (!fn.attrs.TryGetValue("value", out val))
                    val = string.Empty;

                string name = fn.name.ToLowerInvariant();
                start += "\\" + name + "(" + val + ")";
            }

            if (!string.IsNullOrEmpty(start)) start += "}";
            if (!string.IsNullOrEmpty(end)) end += "}";

            return start + temp + end;
        }

        internal static string ApplyHtmlStyle(string text, StyleTag style)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(style.name) || !style.type.HasFlag(StyleTypes.htmlTag)) return text;
            string name = style.name.ToLowerInvariant();
            string start = "<" + name;
            string end = $"</{name}>";

            if (style.attrs.Count > 0)
            {
                var attrs = style.attrs.Select(kv => 
                {
                    if (kv.Value.IsNumber())
                        return $"${kv.Key}={kv.Value}";

                    return $"${kv.Key}=\"{kv.Value}\"";
                });
                start = start + " " + string.Join(" ", attrs);
            }
            start = start + ">";

            return start + text + end;
        }

        public static string ApplyStyle(string text, params StyleTag[] styles)
        {
            if (text == null) return text;
            string temp = text.Trim();

            var ssaStyles = styles.Where(x => x.type.HasFlag(StyleTypes.ssaTag)).ToArray();

            foreach (var style in styles)
            {
                if (style.type == StyleTypes.htmlTag || style.type.HasFlag(StyleTypes.htmlStartTag))
                    temp = ApplyHtmlStyle(temp, style);
            }

            return temp;
        }
        #endregion
    }
}
