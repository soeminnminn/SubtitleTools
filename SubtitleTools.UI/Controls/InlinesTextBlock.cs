using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace SubtitleTools.UI.Controls
{
    public class InlinesTextBlock : TextBlock
    {
        #region Variables
        private static readonly Regex htmlTagRe = new Regex(@"<[^>]+>");
        private static readonly Regex splitRe = new Regex(@"(<\/?b>|<\/?i>|<\/?u>|<font[^>]*color=[^>]*>|<\/?font>)", RegexOptions.IgnoreCase);
        private static readonly Regex htmlStartTagRe = new Regex(@"^<([-A-Za-z0-9_]+)((?:\s+[a-zA-Z_:][-a-zA-Z0-9_:.]*(?:\s*=\s*(?:(?:""[^""]*"")|(?:'[^']*')|[^>\s]+))?)*)\s*(\/?)>");
        private static readonly Regex htmlEndTagRe = new Regex(@"^<\/([-A-Za-z0-9_]+)[^>]*>");
        private static readonly Regex htmlAttrRe = new Regex(@"([a-zA-Z_:][-a-zA-Z0-9_:.]*)(?:\s*=\s*(?:(?:""((?:\\.|[^""])*)"")|(?:'((?:\\.|[^'])*)')|([^>\s]+)))?");

        private static readonly KeyValuePair<string, string>[] encodeDecode = new[]
        {
            new KeyValuePair<string, string>("&lt;", "<"),
            new KeyValuePair<string, string>("&gt;", ">"),
            new KeyValuePair<string, string>("&quot;", "\""),
            new KeyValuePair<string, string>("&amp;", "&"),
            new KeyValuePair<string, string>("&nbsp;", " "),
        };

        private bool needUpdate = true;
        #endregion

        #region Constructors
        static InlinesTextBlock()
        {
            TextProperty.OverrideMetadata(typeof(InlinesTextBlock), new FrameworkPropertyMetadata(OnTextChanged));
        }

        public InlinesTextBlock()
        { }
        #endregion

        #region Dependency Properties
        #endregion

        #region Methods
        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is InlinesTextBlock ctrl)
            {
                ctrl.OnTextChanged((string)e.OldValue, (string)e.NewValue);
                ctrl.UpdateText((string)e.NewValue);
            }
        }

        private void OnTextChanged(string oldValue, string newValue)
        {

        }

        private void UpdateText(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            if (needUpdate)
            {
                if (!htmlTagRe.IsMatch(text)) return;

                var tokens = splitRe.Split(text).Where(x => !string.IsNullOrEmpty(x)).ToArray();
                List<Inline> inlines = new List<Inline>();

                Styles styles = new Styles()
                {
                    TextBrush = Foreground
                };

                foreach (var tok in tokens)
                {
                    var match = htmlStartTagRe.Match(tok);
                    if (match.Success)
                    {
                        var tagName = match.Groups[1].Value.Trim().ToLower();
                        switch (tagName)
                        {
                            case "b":
                                styles.Bold = true;
                                break;
                            case "i":
                                styles.Italic = true;
                                break;
                            case "u":
                                styles.Underline = true;
                                break;
                            case "font":
                                styles.TextBrush = GetTextBrush(match.Groups[2].Value);
                                break;
                        }
                        continue;
                    }

                    match = htmlEndTagRe.Match(tok);
                    if (match.Success)
                    {
                        var tagName = match.Groups[1].Value.Trim().ToLower();
                        switch (tagName)
                        {
                            case "b":
                                styles.Bold = false;
                                break;
                            case "i":
                                styles.Italic = false;
                                break;
                            case "u":
                                styles.Underline = false;
                                break;
                            case "font":
                                styles.TextBrush = Foreground;
                                break;
                        }
                        continue;
                    }

                    var run = new Run(DecodeHtml(tok))
                    {
                        Foreground = styles.TextBrush
                    };

                    if (styles.Bold)
                    {
                        run.FontWeight = FontWeights.Bold;
                    }
                    if (styles.Italic)
                    {
                        run.FontStyle = FontStyles.Italic;
                    }
                    if (styles.Underline)
                    {
                        run.TextDecorations = System.Windows.TextDecorations.Underline;
                    }
                    inlines.Add(run);
                }

                needUpdate = false;
                Inlines.Clear();
                Inlines.AddRange(inlines);
                needUpdate = true;
            }
        }

        private Brush GetTextBrush(string attrs)
        {
            attrs = attrs.Trim();
            var parts = htmlAttrRe.Split(attrs).Select(x => x.Trim()).Where(x => !string.IsNullOrEmpty(x)).ToArray();
            for (int i = 0; i < parts.Length; i += 2)
            {
                var key = parts[i].Trim().ToLower();
                var val = parts[i + 1].Trim();
                
                if (key == "color")
                {
                    try
                    {
                        var color = HexToColor(val);
                        return new SolidColorBrush(color);
                    }
                    catch
                    {
                        return Foreground;
                    }
                }
            }

            return Foreground;
        }

        private static Color HexToColor(string value)
        {
            value = value.Trim('#');
            if (value.Length == 0)
            {
                throw new InvalidCastException();
            }

            if (value.Length <= 6)
            {
                value = "FF" + value.PadLeft(6, '0');
            }

            if (uint.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint u))
            {
                var a = (byte)(u >> 24);
                var r = (byte)(u >> 16);
                var g = (byte)(u >> 8);
                var b = (byte)(u >> 0);
                return Color.FromArgb(a, r, g, b);
            }

            throw new InvalidCastException();
        }

        private static string DecodeHtml(string str)
        {
            string temp = str;

            foreach (var kv in encodeDecode)
            {
                temp = temp.Replace(kv.Key, kv.Value);
            }

            return temp;
            // return System.Net.WebUtility.HtmlDecode(str);
        }
        #endregion

        #region Nested Types
        private class Styles
        {
            #region Properties
            public bool Bold { get; set; }
            public bool Italic { get; set; }
            public bool Underline { get; set; }

            public Brush TextBrush { get; set; } = SystemColors.WindowTextBrush;
            #endregion
        }
        #endregion
    }
}
