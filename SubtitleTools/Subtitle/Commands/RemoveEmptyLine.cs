using System;
using System.Collections;
using System.Collections.Generic;

namespace SubtitleTools.Commands
{
    public class RemoveEmptyLine : ISubtitleCommand, IDialogueCommand
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
                    if (token.tokenType.HasFlag(TokenTypes.DLG_START))
                    {
                        if (i + 1 == tokens.Length)
                            token.tokenType = TokenTypes.EMPTY;
                        else if (i + 1 < tokens.Length)
                        {
                            var next = tokens[i + 1];
                            if (next.tokenType.HasFlag(TokenTypes.NEW_LINE) || next.tokenType.HasFlag(TokenTypes.DLG_START))
                                token.tokenType = TokenTypes.EMPTY;
                        }
                    }
                    else if (tokens.Length == 1)
                    {
                        string temp = token.value;
                        foreach (var ch in ToolsConstants.specialChars)
                        {
                            temp = temp.Replace(ch, "");
                        }
                        if (temp.Trim().Length == 0)
                            token.tokenType = TokenTypes.EMPTY;
                    }
                }

                if (token.tokenType != TokenTypes.EMPTY)
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
