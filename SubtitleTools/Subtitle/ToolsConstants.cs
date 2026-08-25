using System;
using System.Text.RegularExpressions;

namespace SubtitleTools
{
    internal static class ToolsConstants
    {
        #region Variables
        internal static readonly byte[] UTF8_BOM = new byte[] { 0xef, 0xbb, 0xbf };

        internal const int MaxLineLength = 43;
        internal const int MaxLineCount = 2;

        internal static readonly string[] specialChars = new string[] {
            "¶", "♪", "♫", "…", "'", "\"", "-", "+", "=", "_",
            "{", "[", "}", "]", "\\", "|", ":", ";", "<", ",",
            ">", ".", "?", "/", "`", "~", "!", "@", "#", "$",
            "%", "^", "&", "*", "(", ")"
        };

        internal static readonly string[] skipChars = new string[] {
            ":", ",", ".", "?"
        };

        internal const string singleQuotes = "\u0027\u0060\u00B4\u02B9\u02BB\u02BC\u02BD\u02BE\u02BF\u02C8\u02CA\u02CB\u0300\u0301\u0309\u030D\u031B\u0312\u0313\u0314\u0315\u0340\u0341\u0343\u0351\u0357\u0374\u0384\u0559\u055A\u055B\u055D\u1FEF\u1FFD\u1FFE\u2018\u2019\u201B\u2032";
        internal const string doubleQuotes = "\u0022\u02BA\u02DD\u02EE\u030B\u030E\u030F\u201C\u201D\u201F\u2033";
        internal const string commas = "\u002C\u00B8\u02CF\u02DB\u0316\u0317\u031C\u0321\u0322\u0326\u0327\u0328\u0329\u0339\u0375\u201A";
        internal const string semicolons = "\u003B\u037E";
        internal const string colons = "\u003A\u02D0\u02F8\u0589\u05C3";

        internal static readonly ReplaceCondition[] iOrlFixRe = new ReplaceCondition[]
        {
            new ReplaceCondition(new Regex(@"([a-z])['`’]II([:;!,\.\?\-\'\""\s…”])"), "$1`ll$2"),
            new ReplaceCondition(new Regex(@"([\s\""\'``’])[IilL]['’`]m([:;!,\.\?\-\'\""\s…”])"), "$1I`m$2"),

            new ReplaceCondition(new Regex(@"([a-zA-Z])I([aeioudy])"), "$1l$2"),
            new ReplaceCondition(new Regex(@"([\s\""\'``’])I([aeioudy][a-z]+)"), "$1l$2"),
            new ReplaceCondition(new Regex(@"([a-zA-Z])I([:;!,\.\?\-\'\""`\s…”])"), "$1l$2"),

            new ReplaceCondition(new Regex(@"([\s\""\'``’])[lL]([adefnostuv])([:;!,\.\?\-\'\""`\s…”])"), "$1I$2$3"),
            new ReplaceCondition(new Regex(@"([\s\""\'``’])[il]([:;!,\.\?\-\'\""`\s…”])"), "$1I$2"),
            new ReplaceCondition(new Regex(@"([\s\""\'``’])[Iil]am([:;!,\.\?\-\'\""`\s…”])"), "$1I am$2"),

            new ReplaceCondition(new Regex(@"([:\s\-])[Iil]{2}([:;!,\.\?\-\'\""`\s…”])"), "$1II$2"),
            new ReplaceCondition(new Regex(@"([:\s\-])[Ii]{3}([:;!,\.\?\-\'\""`\s…”])"), "$1III$2"),
            new ReplaceCondition(new Regex(@"([:\s\-])[Iil]([VX]+)([:;!,\.\?\-\'\""`\s…”])"), "$1I$2$3"),
            new ReplaceCondition(new Regex(@"([:\s\-])[Iil]{2}([VX]+)([:;!,\.\?\-\'\""`\s…”])"), "$1II$2$3"),
            new ReplaceCondition(new Regex(@"([:\s\-])[Iil]{3}([VX]+)([:;!,\.\?\-\'\""`\s…”])"), "$1III$2$3"),
            new ReplaceCondition(new Regex(@"([:\s\-])([VX]+)[Iil]([:;!,\.\?\-\'\""`\s…”])"), "$1$2I$3"),
            new ReplaceCondition(new Regex(@"([:\s\-])([VX]+)[Iil]{2}([:;!,\.\?\-\'\""`\s…”])"), "$1$2II$3"),
            new ReplaceCondition(new Regex(@"([:\s\-])([VX]+)[Iil]{3}([:;!,\.\?\-\'\""`\s…”])"), "$1$2III$3"),

            // la
            new ReplaceCondition(new Regex(@"([:\s\-])([Iil]a)([:;!\,\.\?\-\'\""`\s…”])"), "$1la$3"),

            // FBI
            new ReplaceCondition(new Regex(@"([:\s\-])(FB[Iil])([:;!\,\.\?\-\'\""`\s…”])"), "$1FBI$3"),
            // CIA
            new ReplaceCondition(new Regex(@"([:\s\-])(C[Iil]A)([:;!\,\.\?\-\'\""`\s…”])"), "$1CIA$3"),
            // AI
            new ReplaceCondition(new Regex(@"([:\s\-])([aA]\.?[Iil])([:;!\,\.\?\-\'\""`\s…”])"), "$1AI$3"),
            // IO
            new ReplaceCondition(new Regex(@"([:\s\-])([Iil]\.?[oO])([:;!\,\.\?\-\'\""`\s…”])"), "$1IO$3"),
            // HI
            new ReplaceCondition(new Regex(@"([:\s\-])(H[Il])([:;!\,\.\?\-\'\""`\s…”])"), "$1HI$3"),

            //new ReplaceCondition(new Regex(@"([\s\""\'``’])([A-Zl]{3,})([:;!,\.\?\-\'\""`\s…”])"), (Match m, string input) => 
            //{
            //    var v = m.Groups[2].Value.Replace('l', 'I').ToUpperInvariant();
            //    return $"{m.Groups[1].Value}{v}{m.Groups[3].Value}";
            //}),
        };

        internal static readonly ReplaceCondition[] typoFixRe = new ReplaceCondition[]
        {
            //new ReplaceCondition(ToolsConstants.singleQuotes.ToCharArray(), "\'"),
            //new ReplaceCondition(ToolsConstants.doubleQuotes.ToCharArray(), "\""),
            //new ReplaceCondition(ToolsConstants.commas.ToCharArray(), ","),
            //new ReplaceCondition(ToolsConstants.semicolons.ToCharArray(), ";"),
            //new ReplaceCondition(ToolsConstants.colons.ToCharArray(), ":"),

            new ReplaceCondition(new Regex(@"([\s\""\'`’])0h([:;!,\.\?\-\'\""`\s…”])", RegexOptions.IgnoreCase), "$1Oh$2"),

            new ReplaceCondition(new Regex(@"([\.\?\-\'\""``’\s])M([rs]s?)[\s\.]{1,2}", RegexOptions.IgnoreCase), "$1M$2. "),
            new ReplaceCondition(new Regex(@"([\.\?\-\'\""``’\s])Dr[\s\.]{1,2}", RegexOptions.IgnoreCase), "$1Dr. "),
            new ReplaceCondition(new Regex(@"([\.\?\-\'\""``’\s])St[\s\.]{1,2}", RegexOptions.IgnoreCase), "$1St. "),

            new ReplaceCondition(new Regex(@"([0-9]+)\s?([:\-])\s?([0-9]+)"), "$1$2$3"),
            new ReplaceCondition(new Regex(@"([0-9]+)\s([0-9]+)"), "$1$2"),

            new ReplaceCondition(new Regex(@"([a-zA-Z])\s([:;!,\.\?\-\'…])"), "$1$2"),

            new ReplaceCondition(new Regex(@"([A-Za-z]+)(9\)')"), "$1ey"),
            new ReplaceCondition(new Regex(@"([A-Za-z]+)(\)')"), "$1y"),

            new ReplaceCondition(new Regex(@"([\.\?\-\'\""`\s])1/4([:;!,\.\?\-\'\""`\s…])"), "$1¼$2"),
            new ReplaceCondition(new Regex(@"([\.\?\-\'\""`\s])1/2([:;!,\.\?\-\'\""`\s…])"), "$1½$2"),
            new ReplaceCondition(new Regex(@"([\.\?\-\'\""`\s])3/4([:;!,\.\?\-\'\""`\s…])"), "$1¾$2"),
            new ReplaceCondition(new Regex(@"([\.\?\-\'\""`\s])1/3([:;!,\.\?\-\'\""`\s…])"), "$1⅓$2"),
            new ReplaceCondition(new Regex(@"([\.\?\-\'\""`\s])2/3([:;!,\.\?\-\'\""`\s…])"), "$1⅔$2"),
            new ReplaceCondition(new Regex(@"([\.\?\-\'\""`\s])1/8([:;!,\.\?\-\'\""`\s…])"), "$1⅛$2"),
            new ReplaceCondition(new Regex(@"([\.\?\-\'\""`\s])3/8([:;!,\.\?\-\'\""`\s…])"), "$1⅜$2"),
            new ReplaceCondition(new Regex(@"([\.\?\-\'\""`\s])5/8([:;!,\.\?\-\'\""`\s…])"), "$1⅝$2"),
            new ReplaceCondition(new Regex(@"([\.\?\-\'\""`\s])7/8([:;!,\.\?\-\'\""`\s…])"), "$1⅞$2"),
        };

        internal static readonly ReplaceCondition[] apostropheFixRe = new ReplaceCondition[]
        {
            new ReplaceCondition(new Regex(@"[Ii]['’`](m|am|ll|ve|s|d)([:;!\,\.\?\-\'\""`\s…”])"), "I'$1$2"),
            new ReplaceCondition(new Regex(@"([Hh]e|I|[Ii]t|[Ss]he|[Tt]hey|[Ww]e|[Ww]ho|[Yy]ou)['’`]ll([:;!\,\.\?\-\'\""`\s…”])"), "$1'll$2"),
            new ReplaceCondition(new Regex(@"(I|[Tt]hey|[Ww]e|[Ww]ho|[Ww]ould|[Yy]ou)['’`]ve([:;!\,\.\?\-\'\""`\s…”])"), "$1've$2"),
            new ReplaceCondition(new Regex(@"([Tt]hey|[Ww]e|[Ww]ho|[Yy]ou)['’`]re([:;!\,\.\?\-\'\""`\s…”])"), "$1're$2"),
            new ReplaceCondition(new Regex(@"([Aa]i|[Aa]re|[Cc]a|[Dd]id|[Dd]oes|[Dd]o|[Hh]ad|[Hh]as|[Hh]ave|[Ii]s|[Mm]ay|[Nn]eed|[Ss]ha|[Ww]as|[Ww]ere|[Ww]o)n['’`]t([:;!\,\.\?\-\'\""`\s…”])"), "$1n't$2"),
            new ReplaceCondition(new Regex(@"([Hh]e|I|[Ii]t|[Ss]he|[Tt]hey|[Ww]e|[Ww]ho|[Yy]ou)['’`]d([:;!\,\.\?\-\'\""`\s…”])"), "$1'd$2"),
            
            new ReplaceCondition(new Regex(@"([a-zA-Z]{2,})['’`]s([:;!\,\.\?\-\'\""`\s…”])"), "$1's$2"),
            new ReplaceCondition(new Regex(@"([a-zA-Z]{2,})in['’`]([:;!\,\.\?\-\'\""`\s…”])"), "$1in'$2"),
            new ReplaceCondition(new Regex(@"([a-zA-Z\s]+)['’`](cause|bout|em)([:;!\,\.\?\-\'\""`\s…”])"), "$1'$2$3"),
            
            new ReplaceCondition(new Regex(@"([Mm]a)['’`]am([:;!\,\.\?\-\'\""`\s…”])"), "$1'am$2"),
            new ReplaceCondition(new Regex(@"([Cc])['’`]mon([:;!\,\.\?\-\'\""`\s…”])"), "$1'mon$2"),
        };

        internal static readonly Regex newLineRe = new Regex(@"\r\n|\r|\n");
        internal static readonly Regex trimWhitespaceStartRe = new Regex(@"^[\s]+");
        internal static readonly Regex trimWhitespaceEndRe = new Regex(@"[\s]+$");

        internal static readonly Regex songTagRe = new Regex(@"[♪♫]+");

        internal static readonly string dotEscape = "·";
        internal static readonly ReplaceCondition[] escapeDotRe = new ReplaceCondition[]
        {
            new ReplaceCondition(new Regex(@"[\._]{2,}"), "…"),
            new ReplaceCondition(new Regex(@"(\. ){2,}\."), "…"),
            new ReplaceCondition(new Regex(@"[‒-―‥]+"), "…"),
            new ReplaceCondition(new Regex(@"([a-zA-Z0-9])[-]{2,}([:;!,\.\?\'\""`\s])"), "$1…$2"),
            new ReplaceCondition(new Regex(@"([a-zA-Z])[-]{2,}"), "$1…"),
            new ReplaceCondition(new Regex(@"[…\.]{2,}"), "…"),

            new ReplaceCondition(new Regex(@"([\.\?\-\'\""\s])M([rs]s?)\s?\.", RegexOptions.IgnoreCase), "$1M$2·"),
            new ReplaceCondition(new Regex(@"([\.\?\-\'\""\s])(Dr)\s?\.", RegexOptions.IgnoreCase), "$1Dr·"),
            new ReplaceCondition(new Regex(@"([\.\?\-\'\""\s])(St)\s?\.", RegexOptions.IgnoreCase), "$1St·"),
            new ReplaceCondition(new Regex(@"([\.\?\-\'\""\s])(Jr?)\s?\.", RegexOptions.IgnoreCase), "$1$2·"),
            new ReplaceCondition(new Regex(@"([\.\?\-\'\""`\s])(S)\.([A-Z])([A-Za-z]+)([:;!,\.\?\-\'\""`\s…])"), "$1$2·$3$4$5"),

            new ReplaceCondition(new Regex(@"([\.\?\-\'\""`\s])([A-Z])\.([A-Z])\.([A-Z])\.([A-Z])\.([A-Z])\.([A-Z])\.([A-Z])([:;!,\.\?\-\'\""`\s…])"), "$1$2·$3·$4·$5·$6·$7·$8$9"),
            new ReplaceCondition(new Regex(@"([\.\?\-\'\""`\s])([A-Z])\.([A-Z])\.([A-Z])\.([A-Z])\.([A-Z])\.([A-Z])([:;!,\.\?\-\'\""`\s…])"), "$1$2·$3·$4·$5·$6·$7$8"),
            new ReplaceCondition(new Regex(@"([\.\?\-\'\""`\s])([A-Z])\.([A-Z])\.([A-Z])\.([A-Z])\.([A-Z])([:;!,\.\?\-\'\""`\s…])"), "$1$2·$3·$4·$5·$6$7"),
            new ReplaceCondition(new Regex(@"([\.\?\-\'\""`\s])([A-Z])\.([A-Z])\.([A-Z])\.([A-Z])([:;!,\.\?\-\'\""`\s…])"), "$1$2·$3·$4·$5$6"),
            new ReplaceCondition(new Regex(@"([\.\?\-\'\""`\s])([A-Z])\.([A-Z])\.([A-Z])([:;!,\.\?\-\'\""`\s…])"), "$1$2·$3·$4$5"),
            new ReplaceCondition(new Regex(@"([\.\?\-\'\""`\s])([A-Z])\.([A-Z])([:;!,\.\?\-\'\""`\s…])"), "$1$2·$3$4"),

            new ReplaceCondition(new Regex(@"([A-Z])\.([`']?s)([:;!,\.\?\-\'\""`\s…])"), "$1·$2$3"),

            new ReplaceCondition(new Regex(@"([0-9:]+)\s?([Aa][\s\.]*[Mm])([:;!,\.\?\-\'\""`\s…])"), "$1 AM$3"),
            new ReplaceCondition(new Regex(@"([0-9:]+)\s?([Pp][\s\.]*[Mm])([:;!,\.\?\-\'\""`\s…])"), "$1 PM$3"),

            new ReplaceCondition(new Regex(@"([0-9]+)\s?\.\s?([0-9]+)"), "$1·$2")
        };

        internal static readonly Regex mergeLinesRe = new Regex(@"([^}>\""\-\.\?!0-9\s])\r?\n([^\s])");

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
        #endregion
    }
}
