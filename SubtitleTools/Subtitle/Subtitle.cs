using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SubtitleTools
{
    public partial class Subtitle : S16.Collections.ExtandableList<Dialogue>, ISubtitle
    {
        #region Variables
        private SubtitleHeaders headers = new SubtitleHeaders();
        private Encoding encoding = Encoding.UTF8;

#if MULTIPARSERS
        private static readonly ISubtitleParser[] parsers = new ISubtitleParser[]
        {
            new MicroDVDParser(), 
            new SAMIParser(),
            new SRTParser(),
            new SSAParser(),
            new SubViewerParser(),
            new TTMLParser(),
            new VTTParser(),
            new YtXmlParser()
        };
#endif
        #endregion

        #region Constructors
        public Subtitle()
        { }
        #endregion

        #region Properties
        public virtual Encoding CurrentEncoding
        {
            get => encoding;
            set { encoding = value; }
        }
        
        public virtual SubtitleHeaders Headers
        {
            get => headers;
        }
        #endregion

        #region Methods
        public virtual bool Parse(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;

            Clear();

#if MULTIPARSERS
            foreach (var parser in parsers)
            {
                if (parser.IsSupported(input))
                {
                    var subtitle = (ISubtitle)this;
                    if (parser.Parse(input, ref subtitle))
                    {
                        return true;
                    }
                }
            }

            return false;
#else
            var regex = new Regex(@"(\d+)\n(\d{2}:\d{2}:\d{2},\d{3}) --> (\d{2}:\d{2}:\d{2},\d{3})");
            var lines = input.Trim().ReplaceRegex(@"\r?\n", "\n").Split(regex);

            if (lines.Length < 4) return false;
            lines = lines.Skip(1).ToArray();

            for (int i = 0; i < lines.Length; i += 4)
            {
                Add(new Dialogue(
                    lines[i].Trim(), 
                    lines[i + 1].Trim(), 
                    lines[i + 2].Trim(), 
                    lines[i + 3].Trim()
                ));
            }
            return true;
#endif
        }

        public override string ToString()
        {
            var cues = ToArray();
            if (cues.Length == 0) return string.Empty;
            return cues.Select((c, i) => $"{i + 1}\n{c.Start.Replace('.', ',')} --> {c.End.Replace('.', '.')}\n{c.StyledText}").Join("\n\n") + "\n";
        }

        internal static bool FromFile(string filePath, ref ISubtitle subtitle)
        {
            if (string.IsNullOrEmpty(filePath)) return false;

            var file = new FileInfo(filePath);
            if (!file.Exists) return false;

            try
            {
                using (var reader = new StreamReader(file.OpenRead(), true))
                {
                    string text = reader.ReadToEnd();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        subtitle.CurrentEncoding = reader.CurrentEncoding;
                        text = BOM.RemoveBOM(text, reader.CurrentEncoding);

#if MULTIPARSERS
                        var ext = file.Extension;
                        if (!string.IsNullOrEmpty(ext))
                        {
                            ext = ext.ToLowerInvariant();

                            foreach (var parser in parsers)
                            {
                                if (!string.IsNullOrEmpty(parser.FileExtension))
                                {
                                    var exts = parser.FileExtension.Split('|');
                                    if (Array.IndexOf(exts, ext) > -1 && parser.IsSupported(text))
                                    {
                                        ((ICollection<Dialogue>)subtitle).Clear();
                                        parser.Parse(text, ref subtitle);
                                        return true;
                                    }
                                }
                            }
                        }
#endif
                        subtitle.Parse(text);
                    }
                }

                return true;
            }
            catch (Exception)
            { }

            return false;
        }
        
        public static Subtitle FromFile(string filePath)
        {
            ISubtitle inst = new Subtitle();
            if (!FromFile(filePath, ref inst)) return null;
            return inst as Subtitle;
        }
        #endregion

        #region Nested Types
        private static class BOM
        {
            public static readonly Encoding BigEndianUTF32 = new UTF32Encoding(true, true);

#pragma warning disable SYSLIB0001
            private static readonly int[] hasBomEncodings = new int[]
            {
                System.Text.Encoding.UTF7.CodePage,
                System.Text.Encoding.UTF8.CodePage,
                System.Text.Encoding.UTF32.CodePage,
                BigEndianUTF32.CodePage,
                System.Text.Encoding.Unicode.CodePage,
                System.Text.Encoding.BigEndianUnicode.CodePage,
             };

            public static readonly byte[] UTF7BOM = new byte[] { 0x2B, 0x2F, 0x76 };
            public static readonly byte[] UTF8BOM = new byte[] { 0xEF, 0xBB, 0xBF };
            public static readonly byte[] UTF32BOM = new byte[] { 0xFF, 0xFE, 0, 0 };
            public static readonly byte[] BigEndianUTF32BOM = new byte[] { 0, 0, 0xFE, 0xFF };
            public static readonly byte[] BigEndianUnicodeBOM = new byte[] { 0xFE, 0xFF };
            public static readonly byte[] UnicodeBOM = new byte[] { 0xFF, 0xFE };

            private static readonly byte[][] BOM_MARKS = new byte[][]
            {
                UTF7BOM,
                UTF8BOM,
                UTF32BOM,
                BigEndianUTF32BOM,
                UnicodeBOM,
                BigEndianUnicodeBOM,
            };

            public static bool HasBOM(Encoding encoding)
            {
                return Array.IndexOf(hasBomEncodings, encoding.CodePage) > -1;
            }

            public static bool HasBOM(string text, Encoding encoding)
            {
                if (string.IsNullOrEmpty(text)) return false;

                int encIdx = Array.IndexOf(hasBomEncodings, encoding.CodePage);
                if (encIdx == -1) return false;

                var strStart = text.Substring(0, Math.Min(4, text.Length - 1));

                var startBytes = encoding.GetBytes(strStart);

                var encBOM = BOM_MARKS[encIdx];
                if (startBytes.Length > encBOM.Length)
                {
                    bool flag = true;
                    for (int i = 0; i < encBOM.Length; i++)
                    {
                        flag = flag && (encBOM[i] == startBytes[i]);
                    }

                    return flag;
                }

                return false;
            }

            public static string RemoveBOM(string text, Encoding encoding)
            {
                if (string.IsNullOrEmpty(text)) return text;

                int encIdx = Array.IndexOf(hasBomEncodings, encoding.CodePage);
                if (encIdx == -1) return text;

                var textBytes = encoding.GetBytes(text);

                var encBOM = BOM_MARKS[encIdx];
                if (textBytes.Length > encBOM.Length)
                {
                    int marks = 0;
                    for (int i = 0; i < encBOM.Length; i++)
                    {
                        if (encBOM[i] == textBytes[i]) marks++;
                    }

                    if (marks == encBOM.Length)
                    {
                        byte[] tempBytes = new byte[textBytes.Length - encBOM.Length];
                        Array.Copy(textBytes, marks, tempBytes, 0, textBytes.Length - marks);
                        return encoding.GetString(tempBytes);
                    }
                }

                return text;
            }
#pragma warning restore SYSLIB0001
        }
        #endregion
    }
}
