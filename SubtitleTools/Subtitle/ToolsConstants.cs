using System;
using System.Text.RegularExpressions;

namespace SubtitleTools
{
    internal static class ToolsConstants
    {
        #region Variables
        internal static readonly byte[] UTF8_BOM = new byte[] { 0xef, 0xbb, 0xbf };

        internal const int MaxLineLength = 40;
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

            new ReplaceCondition(new Regex(@"([:\s\-])(FB[Iil])([:;!\,\.\?\-\'\""`\s…”])"), "$1FBI$3"),
            new ReplaceCondition(new Regex(@"([:\s\-])(C[Iil]A)([:;!\,\.\?\-\'\""`\s…”])"), "$1CIA$3"),
            new ReplaceCondition(new Regex(@"([:\s\-])([aA]\.?[Iil])([:;!\,\.\?\-\'\""`\s…”])"), "$1AI$3"),
            new ReplaceCondition(new Regex(@"([:\s\-])([Iil]\.?[oO])([:;!\,\.\?\-\'\""`\s…”])"), "$1IO$3"),
            new ReplaceCondition(new Regex(@"([:\s\-])(H[Il])([:;!\,\.\?\-\'\""`\s…”])"), "$1HI$3"),

            //new ReplaceCondition(new Regex(@"([\s\""\'``’])([A-Zl]{3,})([:;!,\.\?\-\'\""`\s…”])"), (Match m, string input) => 
            //{
            //    var v = m.Groups[2].Value.Replace('l', 'I').ToUpperInvariant();
            //    return $"{m.Groups[1].Value}{v}{m.Groups[3].Value}";
            //}),
        };

        internal static readonly ReplaceCondition[] typoFixRe = new ReplaceCondition[]
        {
            new ReplaceCondition(new Regex(@"([\s\""\'`’])0h([:;!,\.\?\-\'\""`\s…”])", RegexOptions.IgnoreCase), "$1Oh$2"),

            new ReplaceCondition(new Regex(@"([\.\?\-\'\""``’\s])M([rs]s?)[\s\.]{1,2}", RegexOptions.IgnoreCase), "$1M$2. "),
            new ReplaceCondition(new Regex(@"([\.\?\-\'\""``’\s])Dr[\s\.]{1,2}", RegexOptions.IgnoreCase), "$1Dr. "),
            new ReplaceCondition(new Regex(@"([\.\?\-\'\""``’\s])St[\s\.]{1,2}", RegexOptions.IgnoreCase), "$1St. "),

            new ReplaceCondition(new Regex(@"([0-9]+)\s?([:\-])\s?([0-9]+)"), "$1$2$3"),
            new ReplaceCondition(new Regex(@"([0-9]+)\s([0-9]+)"), "$1$2"),

            new ReplaceCondition(new Regex(@"([a-zA-Z])\s([:;!,\.\?\-\'…])"), "$1$2"),
            // new ReplaceCondition(new Regex(@"([a-zA-Z]+)\""([^\""]+)\s?$"), "$1\"$2\""),
            // new ReplaceCondition(new Regex(@"([a-zA-Z]+)\""([^\""]+)$"), "$1 \"$2"),
            // new ReplaceCondition(new Regex(@"([a-zA-Z]+)\s?\""([^\""]+)\"""), "$1 \"$2\""),

            new ReplaceCondition(new Regex(@"([Ii])['’`]m"), "I`m"),
            new ReplaceCondition(new Regex(@"([Mm]a)['’`]am"), "$1`am"),
            new ReplaceCondition(new Regex(@"([Hh]e|[Ii]|[Ii]t|[Ss]he|[Tt]hey|[Ww]e|[Ww]ho|[Yy]ou)['’`]ll"), "$1`ll"),
            new ReplaceCondition(new Regex(@"([Ii]|[Tt]hey|[Ww]e|[Ww]ho|[Ww]ould|[Yy]ou)['’`]ve"), "$1`ve"),
            new ReplaceCondition(new Regex(@"([Tt]hey|[Ww]e|[Ww]ho|[Yy]ou)['’`]re"), "$1`re"),
            new ReplaceCondition(new Regex(@"([a-zA-Z]{2,})['’`]s"), "$1`s"),
            new ReplaceCondition(new Regex(@"([Aa]i|[Aa]re|[Cc]a|[Dd]id|[Dd]oes|[Dd]o|[Hh]ad|[Hh]as|[Hh]ave|[Ii]s|[Mm]ay|[Nn]eed|[Ss]ha|[Ww]as|[Ww]ere|[Ww]o)n['’`]t"), "$1n`t"),
            new ReplaceCondition(new Regex(@"([Hh]e|[Ii]|[Ii]t|[Ss]he|[Tt]hey|[Ww]e|[Ww]ho|[Yy]ou)['’`]d"), "$1`d"),
            new ReplaceCondition(new Regex(@"([a-zA-Z]{2,})in['’`]"), "$1in`"),
            new ReplaceCondition(new Regex(@"([a-zA-Z\s]+)['’`](cause|bout|em)"), "$1`$2"),
            new ReplaceCondition(new Regex(@"([Cc])['’`]mon"), "$1`mon"),

            new ReplaceCondition(new Regex(@"([A-Za-z]+)(9\)')"), "$1ey"),
            new ReplaceCondition(new Regex(@"([A-Za-z]+)(\)')"), "$1y"),
        };

        internal static readonly Regex newLineRe = new Regex(@"\r?\n");
        internal static readonly Regex trimWhitespaceStartRe = new Regex(@"^[\s]+");
        internal static readonly Regex trimWhitespaceEndRe = new Regex(@"[\s]+$");

        internal static readonly string dotEscape = "·";
        internal static readonly ReplaceCondition[] escapeDotRe = new ReplaceCondition[]
        {
            new ReplaceCondition(new Regex(@"[\._]{2,}"), "…"),
            new ReplaceCondition(new Regex(@"(\. ){2,}\."), "…"),
            new ReplaceCondition(new Regex(@"[‒-―‥]+"), "…"),
            new ReplaceCondition(new Regex(@"([a-zA-Z0-9])[-]{2,}([:;!,\.\?\'\""`\s])"), "$1…$2"),
            new ReplaceCondition(new Regex(@"([a-zA-Z])[-]{2,}"), "$1…"),

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

            new ReplaceCondition(new Regex(@"([A-Z])\.([`']?s)([:;!,\.\?\-\'\""`\s…])"), "·$1·$2$3"),

            new ReplaceCondition(new Regex(@"([0-9]+)\s?([AP])\s?\.\s?(M)([:;!,\.\?\-\'\""`\s…])", RegexOptions.IgnoreCase), "$1$2$3$4"),
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
