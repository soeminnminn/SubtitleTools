using System;
using System.Collections.Generic;

namespace SubtitleTools
{
    public class SubtitleHeaders
    {
        #region Variables
        private readonly Dictionary<string, string> meta = new Dictionary<string, string>();
        private string head = string.Empty;
        private string styles = string.Empty;
        #endregion

        #region Constructors
        internal SubtitleHeaders()
        { }
        #endregion

        #region Properties
        public Dictionary<string, string> Meta
        {
            get => meta;
        }

        public string Head
        {
            get => head;
            set { head = value; }
        }

        public string Styles
        {
            get => styles;
            set { styles = value; }
        }
        #endregion
    }
}
