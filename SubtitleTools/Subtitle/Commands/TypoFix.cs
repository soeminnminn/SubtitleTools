using System;
using System.Collections;
using System.Collections.Generic;

namespace SubtitleTools.Commands
{
    public class TypoFix : ISubtitleCommand, IDialogueCommand
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

            var tokens = dialogue.Tokens;
            var list = new List<Token>();

            for (int i = 0; i < tokens.Length; i++)
            {
                var token = tokens[i];
                if ((TokenTypes.ANY_DLG & token.tokenType) == token.tokenType)
                {
                    foreach (var rep in ToolsConstants.iOrlFixRe)
                    {
                        var val = " " + token.value + " ";
                        val = rep.Replace(val).Trim();

                        token.value = val;
                    }

                    foreach (var rep in ToolsConstants.typoFixRe)
                    {
                        var val = " " + token.value + " ";
                        val = rep.Replace(val).Trim();

                        token.value = val;
                    }
                }
                list.Add(token);
            }
            dialogue.Tokens = list.ToArray();
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
