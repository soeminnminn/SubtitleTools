using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SubtitleTools
{
    [Flags]
    public enum TokenTypes : uint
    {
        EMPTY = 0,
        DIALOUGE = 1 << 1,
        NEW_LINE = 1 << 2,
        END_TAG = 1 << 3,
        SSA_TAG = 1 << 4,
        HTML_TAG = 1 << 5,
        DLG_START = 1 << 6,
        NONE_DLG = 1 << 7,
        BEFORE_COLON = 1 << 8,
        SONG_TAG = 1 << 9,
        ADS = 1 << 10,

        TAGS = SSA_TAG | HTML_TAG,
        ANY_DLG = DIALOUGE | DLG_START | BEFORE_COLON | NONE_DLG | SONG_TAG | ADS,
        ALL = DIALOUGE | NEW_LINE | END_TAG | SSA_TAG | HTML_TAG | DLG_START | NONE_DLG | BEFORE_COLON | SONG_TAG | ADS
    }

    public class Token
    {
        #region Variabels
        private string value = string.Empty;
        private TokenTypes tokenType = TokenTypes.EMPTY;
        private string leading = string.Empty;
        private string trailing = string.Empty;
        #endregion

        #region Constructors
        public Token(string val, TokenTypes type)
        {
            value = val;
            tokenType = type;
        }
        #endregion

        #region Properties
        internal static Token NewLine
        {
            get { return new Token("\n", TokenTypes.NEW_LINE); }
        }

        public TokenTypes TokenType
        {
            get => this.tokenType;
            set { this.tokenType = value; }
        }

        public string Value
        {
            get => this.value;
            set { this.value = value; }
        }

        public string Leading
        {
            get => this.leading;
            internal set { this.leading = value; }
        }

        public string Trailing
        {
            get => this.trailing;
            internal set { this.trailing = value; }
        }
        #endregion

        #region Methods
        public override string ToString()
        {
            return $"{this.leading}{this.value}{this.trailing}";
        }
        #endregion
    }

    internal static class Tokenizer
    {
        #region Variables
        private static readonly Regex ssaTagRe = new Regex(@"^(\{\\[^\s]+\}[^\{\}]+\{\\[^\s]+\}|\{\\[^\}]+\})$");
        private static readonly Regex htmlTagRe = new Regex(@"<\/?[-A-Za-z0-9_]+[^>]*>");
        private static readonly Regex htmlEndTagRe = new Regex(@"^<\/([-A-Za-z0-9_]+)>");
        private static readonly Regex noneDlgRe = new Regex(@"^\([^\(]+\)|\[[^\]]+\]|\{[^\{\}]+\}$");
        private static readonly Regex beforeColonRe = new Regex(@"^([^:]+:)");
        private static readonly Regex songTagRe = new Regex(@"[♪♫]+");
        private static readonly Regex speratorCharsRe = new Regex(@"^[,\.!?:;…]+");

        private static readonly Regex urlRegex = new Regex(@"(\b(https?|ftp|file):\/\/[-A-Z0-9+&@#\/%?=~_|!:,.;]*[-A-Z0-9+&@#\/%=~_|])", RegexOptions.IgnoreCase);
        private static readonly Regex urlTagRe = new Regex(@"<url href=\""([^\""]+)\"">");

        private static readonly Regex thousandSepRe = new Regex(@"([\d]{1,3})+([,]\s?[\d]{3})*([\.][\d]*)?");

        private static readonly string[] inColoned = new string[]
        {
            "ALL", "ANCHOR", "ANGENT", "BOTH", "BOY", "COMMENTATOR", "COMPUTER", "FEMALE", "GIRL",
            "KID", "LOUDSPEAKER", "MAID", "MALE", "MAN", "MEN", "MUSIC", "NARRATOR", "PHONE",
            "RADIO", "RECORDING", "REPORTER", "SENIOR", "SOLDER", "SONG", "STAMMERS", "TV",
            "TEACHER", "VOICE", "WHISPERS", "WOMAN", "VOICE-OVER", "VOICE-MAIL", "CELL PHONE",
        };

        private readonly static string[] tokenReParts = new string[]
        {
            // html tag
            @"<\/?[-A-Za-z0-9_]+[^>]*>",
            
            // ssa tag block '{\p}xxx{\p0}'
            @"\{\\[^\s]+\}[^\{\}]+\{\\[^\s]+\}",
            
            // ssa tags '{\i}'
            @"\{\\[^\s\}]+\}",
            
            // none dialog '(xxx)'
            @"\([^\(\))]+\)",
            
            // none dialog '[xxx]'
            @"\[[^\\[\]]+\]",
            
            // none dialog '{xxx}'
            @"\{[^\}\}]+\}",
            
            // song '¶ xxx ¶', '♪ xxx ♪', '♫ xxx ♫'
            songTagRe.ToString(),
            
            // dialog start '- xx'
            @"\s-\s",

            // new line
            @"\n"
        };

        private static readonly Regex tokenRe = new Regex($"({tokenReParts.Join("|")})", RegexOptions.Compiled);

        private static readonly ReplaceCondition[] preTokenReplace = new ReplaceCondition[]
        {
            new ReplaceCondition(new Regex(@"\\[Nn]"), "\n"),
            new ReplaceCondition(new Regex(@"\\h"), " "),
            new ReplaceCondition(new Regex(@"<\s?br\s?\/?>", RegexOptions.IgnoreCase), "\n"),
            new ReplaceCondition(new Regex(@"(\r\n|\r|\n)+"), "\n"),

            new ReplaceCondition(new Regex(@"<\s?(\/?\s?[-A-Za-z0-9_]+[^>]*)\s?>"), (Match match, string input) => { return "<" + match.Groups[1].Value.Trim() + ">"; }),
            new ReplaceCondition(new Regex(@"<\/\s?([-A-Za-z0-9_]+)\s?>"), "</$1>"),

            new ReplaceCondition(urlRegex, "<url href=\"$1\">"),

            // * xxx * | # xxx # | ¶ xxx ¶ --> ♪ xxx ♪
            new ReplaceCondition(new Regex(@"(^[\s]+[\*#][\s])|([\s][\*#¶][\s]+$)"), " ♪ "),
            // ♪ xxx --> ♪ xxx ♪
            new ReplaceCondition(new Regex(@"^([\s]+♪[\s])([^♪]+)[\s]+$"), "$1$2 ♪ "),
            // xxx ♪ --> ♪ xxx ♪
            new ReplaceCondition(new Regex(@"^([^♪]+)([\s]+♪[\s])$"), " ♪ $1$2"),

            // -Hello --> - Hello
            new ReplaceCondition(new Regex(@"([\.\?\'\""\s])-([a-zA-Z])"), "$1- $2"),

            // 0: 000 --> 0∶000 | 
            new ReplaceCondition(new Regex(@"([0-9]+)\s?[∶:]\s?([0-9]+)"), "$1∶$2"),
            // 0. 000 --> 0·000
            new ReplaceCondition(new Regex(@"([0-9]+)\s?[\.]\s?([0-9]+)"), "$1·$2"),

            new ReplaceCondition(thousandSepRe, (Match match, string input) => { return match.Value.Replace(',', '‚'); }),
        };
        #endregion

        #region Methods
        private static bool IsBeforeColon(string text)
        {
            if (beforeColonRe.IsMatch(text))
            {
                if (Regex.IsMatch(text, @"^[0-9:;!,\.\?\-\'\""\s…]+$")) return false;

                var orig = text.Replace("l", "I").Replace("0", "O")
                    .Replace("Mc", "MC").Replace("St", "ST")
                    .Replace("Dr", "DR").Replace("Jr", "JR")
                    .Replace("Mrs", "MRS").Replace("Mr", "MR")
                    .ReplaceRegex(@"[^\sa-z0-9]", "", RegexOptions.IgnoreCase)
                    .Trim();

                if (orig.Length == 0) return false;

                var upper = orig.ToUpperInvariant();
                if (upper == orig) return true;

                foreach (string tin in inColoned)
                {
                    if (upper.IndexOf(tin) > -1) return true;
                }

                return Regex.Replace(orig, @"[\s]+", "").Length == orig.Length;
            }
            return false;
        }

        private static bool IsAds(string text)
        {
            var temp = text.ReplaceRegex(@"^[:;!,\.\?\-\'\""\s…]", "");

            bool isMatch = false;
            foreach (var re in ToolsConstants.adMatches)
            {
                isMatch = isMatch || re.IsMatch(temp);
            }
            return isMatch;
        }

        private static bool IsTokenTypes(TokenTypes tokenType, params TokenTypes[] types)
        {
            foreach (var type in types)
            {
                if (type == TokenTypes.EMPTY)
                {
                    if (tokenType == TokenTypes.EMPTY)
                        return true;

                    continue;
                }
                
                if (tokenType.HasFlag(type))
                    return true;
            }
            return false;
        }

        public static Token[] Tokenize(string input)
        {
            string text = " " + input.Normalize(NormalizationForm.FormKC).Replace("\u002D", "").Trim().EscapeDot() + " ";
            foreach (var rep in preTokenReplace)
            {
                text = rep.Replace(text);
            }

            text = " " + text.Trim() + " ";
            text = text.ReplaceRegex(@"[ ]+", " ");

            var arr = new List<string>();

            tokenRe.Split(text).ForEach((x) =>
            {
                if (x == "\n")
                {
                    arr.Add("\\n");
                }
                else if (ssaTagRe.IsMatch(x) || htmlTagRe.IsMatch(x) || noneDlgRe.IsMatch(x))
                {
                    arr.Add(x);
                }
                else if (!string.IsNullOrEmpty(x))
                {
                    var t = x.Trim();
                    if (t.StartsWith("-"))
                    {
                        arr.Add("-");

                        if (t.Length > 1)
                            t = t.Substring(1).Trim();
                        else
                            t = string.Empty;
                    }

                    beforeColonRe.Split(t.Trim()).Where(xt => !string.IsNullOrWhiteSpace(xt))
                        .ForEach(xt => arr.Add(xt.Trim()));
                }
            });

            var result = new List<Token>();
            TokenTypes prevType = TokenTypes.EMPTY;
            bool songTagStarted = false;

            foreach (var i in arr)
            {
                TokenTypes type = TokenTypes.DIALOUGE;
                var x = i.Trim();

                var tokens = new List<Token>();

                if (x == "\\n")
                {
                    if (!IsTokenTypes(prevType, TokenTypes.EMPTY, TokenTypes.NEW_LINE, TokenTypes.BEFORE_COLON))
                        tokens.Add(Token.NewLine);
                }
                else
                {
                    x = x.ReplaceRegex(@"[\n]+", " ");

                    if (ssaTagRe.IsMatch(x))
                    {
                        type = TokenTypes.SSA_TAG;
                        tokens.Add(new Token(x, type));
                    }
                    else if (htmlTagRe.IsMatch(x))
                    {
                        type = TokenTypes.HTML_TAG;

                        if (htmlEndTagRe.IsMatch(x))
                            type |= TokenTypes.END_TAG;

                        tokens.Add(new Token(x, type));
                    }
                    else if (x == "-")
                    {
                        if (!IsTokenTypes(prevType, TokenTypes.EMPTY, TokenTypes.SSA_TAG, TokenTypes.HTML_TAG, TokenTypes.NEW_LINE))
                            tokens.Add(Token.NewLine);

                        type = TokenTypes.DLG_START;

                        tokens.Add(new Token(x, type)
                        {
                            Trailing = " ",
                        });
                    }
                    else if (noneDlgRe.IsMatch(x))
                    {
                        if (!IsTokenTypes(prevType, TokenTypes.EMPTY, TokenTypes.SSA_TAG, TokenTypes.HTML_TAG, TokenTypes.NEW_LINE, TokenTypes.DLG_START, TokenTypes.BEFORE_COLON))
                            tokens.Add(Token.NewLine);

                        type = TokenTypes.NONE_DLG;

                        x = urlTagRe.Replace(x, "$1");
                        x = x.UnescapeDot();

                        tokens.Add(new Token(x, type)
                        {
                            Trailing = " ",
                        });
                    }
                    else if (IsBeforeColon(x))
                    {
                        if (!IsTokenTypes(prevType, TokenTypes.EMPTY, TokenTypes.SSA_TAG, TokenTypes.HTML_TAG, TokenTypes.NEW_LINE, TokenTypes.DLG_START))
                            tokens.Add(Token.NewLine);

                        type = TokenTypes.BEFORE_COLON;

                        tokens.Add(new Token(x, type)
                        {
                            Trailing = " ",
                        });
                    }
                    else if (songTagRe.IsMatch(x))
                    {
                        if (!IsTokenTypes(prevType, TokenTypes.EMPTY, TokenTypes.SSA_TAG, TokenTypes.HTML_TAG, TokenTypes.NEW_LINE, TokenTypes.DLG_START, TokenTypes.BEFORE_COLON) && !songTagStarted)
                            tokens.Add(Token.NewLine);

                        type = TokenTypes.SONG_TAG;
                        if (songTagStarted)
                            type |= TokenTypes.END_TAG;

                        tokens.Add(new Token(x.Trim(), type)
                        {
                            Leading = songTagStarted ? " " : string.Empty,
                            Trailing = songTagStarted ? string.Empty : " ",
                        });

                        songTagStarted = !songTagStarted;
                    }
                    else if (IsAds(x))
                    {
                        type = TokenTypes.ADS;
                        
                        x = urlTagRe.Replace(x, "$1");
                        x = x.UnescapeDot();

                        tokens.Add(new Token(x, type)
                        {
                            Trailing = " ",
                        });
                    }
                    else
                    {
                        string leading = string.Empty;
                        string trailing = string.Empty;

                        if (IsTokenTypes(prevType, TokenTypes.DIALOUGE, TokenTypes.ADS, TokenTypes.NONE_DLG, TokenTypes.END_TAG))
                            leading = " ";

                        if (i.StartsWith(" "))
                            leading = " ";

                        if (i.Length > 2)
                            trailing = " ";

                        x = urlTagRe.Replace(x, "$1");
                        x = x.UnescapeDot();

                        var stringLiteral = new StringLiteralMatcher(x, new char[] { '"' });

                        int idx = 0;
                        foreach (var sl in stringLiteral)
                        {
                            if (sl.Index > 0)
                            {
                                int len = sl.Index - idx;
                                
                                var t = x.Substring(idx, len).Trim();
                                if (speratorCharsRe.IsMatch(t))
                                    leading = string.Empty;

                                tokens.Add(new Token(t, type)
                                {
                                    Leading = leading,
                                });

                                leading = " ";
                            }
                            

                            tokens.Add(new Token(sl.Quote + sl.Value.Trim() + sl.Quote, type)
                            {
                                Leading = leading,
                            });

                            idx = sl.Index + sl.Length;
                        }

                        var tl = x.Substring(idx).Trim();
                        if (speratorCharsRe.IsMatch(tl))
                            leading = string.Empty;

                        tokens.Add(new Token(tl, type)
                        {
                            Leading = leading,
                            Trailing = trailing,
                        });
                    }

                    if (IsTokenTypes(type, TokenTypes.END_TAG) && result.Count > 0)
                    {
                        string prevTrailing = result[result.Count - 1].Trailing;
                        result[result.Count - 1].Trailing = prevTrailing.Trim();
                    }

                    prevType = type;
                }

                result.AddRange(tokens);
             }

            return result.ToArray();
        }
        #endregion
    }
}
