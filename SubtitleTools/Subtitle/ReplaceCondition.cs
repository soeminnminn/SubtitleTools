using System;
using System.Text.RegularExpressions;

namespace SubtitleTools
{
    public delegate string ReplaceEvaluator(Match match, string input);

    internal class ReplaceCondition
    {
        #region Variables
        private readonly Regex regex;
        private readonly string replacment;
        private readonly ReplaceEvaluator replacmentFn;
        #endregion

        #region Constructor
        public ReplaceCondition(Regex regex, string replacment) 
        {
            this.regex = regex;
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
            if (string.IsNullOrEmpty(input) || this.regex == null) return false;
            return this.regex.IsMatch(input);
        }

        public string Replace(string input)
        {
            if (string.IsNullOrEmpty(input) || this.regex == null) return input;

            if (this.replacmentFn != null)
            {
                return this.regex.Replace(input, (Match match) => this.replacmentFn(match, input));
            }
            else
            {
                return this.regex.Replace(input, this.replacment);
            }
        }
        #endregion
    }
}
