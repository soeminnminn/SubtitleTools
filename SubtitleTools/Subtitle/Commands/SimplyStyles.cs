using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SubtitleTools.Commands
{
    public class SimplyStyles : ISubtitleCommand, IDialogueCommand
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

            List<TokenTypes> types = new List<TokenTypes>();

            for (int i = 0; i < tokens.Length; i++)
            {
                var token = tokens[i];
                if (token.tokenType == TokenTypes.SSA_TAG)
                {
                    if (Regex.IsMatch(token.value, @"\{i[0-9]?\}"))
                    {
                        types.Add(token.tokenType);
                    }
                    else if (token.value.Contains(@"{\an8}"))
                    {
                        var style = new StyleTag("an", StyleTypes.ssaTag);
                        style.attrs.Add("value", "8");

                        dialogue.Styles.Add(style);
                    }
                    token.tokenType = TokenTypes.EMPTY;
                }
                else if (token.tokenType == TokenTypes.HTML_TAG)
                {
                    if (Regex.IsMatch(token.value, @"<\/?i>", RegexOptions.IgnoreCase))
                    {
                        types.Add(token.tokenType);
                    }
                    token.tokenType = TokenTypes.EMPTY;
                }
                else if (token.tokenType == TokenTypes.ADS || token.tokenType == TokenTypes.DIALOUGE || token.tokenType == TokenTypes.BEFORE_COLON || token.tokenType == TokenTypes.NONE_DLG || token.tokenType == TokenTypes.SONG_TAG)
                {
                    types.Add(token.tokenType);
                }

                if (token.tokenType != TokenTypes.EMPTY)
                    list.Add(token);
            }

            if (types.Count > 2)
            {
                if (types[0] == TokenTypes.SSA_TAG || types[0] == TokenTypes.HTML_TAG)
                {
                    var li = types.Count - 1;
                    if (types[li] == TokenTypes.SSA_TAG || types[li] == TokenTypes.HTML_TAG)
                    {
                        dialogue.Styles.Add(new StyleTag("i", StyleTypes.htmlTag));
                    }
                }
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
