using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SubtitleTools.Commands
{
    public class MergeLines : ISubtitleCommand, IDialogueCommand
    {
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

            var text = dialogue.Text;

            if (dialogue.LineCount > 1 && !ToolsConstants.songTagRe.IsMatch(text))
            {
                text = ToolsConstants.mergeLinesRe.Replace(text, "$1 $2");
            }

            var arr = text.SimplifyNewLine().Split('\n').Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            dialogue.Text = string.Join("\n", arr);
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
