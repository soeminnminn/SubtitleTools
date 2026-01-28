using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SubtitleTools
{
    [Flags]
    public enum TokenTypes : uint
    {
        EMPTY = 0,
        DIALOUGE = 1 << 1,
        NEW_LINE = 1 << 2,
        SSA_TAG = 1 << 3,
        HTML_TAG = 1 << 4,
        DLG_START = 1 << 5,
        NONE_DLG = 1 << 6,
        BEFORE_COLON = 1 << 7,
        SONG_TAG = 1 << 8,
        ADS = 1 << 9,

        TAGS = SSA_TAG | HTML_TAG,
        ANY_DLG = DIALOUGE | DLG_START | BEFORE_COLON | NONE_DLG | SONG_TAG | ADS,
        ALL = DIALOUGE | NEW_LINE | SSA_TAG | HTML_TAG | DLG_START | NONE_DLG | BEFORE_COLON | SONG_TAG | ADS
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
        internal static readonly Regex ssaTagRe = new Regex(@"^(\{\\[^\s]+\}[^\{\}]+\{\\[^\s]+\}|\{\\[^\}]+\})$");
        internal static readonly Regex htmlTagRe = new Regex(@"^<[^>]+>$");
        internal static readonly Regex noneDlgRe = new Regex(@"^\([^\(]+\)|\[[^\]]+\]|\{[^\{\}]+\}$");
        internal static readonly Regex beforeColonRe = new Regex(@"^([^:]+:)");
        internal static readonly Regex songTagRe = new Regex(@"[♪♫]+");
        internal static readonly Regex speratorCharRe = new Regex(@"([^\.\?!;,…]*[\.\?!;,…]+)");

        internal static readonly Regex urlRegex = new Regex(@"(\b(https?|ftp|file):\/\/[-A-Z0-9+&@#\/%?=~_|!:,.;]*[-A-Z0-9+&@#\/%=~_|])", RegexOptions.IgnoreCase);
        internal static readonly Regex urlTagRe = new Regex(@"<url href=\""([^\""]+)\"">");

        internal static readonly Regex thousandSepRe = new Regex(@"([\d]{1,3})+([,]\s?[\d]{3})*([\.][\d]*)?");

        internal static readonly string[] inColoned = new string[]
        {
            "ALL", "ANCHOR", "ANGENT", "BOTH", "BOY", "COMMENTATOR", "COMPUTER", "FEMALE", "GIRL",
            "KID", "LOUDSPEAKER", "MAID", "MALE", "MAN", "MEN", "MUSIC", "NARRATOR", "PHONE",
            "RADIO", "RECORDING", "REPORTER", "SENIOR", "SOLDER", "SONG", "STAMMERS", "TV",
            "TEACHER", "VOICE", "WHISPERS", "WOMAN", "VOICE-OVER", "VOICE-MAIL", "CELL PHONE",
        };

        internal readonly static string[] tokenReParts = new string[]
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

        internal static readonly Regex tokenRe = new Regex($"({tokenReParts.Join("|")})", RegexOptions.Compiled);

        internal static readonly ReplaceCondition[] preTokenReplace = new ReplaceCondition[]
        {
            new ReplaceCondition(new Regex(@"\\[Nn]"), "\n"),
            new ReplaceCondition(new Regex(@"\\h"), " "),
            new ReplaceCondition(new Regex(@"<\s?br\s?\/?>", RegexOptions.IgnoreCase), "\n"),
            new ReplaceCondition(new Regex(@"[\r\n]+"), "\n"),

            new ReplaceCondition(urlRegex, "<url href=\"$1\">"),

            new ReplaceCondition(" 1/4 ", " ¼ "),
            new ReplaceCondition(" 1/2 ", " ½ "),
            new ReplaceCondition(" 3/4 ", " ¾ "),
            new ReplaceCondition(" 1/3 ", " ⅓ "),
            new ReplaceCondition(" 2/3 ", " ⅔ "),
            new ReplaceCondition(" 1/8 ", " ⅛ "),
            new ReplaceCondition(" 3/8 ", " ⅜ "),
            new ReplaceCondition(" 5/8 ", " ⅝ "),
            new ReplaceCondition(" 7/8 ", " ⅞ "),

            // * xxx * | # xxx # | ¶ xxx ¶ --> ♪ xxx ♪
            new ReplaceCondition(new Regex(@"(^[\s]+[\*#][\s])|([\s][\*#¶][\s]+$)"), " ♪ "),
            // ♪ xxx --> ♪ xxx ♪
            new ReplaceCondition(new Regex(@"^([\s]+♪[\s])([^♪]+)[\s]+$"), "$1$2 ♪ "),
            // xxx ♪ --> ♪ xxx ♪
            new ReplaceCondition(new Regex(@"^([^♪]+)([\s]+♪[\s])$"), " ♪ $1$2"),

            new ReplaceCondition(ToolsConstants.singleQuotes.ToCharArray(), "\'"),
            new ReplaceCondition(ToolsConstants.doubleQuotes.ToCharArray(), "\""),
            new ReplaceCondition(ToolsConstants.commas.ToCharArray(), ","),
            new ReplaceCondition(ToolsConstants.semicolons.ToCharArray(), ";"),
            new ReplaceCondition(ToolsConstants.colons.ToCharArray(), ":"),

            // I 'm --> I'm
            new ReplaceCondition(new Regex(@"([a-zA-Z])[\s]+([\'])([a-zA-Z]+)"), "$1$2$3"),

            // -Hello --> - Hello
            new ReplaceCondition(new Regex(@"([\.\?\'\""\s])-([a-zA-Z])"), "$1- $2"),

            //new ReplaceCondition(new Regex(@"([\.\?\-\'\""`\s])([A-Z])\s?\.\s?([A-Z])\s?\.\s?([A-Z])\s?\.\s?([A-Z])\s?\.\s?([A-Z])\s?\.\s?([A-Z])\s?\.\s?([A-Z])([:;!,\.\?\-\'\""`\s…])"), "$1$2·$3·$4·$5·$6·$7·$8$9"),
            //new ReplaceCondition(new Regex(@"([\.\?\-\'\""`\s])([A-Z])\s?\.\s?([A-Z])\s?\.\s?([A-Z])\s?\.\s?([A-Z])\s?\.\s?([A-Z])\s?\.\s?([A-Z])([:;!,\.\?\-\'\""`\s…])"), "$1$2·$3·$4·$5·$6·$7$8"),
            //new ReplaceCondition(new Regex(@"([\.\?\-\'\""`\s])([A-Z])\s?\.\s?([A-Z])\s?\.\s?([A-Z])\s?\.\s?([A-Z])\s?\.\s?([A-Z])([:;!,\.\?\-\'\""`\s…])"), "$1$2·$3·$4·$5·$6$7"),
            //new ReplaceCondition(new Regex(@"([\.\?\-\'\""`\s])([A-Z])\s?\.\s?([A-Z])\s?\.\s?([A-Z])\s?\.\s?([A-Z])([:;!,\.\?\-\'\""`\s…])"), "$1$2·$3·$4·$5$6"),
            //new ReplaceCondition(new Regex(@"([\.\?\-\'\""`\s])([A-Z])\s?\.\s?([A-Z])\s?\.\s?([A-Z])([:;!,\.\?\-\'\""`\s…])"), "$1$2·$3·$4$5"),

            // 0: 000 --> 0∶000 | 
            new ReplaceCondition(new Regex(@"([0-9]+)\s?[∶:]\s?([0-9]+)"), "$1∶$2"),
            // 0. 000 --> 0·000
            new ReplaceCondition(new Regex(@"([0-9]+)\s?[\.]\s?([0-9]+)"), "$1·$2"),

            // hello"… --> hello…"
            // new ReplaceCondition(new Regex(@"([a-zA-Z]+)([\""])([…])(\s)"), "$1$3$2$4"),
        };
        #endregion

        #region Methods
        internal static bool IsBeforeColon(string text)
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

        internal static bool IsAds(string text)
        {
            var temp = text.ReplaceRegex(@"^[:;!,\.\?\-\'\""\s…]", "");

            bool isMatch = false;
            foreach (var re in ToolsConstants.adMatches)
            {
                isMatch = isMatch || re.IsMatch(temp);
            }
            return isMatch;
        }

        internal static void AddNewLineToken(ref List<Token> result)
        {
            if (result.Count > 0 && result[result.Count - 1].TokenType != TokenTypes.NEW_LINE)
            {
                result.Add(Token.NewLine);
            }
        }

        public static Token[] Tokenize(string input)
        {
            string text = " " + input.Trim().EscapeDot() + " ";
            foreach (var rep in preTokenReplace)
            {
                text = rep.Replace(text);
            }

            text = " " + text.Trim() + " ";
            text = thousandSepRe.Replace(text, (Match match) =>
            {
                return match.Value.Replace(',', '‚');
            });

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

                    speratorCharRe.Split(t).Where(xs => !string.IsNullOrWhiteSpace(xs)).ForEach(xs =>
                    {
                        beforeColonRe.Split(xs.Trim()).Where(xt => !string.IsNullOrWhiteSpace(xt))
                            .ForEach(xt => arr.Add(xt.Trim()));
                    });
                }
            });

            var result = new List<Token>();
            TokenTypes prevType = TokenTypes.EMPTY;

            foreach (var i in arr)
            {
                TokenTypes type = TokenTypes.DIALOUGE;
                var x = i.Trim();

                if (x == "\\n")
                {
                    AddNewLineToken(ref result);
                }
                else
                {
                    x = x.ReplaceRegex(@"[\n]+", "");

                    bool spaceBefore = false;
                    bool spaceAfter = false;

                    if (ssaTagRe.IsMatch(x))
                    {
                        type = TokenTypes.SSA_TAG;
                    }
                    else if (htmlTagRe.IsMatch(x))
                    {
                        type = TokenTypes.HTML_TAG;
                    }
                    else if (x == "-")
                    {
                        AddNewLineToken(ref result);
                        type = TokenTypes.DLG_START;
                    }
                    else if (noneDlgRe.IsMatch(x))
                    {
                        type = TokenTypes.NONE_DLG;
                    }
                    else if (IsBeforeColon(x))
                    {
                        AddNewLineToken(ref result);
                        type = TokenTypes.BEFORE_COLON;
                    }
                    else if (songTagRe.IsMatch(x))
                    {
                        type = TokenTypes.SONG_TAG;
                    }
                    else if (IsAds(x))
                    {
                        type = TokenTypes.ADS;
                    }
                    else
                    {
                        spaceBefore = i.StartsWith(" ");
                        spaceAfter = i.EndsWith(" ");
                    }

                    if (type != TokenTypes.SSA_TAG && type != TokenTypes.HTML_TAG && type != TokenTypes.DLG_START && type != TokenTypes.BEFORE_COLON)
                    {
                        x = urlTagRe.Replace(x, "$1");
                        x = x.UnescapeDot();
                    }

                    if (type == TokenTypes.SSA_TAG && type == TokenTypes.HTML_TAG && type == TokenTypes.ADS)
                    {
                        result.Add(new Token(x, type));
                    }
                    else if (type == TokenTypes.DLG_START || type == TokenTypes.BEFORE_COLON || type == TokenTypes.NONE_DLG)
                    {
                        result.Add(new Token(x, type) { Trailing = " " });
                    }
                    else if (type == TokenTypes.SONG_TAG)
                    {
                        result.Add(new Token(x, type)
                        {
                            Leading = prevType != TokenTypes.EMPTY ? " " : string.Empty,
                            Trailing = " ",
                        });
                    }
                    else
                    {
                        result.Add(new Token(x, type)
                        {
                            Leading = spaceBefore ? " " : string.Empty,
                            Trailing = spaceAfter ? " " : string.Empty,
                        });
                    }
                    prevType = type;
                }
            }

            return result.ToArray();
        }
        #endregion
    }
}
