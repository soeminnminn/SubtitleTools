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
    }
}
