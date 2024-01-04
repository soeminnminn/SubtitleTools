using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SubtitleTools
{
    public partial class ObservableSubtitle : ObservableCollection<Dialogue>, ISubtitle
    {
        #region Variables
        private Encoding encoding = Encoding.UTF8;
        #endregion

        #region Constructors
        public ObservableSubtitle()
        { }
        #endregion

        #region Properties
        public Encoding CurrentEncoding
        {
            get => encoding;
            set { encoding = value; }
        }
        #endregion

        #region Methods
        public bool Parse(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;

            Clear();

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
        }

        public void Renumber()
        {
            if (Count > 0)
            {
                for(var i = 0; i < Count; i++)
                {
                    this[i].Id = $"{i+1}";
                }
            }
        }

        public override string ToString()
        {
            var cues = this.ToArray();
            if (cues.Length == 0) return string.Empty;
            return cues.Select((c, i) => $"{i + 1}\n{c.Start.Replace('.', ',')} --> {c.End.Replace('.', '.')}\n{c.StyledText}").Join("\n\n") + "\n";
        }

        public static ObservableSubtitle FromFile(string filePath)
        {
            ISubtitle inst = new ObservableSubtitle();
            if (!Subtitle.FromFile(filePath, ref inst)) return null;
            return inst as ObservableSubtitle;
        }
        #endregion
    }
}
