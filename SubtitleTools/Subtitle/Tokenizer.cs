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

    public struct Token
    {
        #region Variabels
        public string value;
        public TokenTypes tokenType;
        #endregion

        #region Constructors
        public Token(string val, TokenTypes type)
        {
            value = val;
            tokenType = type;
        }
        #endregion

        #region Methods
        public override string ToString()
        {
            return $"Type={tokenType}, Text=\"{value}\"";
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
        internal static readonly Regex songTagRe = new Regex(@"[¶\u226a\u226b]+");
        internal static readonly Regex speratorCharRe = new Regex(@"([^\.\?!;,\u2026]*[\.\?!;,\u2026]+)");

        internal static readonly Regex urlRegex = new Regex(@"(\b(https?|ftp|file):\/\/[-A-Z0-9+&@#\/%?=~_|!:,.;]*[-A-Z0-9+&@#\/%=~_|])", RegexOptions.IgnoreCase);
        internal static readonly Regex urlTagRe = new Regex(@"<url href=\""([^\""]+)\"">");

        internal static readonly Regex thousandSepRe = new Regex(@"([\d]{1,3})+([,]\s?[\d]{3})*([\.][\d]*)?");

        internal static readonly Regex[] adMatches = new Regex[]
        {
            new Regex(@"^Fixed (and|&) Synced by", RegexOptions.IgnoreCase),
            new Regex(@"^Sub by", RegexOptions.IgnoreCase),
            new Regex(@"^Improved By", RegexOptions.IgnoreCase),
            new Regex(@"^Subtitles by", RegexOptions.IgnoreCase),
            new Regex(@"^Created (and|&) Encoded by", RegexOptions.IgnoreCase),
            new Regex(@"^Re-Sync (and|&) Improved By", RegexOptions.IgnoreCase),
            new Regex(@"^Synced (and|&) corrected by", RegexOptions.IgnoreCase),
            new Regex(@"^English [\-] [A-Z]+", RegexOptions.IgnoreCase),
            new Regex(@"^Advertise your product", RegexOptions.IgnoreCase),
            new Regex(@"www\.[^\.]+advice.com", RegexOptions.IgnoreCase),
            new Regex(@"^Downloaded from", RegexOptions.IgnoreCase),
            new Regex(@"^Official.*movies site", RegexOptions.IgnoreCase),
            new Regex(@"movie info.*file", RegexOptions.IgnoreCase),

            new Regex(@"\b(caption(s|ed)?|subtitl(e|ed|es|ing)|fixed(?!-)|(re-?)?synch?(?!-)(ed|ro(nized)?)?|rip(ped)?(?!-)|translat(e|ed|ion|ions)|correct(ions|ed)|transcri(be|bed|pt|ption|ptions)|improve(d|ments)|subs|provided|encoded|edit(ed|s)?)\W*(by|from)?\W*(:|;)..", RegexOptions.IgnoreCase),
            new Regex(@"^present(s|ing)?:$", RegexOptions.IgnoreCase),
        };

        internal static readonly string[] inColoned = new string[]
        {
            "ALL", "ANCHOR", "ANGENT", "BOTH", "BOY", "COMMENTATOR", "COMPUTER", "FEMALE", "GIRL",
            "KID", "LOUDSPEAKER", "MAID", "MALE", "MAN", "MEN", "MUSIC", "NARRATOR", "PHONE",
            "RADIO", "RECORDING", "REPORTER", "SENIOR", "SOLDER", "SONG", "STAMMERS", "TV",
            "TEACHER", "VOICE", "WHISPERS", "WOMAN",
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
            
            //// song ' * xxx *', ' # xxx #'
            //@"\s[\*#]+",
            
            // dialog start '- xx'
            @"\s-\s",

            // new line
            @"\n"
        };

        internal static readonly Regex tokenRe = new Regex($"({tokenReParts.Join("|")})", RegexOptions.Compiled);

        internal static readonly (Regex, string)[] preTokenRe = new (Regex, string)[]
        {
            (new Regex(@"[\._]{2,}"), "…"),
            (new Regex(@"(\. ){2,}\."), "…"),
            (new Regex(@"[\u2012-\u2015\u2025]+"), "…"),
            (new Regex(@"([a-zA-Z])[-]{2,}"), "$1…"),

            (new Regex(@"\\[Nn]"), "\n"),
            (new Regex(@"\\h"), " "),
            (new Regex(@"<\s?br\s?\/?>", RegexOptions.IgnoreCase), "\n"),
            (new Regex(@"[\r\n]+"), "\n"),

            (urlRegex, "<url href=\"$1\">"),

            (new Regex(@"([\u201C\u201D\u201F\u2033\""]+)"), "\""),

            (new Regex(@"([\u2018\u2019\u201B\u2032\'`]+)"), "'"),

            (new Regex(@"([\.\?\'\""\s])-([a-zA-Z])"), "$1- $2"),

            (new Regex(@"([\.\?\-\'\""\s])M([rs]s?)\s?\.", RegexOptions.IgnoreCase), "$1M$2·"),
            (new Regex(@"([\.\?\-\'\""\s])(Dr)\s?\.", RegexOptions.IgnoreCase), "$1Dr·"),
            (new Regex(@"([\.\?\-\'\""\s])(St)\s?\.", RegexOptions.IgnoreCase), "$1St·"),
            (new Regex(@"([\.\?\-\'\""\s])(Jr?)\s?\.", RegexOptions.IgnoreCase), "$1$2·"),

            (new Regex(@"([\.\?\-\'\""`\s])([A-Z])\s?\.\s?([A-Z])\s?\.\s?([A-Z])\s?\.\s?([A-Z])\s?\.\s?([A-Z])\s?\.\s?([A-Z])\s?\.\s?([A-Z])([:;!,\.\?\-\'\""`\s\u2026])"), "$1$2·$3·$4·$5·$6·$7·$8$9"),
            (new Regex(@"([\.\?\-\'\""`\s])([A-Z])\s?\.\s?([A-Z])\s?\.\s?([A-Z])\s?\.\s?([A-Z])\s?\.\s?([A-Z])\s?\.\s?([A-Z])([:;!,\.\?\-\'\""`\s\u2026])"), "$1$2·$3·$4·$5·$6·$7$8"),
            (new Regex(@"([\.\?\-\'\""`\s])([A-Z])\s?\.\s?([A-Z])\s?\.\s?([A-Z])\s?\.\s?([A-Z])\s?\.\s?([A-Z])([:;!,\.\?\-\'\""`\s\u2026])"), "$1$2·$3·$4·$5·$6$7"),
            (new Regex(@"([\.\?\-\'\""`\s])([A-Z])\s?\.\s?([A-Z])\s?\.\s?([A-Z])\s?\.\s?([A-Z])([:;!,\.\?\-\'\""`\s\u2026])"), "$1$2·$3·$4·$5&6"),
            (new Regex(@"([\.\?\-\'\""`\s])([A-Z])\s?\.\s?([A-Z])\s?\.\s?([A-Z])([:;!,\.\?\-\'\""`\s\u2026])"), "$1$2·$3·$4$5"),

            (new Regex(@"([0-9]+)\s?([AP])\s?\.\s?(M)", RegexOptions.IgnoreCase), "$1$2$3"),
            (new Regex(@"([0-9]+)\s?\.\s?([0-9]+)"), "$1·$2"),
            (new Regex(@"([0-9]+)\s?:\s?([0-9]+)"), "$1∶$2"),
            // (new Regex(@"([a-zA-Z]+)\s?\""([^\""]+)\"""), "$1 “$2”"),
            (new Regex(@"([a-zA-Z]+)([;!,\.\?])([\""”])(\s)"), "$1$3$2$4"),
        };

        internal static readonly (string, string)[] preTokenReplace = new (string, string)[]
        {
            ("1/4", "¼"), ("1/2", "½"), ("3/4", "¾"), ("1/3", "⅓"), ("2/3", "⅔"), ("1/8", "⅛"), ("3/8", "⅜"), ("5/8", "⅝"), ("7/8", "⅞")
        };
        #endregion

        #region Methods
        internal static bool IsBeforeColon(string text)
        {
            if (beforeColonRe.IsMatch(text))
            {
                if (Regex.IsMatch(text, @"^[0-9:;!,\.\?\-\'\""\s\u2026]+$")) return false;

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
            var temp = text.ReplaceRegex(@"^[:;!,\.\?\-\'\""\s\u2026]", "");

            bool isMatch = false;
            foreach (var re in adMatches)
            {
                isMatch = isMatch || re.IsMatch(temp);
            }
            return isMatch;
        }

        internal static void AddNewLineToken(ref List<Token> result)
        {
            if (result.Count > 0 && result[result.Count - 1].tokenType != TokenTypes.NEW_LINE)
            {
                result.Add(new Token("\n", TokenTypes.NEW_LINE));
            }
        }

        public static Token[] Tokenize(string input)
        {
            string text = " " + input.Trim() + " ";
            foreach (var rep in preTokenRe)
            {
                text = rep.Item1.Replace(text, rep.Item2);
            }

            text = " " + text.Trim() + " ";
            text = thousandSepRe.Replace(text, (Match match) =>
            {
                return match.Value.Replace(',', '‚');
            });

            foreach (var rep in preTokenReplace)
            {
                text = text.Replace(rep.Item1, rep.Item2);
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

                    speratorCharRe.Split(t).Where(xs => !string.IsNullOrWhiteSpace(xs)).ForEach(xs =>
                    {
                        beforeColonRe.Split(xs.Trim()).Where(xt => !string.IsNullOrWhiteSpace(xt))
                            .ForEach(xt => arr.Add(xt.Trim()));
                    });
                }
            });

            var result = new List<Token>();

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

                    if (ssaTagRe.IsMatch(x))
                        type = TokenTypes.SSA_TAG;
                    else if (htmlTagRe.IsMatch(x))
                        type = TokenTypes.HTML_TAG;
                    else if (x == "-")
                    {
                        AddNewLineToken(ref result);
                        type = TokenTypes.DLG_START;
                    }
                    else if (noneDlgRe.IsMatch(x))
                        type = TokenTypes.NONE_DLG;
                    else if (IsBeforeColon(x))
                    {
                        AddNewLineToken(ref result);
                        type = TokenTypes.BEFORE_COLON;
                    }
                    else if (songTagRe.IsMatch(x))
                        type = TokenTypes.SONG_TAG;
                    else if (IsAds(x))
                        type = TokenTypes.ADS;

                    x = urlTagRe.Replace(x, "$1");
                    x = x.Replace('·', '.');
                    x = x.Replace('∶', ':');

                    result.Add(new Token(x, type));
                }
            }

            return result.ToArray();
        }
        #endregion
    }
}
