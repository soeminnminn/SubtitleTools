using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SubtitleTools
{
    public class Cleaner
    {
        #region Variables
        internal const int MaxLineLength = 40;

        internal static readonly string[] specialChars = new string[] {
            "¶", "♪", "♫", "…", "'", "\"", "-", "+", "=", "_",
            "{", "[", "}", "]", "\\", "|", ":", ";", "<", ",",
            ">", ".", "?", "/", "`", "~", "!", "@", "#", "$",
            "%", "^", "&", "*", "(", ")"
        };

        internal static readonly string[] skipChars = new string[] {
            ":", ",", ".", "?"
        };

        internal static readonly (Regex, string)[] iOrlFixRe = new (Regex, string)[]
        {
            (new Regex(@"([a-z])['\u1FEF\u2019]II"), "$1’ll"),
            (new Regex(@"([\s\""\'`\u1FEF\u2019])[Iil]['’]m([:;!,\.\?\-\'\""\s\u2026\u201D])"), "$1I’m$2"),

            (new Regex(@"([a-zA-Z])I([aeioudy])"), "$1l$2"),
            (new Regex(@"([\s\""\'`\u1FEF\u2019])I([aeioudy][a-z]+)"), "$1l$2"),
            (new Regex(@"([a-zA-Z])I([:;!,\.\?\-\'\""`\s\u2026\u201D])"), "$1l$2"),

            (new Regex(@"([\s\""\'`\u1FEF\u2019])l([adefnostuv])([:;!,\.\?\-\'\""`\s\u2026\u201D])"), "$1I$2$3"),
            (new Regex(@"([\s\""\'`\u1FEF\u2019])[il]([:;!,\.\?\-\'\""`\s\u2026\u201D])"), "$1I$2"),
            (new Regex(@"([\s\""\'`\u1FEF\u2019])[Iil]am([:;!,\.\?\-\'\""`\s\u2026\u201D])"), "$1I am$2"),

            (new Regex(@"([:\s\-])[Iil]{2}([:;!,\.\?\-\'\""`\s\u2026\u201D])"), "$1II$2"),
            (new Regex(@"([:\s\-])[Ii]{3}([:;!,\.\?\-\'\""`\s\u2026\u201D])"), "$1III$2"),
            (new Regex(@"([:\s\-])[Iil]([VX]+)([:;!,\.\?\-\'\""`\s\u2026\u201D])"), "$1I$2$3"),
            (new Regex(@"([:\s\-])[Iil]{2}([VX]+)([:;!,\.\?\-\'\""`\s\u2026\u201D])"), "$1II$2$3"),
            (new Regex(@"([:\s\-])[Iil]{3}([VX]+)([:;!,\.\?\-\'\""`\s\u2026\u201D])"), "$1III$2$3"),
            (new Regex(@"([:\s\-])([VX]+)[Iil]([:;!,\.\?\-\'\""`\s\u2026\u201D])"), "$1$2I$3"),
            (new Regex(@"([:\s\-])([VX]+)[Iil]{2}([:;!,\.\?\-\'\""`\s\u2026\u201D])"), "$1$2II$3"),
            (new Regex(@"([:\s\-])([VX]+)[Iil]{3}([:;!,\.\?\-\'\""`\s\u2026\u201D])"), "$1$2III$3"),

            (new Regex(@"([:\s\-])(FB[Iil])([:;!\,\.\?\-\'\""`\s\u2026\u201D])"), "$1FBI$3"),
            (new Regex(@"([:\s\-])(C[Iil]A)([:;!\,\.\?\-\'\""`\s\u2026\u201D])"), "$1CIA$3"),
            (new Regex(@"([:\s\-])([aA]\.?[Iil])([:;!\,\.\?\-\'\""`\s\u2026\u201D])"), "$1AI$3"),
            (new Regex(@"([:\s\-])([Iil]\.?[oO])([:;!\,\.\?\-\'\""`\s\u2026\u201D])"), "$1IO$3"),
        };

        internal static readonly (Regex, string)[] typoFixRe = new (Regex, string)[]
        {
            (new Regex(@"([\s\""\'\u1FEF\u2019])0h([:;!,\.\?\-\'\""`\s\u2026\u201D])", RegexOptions.IgnoreCase), "$1Oh$2"),

            (new Regex(@"([\.\?\-\'\""`\u1FEF\u2019\s])M([rs]s?)[\s\.]{1,2}", RegexOptions.IgnoreCase), "$1M$2. "),
            (new Regex(@"([\.\?\-\'\""`\u1FEF\u2019\s])Dr[\s\.]{1,2}", RegexOptions.IgnoreCase), "$1Dr. "),
            (new Regex(@"([\.\?\-\'\""`\u1FEF\u2019\s])St[\s\.]{1,2}", RegexOptions.IgnoreCase), "$1St. "),

            (new Regex(@"([0-9]+)\s?([:\-])\s?([0-9]+)"), "$1$2$3"),
            (new Regex(@"([0-9]+)\s([0-9]+)"), "$1$2"),

            (new Regex(@"([a-zA-Z])\s([:;!,\.\?\-\'\u2026])"), "$1$2"),
            // (new Regex(@"([a-zA-Z]+)\""([^\""]+)\s?$"), "$1\"$2\""),
            // (new Regex(@"([a-zA-Z]+)\""([^\""]+)$"), "$1 \"$2"),
            // (new Regex(@"([a-zA-Z]+)\s?\""([^\""]+)\"""), "$1 \"$2\""),

            (new Regex(@"([Ii])['’`]m"), "I`m"),
            (new Regex(@"([Mm]a)['’`]am"), "$1`am"),
            (new Regex(@"([Hh]e|[Ii]|[Ii]t|[Ss]he|[Tt]hey|[Ww]e|[Ww]ho|[Yy]ou)['’`]ll"), "$1`ll"),
            (new Regex(@"([Ii]|[Tt]hey|[Ww]e|[Ww]ho|[Ww]ould|[Yy]ou)['’`]ve"), "$1`ve"),
            (new Regex(@"([Tt]hey|[Ww]e|[Ww]ho|[Yy]ou)['’`]re"), "$1`re"),
            (new Regex(@"([a-zA-Z]{2,})['’`]s"), "$1`s"),
            (new Regex(@"([Aa]i|[Aa]re|[Cc]a|[Dd]id|[Dd]oes|[Dd]o|[Hh]ad|[Hh]as|[Hh]ave|[Ii]s|[Mm]ay|[Nn]eed|[Ss]ha|[Ww]as|[Ww]ere|[Ww]o)n['’`]t"), "$1n`t"),
            (new Regex(@"([Hh]e|[Ii]|[Ii]t|[Ss]he|[Tt]hey|[Ww]e|[Ww]ho|[Yy]ou)['’`]d"), "$1`d"),
            (new Regex(@"([a-zA-Z]{2,})in['’`]"), "$1in`"),
            (new Regex(@"([a-zA-Z\s]+)['’`](cause|bout|em)"), "$1`$2"),
            (new Regex(@"([Cc])['’`]mon"), "$1`mon"),
        };

        internal static readonly Regex mergeLinesRe = new Regex(@"([^}>\""\-\.\?!0-9\s])\r?\n([^\s])");
        #endregion

        #region Properties
        public List<Action<ISubtitle>> BeforeCommands { get; set; } = new List<Action<ISubtitle>>();

        public List<Action<Dialogue>> InDialogueCommands { get; set; } = new List<Action<Dialogue>>();

        public List<Action<ISubtitle>> AfterCommands { get; set; } = new List<Action<ISubtitle>>();
        #endregion

        #region Constructor
        public Cleaner()
        {
            InDialogueCommands.Add(CreateSimplyStyles());
            InDialogueCommands.Add(CreateRemoveHearingText());
            InDialogueCommands.Add(CreateRemoveEmptyLine());
            InDialogueCommands.Add(CreateTypoFix());
            InDialogueCommands.Add(CreateMergeLines());

            AfterCommands.Add(CreateRemoveEmptyDialogues());
        }
        #endregion

        #region Methods
        public void Clean(ref ISubtitle subtitle)
        {
            foreach (var before in BeforeCommands)
            {
                before(subtitle);
            }

            foreach (var dlg in subtitle)
            {
                foreach (var inDlg in InDialogueCommands)
                {
                    inDlg(dlg);
                }
            }

            foreach (var after in AfterCommands)
            {
                after(subtitle);
            }
        }

        private static int FindCutPoint(string text)
        {
            if (string.IsNullOrEmpty(text)) return -1;
            if (text.Length < MaxLineLength) return -1;

            List<string> cutChars = new List<string>() { ". ", "? ", "! ", "… " };

            int halfIdx = (int)Math.Floor(text.Length * 0.5);

            if (halfIdx < MaxLineLength)
            {
                string temp = Regex.Replace(text, @"([\.\?\-\'\""\s])(Mrs?)\.", "$1$2\u05C5", RegexOptions.IgnoreCase);
                temp = Regex.Replace(temp, @"([\.\?\-\'\""\s])(Dr|St)\.", "$1$2\u05C5", RegexOptions.IgnoreCase);
                temp = Regex.Replace(temp, @"([^\s])\.([^\s])", "$1\u05C5$2");
                temp = Regex.Replace(temp, @"([^\s]),([^\s])", "$1\u05A5$2");

                for (var i = halfIdx; i < MaxLineLength && i < temp.Length; i++)
                {
                    var check = temp[i].ToString() + temp[i + 1].ToString();
                    if (cutChars.Contains(check))
                    {
                        return i + 1;
                    }
                }

                int spaceCount = 0;
                int fromEnd = temp.Length - MaxLineLength;
                for (var i = halfIdx; i > fromEnd && i > 0; i--)
                {
                    if (temp[i] == ' ')
                    {
                        spaceCount++;
                        if (spaceCount == 3) break;
                    }

                    var check = temp[i].ToString() + temp[i + 1].ToString();
                    if (cutChars.Contains(check))
                    {
                        return i + 1;
                    }
                }
            }

            int spIdx = text.IndexOf(' ', halfIdx);
            if (spIdx > MaxLineLength)
            {
                spIdx = text.Substring(0, MaxLineLength).LastIndexOf(' ');
            }

            return spIdx;
        }

        public static Action<Dialogue> CreateSimplyStyles()
        {
            return new Action<Dialogue>(dialogue =>
            {
                if (dialogue.Tokens == null) return;
                if (dialogue.Tokens.Length == 0) return;

                var tokens = dialogue.Tokens;
                var list = new List<Token>();

                List<TokenTypes> types = new List<TokenTypes>();

                for (int i = 0; i < tokens.Length; i++)
                {
                    var token = tokens[i];
                    if (token.tokenType == TokenTypes.SSA_TAG)
                    {
                        if (Regex.IsMatch(token.value, @"\{i[0-9]?\}"))
                        {
                            types.Add(token.tokenType);
                        }
                        else if (token.value.Contains(@"{\an8}"))
                        {
                            var style = new StyleTag("an", StyleTypes.ssaTag);
                            style.attrs.Add("value", "8");

                            dialogue.Styles.Add(style);
                        }
                        token.tokenType = TokenTypes.EMPTY;
                    }
                    else if (token.tokenType == TokenTypes.HTML_TAG)
                    {
                        if (Regex.IsMatch(token.value, @"<\/?i>", RegexOptions.IgnoreCase))
                        {
                            types.Add(token.tokenType);
                        }
                        token.tokenType = TokenTypes.EMPTY;
                    }
                    else if (token.tokenType == TokenTypes.ADS || token.tokenType == TokenTypes.DIALOUGE || token.tokenType == TokenTypes.BEFORE_COLON || token.tokenType == TokenTypes.NONE_DLG || token.tokenType == TokenTypes.SONG_TAG)
                    {
                        types.Add(token.tokenType);
                    }

                    if (token.tokenType != TokenTypes.EMPTY)
                        list.Add(token);
                }

                if (types.Count > 2)
                {
                    if (types[0] == TokenTypes.SSA_TAG || types[0] == TokenTypes.HTML_TAG)
                    {
                        var li = types.Count - 1;
                        if (types[li] == TokenTypes.SSA_TAG || types[li] == TokenTypes.HTML_TAG)
                        {
                            dialogue.Styles.Add(new StyleTag("i", StyleTypes.htmlTag));
                        }
                    }
                }

                dialogue.Tokens = list.ToArray();
            });
        }

        public static Action<Dialogue> CreateRemoveHearingText()
        {
            return new Action<Dialogue>(dialogue =>
            {
                if (dialogue.Tokens == null) return;
                if (dialogue.Tokens.Length == 0) return;

                var ads = dialogue.Tokens.Count(t => t.tokenType.HasFlag(TokenTypes.ADS));
                if (ads > 0)
                {
                    dialogue.Text = string.Empty;
                    return;
                }

                var text = dialogue.ToString(TokenTypes.DLG_START | TokenTypes.DIALOUGE | TokenTypes.NEW_LINE | TokenTypes.SONG_TAG);

                string temp = Regex.Replace(text, @"\r?\n", "");
                foreach (var ch in specialChars)
                {
                    temp = temp.Replace(ch, "");
                }

                if (temp.Trim().Length == 0)
                    dialogue.Text = string.Empty;
                else
                {
                    text = text.Trim();

                    var arr = text.SplitRegex(@"\r?\n");
                    string[] lines = new string[arr.Length];
                    for (var i = 0; i < arr.Length; i++)
                    {
                        var l = arr[i];
                        if (string.IsNullOrWhiteSpace(l)) continue;

                        string[] words = l.Split(' ');
                        if (words.Length > 1)
                        {
                            if (words[0] == "-" && Array.IndexOf(skipChars, words[1]) > -1)
                                words[1] = "";
                            else if (Array.IndexOf(skipChars, words[0]) > -1)
                                words[0] = "";
                        }

                        lines[i] = words.Join(" ").ReplaceRegex(@"[ ]+", " ");
                    }

                    dialogue.Text = lines.Where(x => !string.IsNullOrEmpty(x)).Join("\r\n");
                }
            });
        }

        public static Action<Dialogue> CreateRemoveEmptyLine()
        {
            return new Action<Dialogue>(dialogue =>
            {
                if (dialogue.Tokens == null) return;
                if (dialogue.Tokens.Length == 0) return;

                var tokens = dialogue.Tokens;
                var list = new List<Token>();

                for (int i = 0; i < tokens.Length; i++)
                {
                    var token = tokens[i];
                    if ((TokenTypes.ANY_DLG & token.tokenType) == token.tokenType)
                    {
                        if (token.tokenType.HasFlag(TokenTypes.DLG_START))
                        {
                            if (i + 1 == tokens.Length)
                                token.tokenType = TokenTypes.EMPTY;
                            else if (i + 1 < tokens.Length)
                            {
                                var next = tokens[i + 1];
                                if (next.tokenType.HasFlag(TokenTypes.NEW_LINE) || next.tokenType.HasFlag(TokenTypes.DLG_START))
                                    token.tokenType = TokenTypes.EMPTY;
                            }
                        }
                        else
                        {
                            string temp = token.value;
                            foreach (var ch in specialChars)
                            {
                                temp = temp.Replace(ch, "");
                            }
                            if (temp.Trim().Length == 0)
                                token.tokenType = TokenTypes.EMPTY;
                        }
                    }

                    if (token.tokenType != TokenTypes.EMPTY)
                        list.Add(token);
                }

                dialogue.Tokens = list.ToArray();
            });
        }

        public static Action<Dialogue> CreateTypoFix()
        {
            return new Action<Dialogue>(dialogue =>
            {
                if (dialogue.Tokens == null) return;
                if (dialogue.Tokens.Length == 0) return;

                var tokens = dialogue.Tokens;
                var list = new List<Token>();

                for (int i = 0; i < tokens.Length; i++)
                {
                    var token = tokens[i];
                    if ((TokenTypes.ANY_DLG & token.tokenType) == token.tokenType)
                    {
                        foreach (var rep in iOrlFixRe)
                        {
                            var val = " " + token.value + " ";
                            val = rep.Item1.Replace(val, rep.Item2).Trim();

                            token.value = val;
                        }

                        foreach (var rep in typoFixRe)
                        {
                            var val = " " + token.value + " ";
                            val = rep.Item1.Replace(val, rep.Item2).Trim();

                            token.value = val;
                        }
                    }
                    list.Add(token);
                }
                dialogue.Tokens = list.ToArray();
            });
        }

        public static Action<Dialogue> CreateMergeLines()
        {
            return new Action<Dialogue>(dialogue =>
            {
                if (dialogue.Tokens == null) return;
                if (dialogue.Tokens.Length == 0) return;

                var text = dialogue.Text;

                if (dialogue.LineCount > 1)
                {
                    text = mergeLinesRe.Replace(text, "$1 $2");
                }

                var arr = text.SplitRegex(@"\r?\n").Where(x => !string.IsNullOrEmpty(x)).ToArray();

                var list = new string[arr.Length];
                Array.Copy(arr, list, arr.Length);

                if (arr.Length == 1)
                {
                    if (arr[0].Length > MaxLineLength)
                    {
                        int pt = FindCutPoint(arr[0]);
                        if (pt > 0)
                        {
                            list[0] = arr[0].Insert(pt, "\n").Split('\n').Select(x => x.Trim()).Join("\n");
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < arr.Length; i++)
                    {
                        if (arr[i].Length > MaxLineLength)
                        {
                            int pt = FindCutPoint(arr[i]);
                            if (pt > 0)
                            {
                                list[i] = arr[i].Insert(pt, "\n").Split('\n').Select(x => x.Trim()).Join("\n");
                            }
                        }
                    }
                }

                dialogue.Text = list.Join("\n");
            });
        }

        public static Action<ISubtitle> CreateRemoveEmptyDialogues()
        {
            return new Action<ISubtitle>(subTitle =>
            {
                var cues = subTitle.Where(x =>
                {
                    var text = x.ToString(TokenTypes.ANY_DLG);
                    foreach (var sc in specialChars)
                    {
                        text = text.Replace(sc, "");
                    }
                    return !string.IsNullOrEmpty(text);
                }).ToArray();

                ((ICollection<Dialogue>)subTitle).Clear();
                foreach (var cue in cues)
                {
                    subTitle.Add(cue);
                }
            });
        }
        #endregion
    }
}
