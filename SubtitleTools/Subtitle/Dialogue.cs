using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SubtitleTools
{
    public class Dialogue : INotifyPropertyChanged
    {
        #region Variables
        private string id = string.Empty;
        private double startTime = 0;
        private double duration = 0;
        private string originalText = string.Empty;
        private Token[] tokens = new Token[0];
        private List<StyleTag> styles = new List<StyleTag>();
        #endregion

        #region Event
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region Constructors
        public Dialogue()
        { }

        public Dialogue(double start)
        {
            startTime = start;
        }

        public Dialogue(double start, string text)
        {
            startTime = start;
            Text = text;
        }

        public Dialogue(string id, string start, string end, string text)
        {
            this.id = id;
            var startTime = Utils.TimeMs(start);
            var endTime = Utils.TimeMs(end);

            if (startTime >= 0 && endTime > 0)
            {
                this.startTime = Math.Min(startTime, endTime);
                endTime = Math.Max(startTime, endTime);
                this.duration = endTime - this.startTime;
            }

            this.Text = text;
        }

        public Dialogue(string id, double start, double end, string text)
        {
            this.id = id;
            var startTime = start;
            var endTime = end;

            if (startTime >= 0 && endTime > 0)
            {
                this.startTime = Math.Min(startTime, endTime);
                endTime = Math.Max(startTime, endTime);
                this.duration = endTime - this.startTime;
            }

            this.Text = text;
        }
        #endregion

        #region Properties
        public string Id
        {
            get => id;
            set 
            { 
                id = value;
                OnPropertyChanged();
            }
        }

        public double StartTime
        {
            get => startTime;
            set 
            {
                startTime = value;
                OnPropertyChanged("StartTime", "Start", "EndTime", "End", "Duration");
            }
        }

        public double EndTime
        {
            get => startTime + duration;
            set 
            {
                duration = value - startTime;
                OnPropertyChanged("StartTime", "Start", "EndTime", "End", "Duration");
            }
        }

        public string Start => Utils.MsTime(startTime);

        public string End => Utils.MsTime(EndTime);

        public double Duration
        {
            get => duration;
            set
            {
                duration = value;
                OnPropertyChanged("EndTime", "End", "Duration");
            }
        }

        public string OriginalText => originalText;

        public string Text
        {
            get => ToString(TokenTypes.ALL);
            set
            {
                originalText = string.IsNullOrEmpty(value) ? string.Empty : value;
                tokens = Tokenizer.Tokenize(originalText);
                OnPropertyChanged("Text", "OriginalText", "LineCount", "Length", "Tokens", "Styles", "StyledText");
            }
        }

        public int LineCount
        {
            get
            {
                if (tokens.Length == 0) return 0;
                return tokens.Where(x => x.tokenType.HasFlag(TokenTypes.NEW_LINE)).Count() + 1;
            }
        }

        public int Length
        {
            get
            {
                if (tokens.Length == 0) return 0;
                return ToString(TokenTypes.ALL & ~(TokenTypes.SSA_TAG | TokenTypes.HTML_TAG)).Length;
            }
        }

        public Token[] Tokens
        {
            get => tokens;
            set 
            { 
                tokens = value;
                OnPropertyChanged("Tokens", "Text", "LineCount", "Length", "Styles", "StyledText");
            }
        }

        public List<StyleTag> Styles
        {
            get => styles;
            set { styles = value; }
        }

        public string StyledText
        {
            get
            {
                var text = ToString(TokenTypes.ALL);
                text = Styler.ApplyStyle(text, styles.ToArray());
                return text;
            }
        }

        public int WordsCount
        {
            get
            {
                if (Tokens.Length == 0) return 0;
                var text = ToString(TokenTypes.DIALOUGE);
                if (string.IsNullOrWhiteSpace(text)) return 0;
                text = text.Replace('\t', ' ');

                var words = text.Split(
                    new[] { ' ', ',', ';', '.', '!', '"', '(', ')', '?', ':', '\'', '«', '»', '+', '-' },
                    StringSplitOptions.RemoveEmptyEntries);

                return words.Length;
            }
        }
        #endregion

        #region Methods
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "", params string[] propertyNames)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));

                foreach (var name in propertyNames)
                {
                    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(name));
                }
            }
        }

        public string ToString(TokenTypes options)
        {
            if (tokens.Length == 0) return string.Empty;

            var str = tokens.Select(tok =>
            {
                if ((options & TokenTypes.SSA_TAG) == tok.tokenType)
                    return tok.value;
                else if ((options & TokenTypes.HTML_TAG) == tok.tokenType)
                    return tok.value;
                else if ((options & TokenTypes.NEW_LINE) == tok.tokenType)
                    return "\n";
                else if ((options & TokenTypes.NONE_DLG) == tok.tokenType)
                    return $"{tok.value} ";
                else if ((options & TokenTypes.BEFORE_COLON) == tok.tokenType)
                    return $"{tok.value} ";
                else if ((options & TokenTypes.SONG_TAG) == tok.tokenType)
                    return $"{tok.value} ";
                else if ((options & TokenTypes.DLG_START) == tok.tokenType)
                    return $"\n{tok.value} ";
                else if ((options & tok.tokenType) == tok.tokenType)
                    return $"{tok.value} ";

                return string.Empty;

            }).Join("")
                .ReplaceRegex(@"[\n]{2,}", "\n")
                .ReplaceRegex(@"[ ]{2,}", " ")
                // .ReplaceRegex(@"[ ][\b]", "")
                .ReplaceRegex(@"([0-9])([:\.])\s([0-9])", "$1$2$3");

            return str.Split('\n').Select(x => x.Trim()).Join("\r\n").Trim();
        }

        public override string ToString()
        {
            return ToString(TokenTypes.ALL & ~(TokenTypes.NEW_LINE | TokenTypes.SSA_TAG | TokenTypes.HTML_TAG));
        }

        public Dialogue AddTime(double ms)
        {
            startTime += ms;

            OnPropertyChanged("StartTime", "Start", "EndTime", "End", "Duration");
            
            return this;
        }
        #endregion
    }
}
