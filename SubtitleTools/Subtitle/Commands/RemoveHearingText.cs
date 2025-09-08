using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;

namespace SubtitleTools.Commands
{
    public class RemoveHearingText : ISubtitleCommand, IDialogueCommand
    {
        #region Variables
        
        #endregion

        #region Methods
        public bool CanExecute(ISubtitle subtitle)
        {
            return ((ICollection)subtitle).Count > 0;
        }

        public bool CanExecute(Dialogue dialogue)
        {
            return dialogue != null && dialogue.Tokens != null && dialogue.Tokens.Length > 0;
        }

        public void Execute(ref Dialogue dialogue)
        {
            if (dialogue.Tokens == null) return;
            if (dialogue.Tokens.Length == 0) return;

            var ads = dialogue.Tokens.Count(t => t.tokenType.HasFlag(TokenTypes.ADS));
            if (ads > 0)
            {
                dialogue.Text = string.Empty;
                return;
            }

            var text = dialogue.ToString(TokenTypes.DLG_START | TokenTypes.DIALOUGE | TokenTypes.NEW_LINE | TokenTypes.SONG_TAG);

            string temp = Regex.Replace(text, @"\r?\n", "");
            foreach (var ch in ToolsConstants.specialChars)
            {
                temp = temp.Replace(ch, "");
            }

            if (temp.Trim().Length == 0)
                dialogue.Text = string.Empty;
            else
            {
                text = text.Trim();

                var arr = text.SplitRegex(@"\r?\n");
                string[] lines = new string[arr.Length];
                for (var i = 0; i < arr.Length; i++)
                {
                    var l = arr[i];
                    if (string.IsNullOrWhiteSpace(l)) continue;

                    string[] words = l.Split(' ');
                    if (words.Length > 1)
                    {
                        if (words[0] == "-" && Array.IndexOf(ToolsConstants.skipChars, words[1]) > -1)
                            words[1] = "";
                        else if (Array.IndexOf(ToolsConstants.skipChars, words[0]) > -1)
                            words[0] = "";
                    }

                    lines[i] = words.Join(" ").ReplaceRegex(@"[ ]+", " ");
                }

                dialogue.Text = lines.Where(x => !string.IsNullOrWhiteSpace(x)).Join("\r\n");
            }
        }

        public void ExecuteDialouge(ref Dialogue dialogue)
            => Execute(ref dialogue);

        public void PostExecute(ref ISubtitle subtitle)
        {
        }

        public void PreExecute(ref ISubtitle subtitle)
        {
        }
        #endregion
    }
}
