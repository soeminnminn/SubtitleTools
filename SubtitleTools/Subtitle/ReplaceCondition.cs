using System;
using System.Text.RegularExpressions;

namespace SubtitleTools
{
    public delegate string ReplaceEvaluator(Match match, string input);

    internal class ReplaceCondition
    {
        #region Variables
        private readonly Regex regex;
        private readonly char[] chars;
        private readonly string replacment;
        private readonly ReplaceEvaluator replacmentFn;
        #endregion

        #region Constructor
        public ReplaceCondition(Regex regex, string replacment) 
        {
            this.regex = regex;
            this.chars = null;
            this.replacment = replacment;
            this.replacmentFn = null;
        }

        public ReplaceCondition(string search, string replacment, bool ignoreCase = false)
        {
            if (!string.IsNullOrEmpty(search))
            {
                if (ignoreCase)
                    this.regex = new Regex(search, RegexOptions.IgnoreCase);
                else
                    this.regex = new Regex(search);
            }

            this.chars = null;
            this.replacment = replacment;
            this.replacmentFn = null;
        }

        public ReplaceCondition(char[] chars, string replacment)
        {
            if (chars.Length > 0)
            {
                this.chars = chars;
            }

            this.regex = null;
            this.replacment = replacment;
            this.replacmentFn = null;
        }

        public ReplaceCondition(Regex regex, ReplaceEvaluator replacment)
            : this(regex, string.Empty)
        {
            this.replacmentFn = replacment;
        }

        public ReplaceCondition(string search, ReplaceEvaluator replacment, bool ignoreCase = false)
            : this(search, string.Empty, ignoreCase)
        {
            this.replacmentFn = replacment;
        }
        #endregion

        #region Methods
        public bool IsMatch(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            if (this.regex != null)
            {
                return this.regex.IsMatch(input);
            }
            else if (this.chars != null)
            {
                foreach (char c in this.chars)
                {
                    if (input.Contains(c.ToString()))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public string Replace(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            if (this.replacmentFn != null && this.regex != null)
            {
                return this.regex.Replace(input, (Match match) => this.replacmentFn(match, input));

            }
            else if (this.regex != null)
            {
                return this.regex.Replace(input, this.replacment);
            }
            else if (this.chars != null)
            {
                string temp = input;
                foreach (char c in this.chars)
                {
                    string chStr = c.ToString();
                    temp = temp.Replace(chStr, this.replacment);
                }

                return temp;
            }

            return input;
        }
        #endregion
    }
}
