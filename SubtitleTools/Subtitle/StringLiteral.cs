using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SubtitleTools
{
    public class StringLiteralMatcher : IEnumerable<StringLiteral>, IEnumerator<StringLiteral>
    {
        #region Variables
        private static readonly char[] quoteChars = { '"', '\'', '`' };

        private readonly string _input;
        private readonly List<MatchData> _matches = new List<MatchData>();
        private int _index = 0;
        private int[] _found = new int[2] { -1, -1 };
        #endregion

        #region Constructor
        public StringLiteralMatcher(string input)
        {
            this._input = input;

            if (!string.IsNullOrEmpty(input))
            {
                for (int i = 0; i < input.Length; i++)
                {
                    char c = input[i];
                    if (Array.IndexOf(quoteChars, c) > -1)
                    {
                        this._matches.Add(new MatchData() 
                        {
                            Value = c.ToString(),
                            Index = i,
                        });
                    }
                }
            }
        }
        #endregion

        #region Properties
        public StringLiteral Current
        {
            get
            {
                if (this._found[0] > -1 && this._found[1] > -1 && this._found[0] < this._found[1])
                {
                    string input = this._input;
                    int index = this._found[0];
                    int len = (this._found[1] - index) + 1;
                    string result = input.Substring(index, len);

                    return new StringLiteral(result, index, input[index]);
                }

                return StringLiteral.Empty;
            }
        }

        object IEnumerator.Current => Current;
        #endregion

        #region Methods
        public IEnumerator<StringLiteral> GetEnumerator()
        {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            this._index = 0;
            this._matches.Clear();
        }

        public bool MoveNext()
        {
            var matches = this._matches;
            var found = new int[2] { -1, -1 };

            for (int i = this._index; i < matches.Count; i++)
            {
                var m = matches[i];
                var nextI = matches.FindIndex(i + 1, (x) => x.Value == m.Value);
                if (nextI > i)
                {
                    found[0] = m.Index;
                    found[1] = matches[nextI].Index;
                    this._index = nextI + 1;
                    break;
                }

                this._index = i;
            }

            if (found[0] > -1 && found[1] > -1)
            {
                this._found[0] = found[0];
                this._found[1] = found[1];
                return true;
            }

            this._found[0] = -1;
            this._found[1] = -1;
            return false;
        }

        public void Reset()
        {
            this._index = 0;
            this._found = new int[2] { -1, -1 };
        }
        
        public StringLiteral[] ToArray()
        {
            var matches = this._matches;
            if (matches.Count == 0) return new StringLiteral[0];

            var input = this._input;
            var result = new List<StringLiteral>();
            var found = new int[2] { -1, -1 };

            int idx = 0;

            while (idx < matches.Count)
            {
                for (int i = idx; i < matches.Count; i++)
                {
                    var m = matches[i];
                    var nextI = matches.FindIndex(i + 1, (x) => x.Value == m.Value);
                    if (nextI > i)
                    {
                        found[0] = m.Index;
                        found[1] = matches[nextI].Index;
                        this._index = nextI + 1;
                        break;
                    }

                    idx = i;
                }

                if (found[0] > -1 && found[1] > -1 && found[0] < found[1])
                {
                    int index = found[0];
                    int len = (found[1] - index) + 1;
                    result.Add(new StringLiteral(input.Substring(index, len), index, input[index]));
                }
            }

            return result.ToArray();
        }

        public static StringLiteralMatcher From(string input)
        {
            return new StringLiteralMatcher(input);
        }
        #endregion

        #region Nested Types
        [StructLayout(LayoutKind.Sequential)]
        private struct MatchData
        {
            public string Value;
            public int Index;
        }
        #endregion
    }

    public class StringLiteral
    {
        #region Variables
        private readonly string _value;
        private int _index = -1;
        private char _quote = '"';
        #endregion

        #region Constructor
        public StringLiteral(string value)
        {
            _value = value;
        }

        public StringLiteral(string value, int index, char quote)
        {   
            _value = value;
            _index = index;
            _quote = quote;
        }
        #endregion

        #region Properties
        internal static StringLiteral Empty
        {
            get => new StringLiteral(string.Empty);
        }

        public string Text
        {
            get => this._value;
        }

        public string Value
        {
            get => this._value.Substring(1, this._value.Length - 2);
        }

        public int Index
        {
            get => this._index;
        }

        public char Quote
        {
            get => this._quote;
        }

        public int Length
        {
            get => this._value.Length;
        }
        #endregion

        #region Methods
        public override string ToString()
        {
            return $"{this._quote}{this._value}{this._quote}";
        }

        public override int GetHashCode()
        {
            return this._value.GetHashCode();
        }
        #endregion

        #region Operators
        public static implicit operator StringLiteral(string value)
        {
            return new StringLiteral(value);
        }

        public static explicit operator string(StringLiteral value)
        {
            return value.ToString();
        }
        #endregion
    }
}
