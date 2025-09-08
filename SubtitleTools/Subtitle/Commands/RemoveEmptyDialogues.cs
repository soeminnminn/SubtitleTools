using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SubtitleTools.Commands
{
    public class RemoveEmptyDialogues : ISubtitleCommand
    {
        #region Methods
        public bool CanExecute(ISubtitle subtitle)
        {
            return ((ICollection)subtitle).Count > 0;
        }

        public void ExecuteDialouge(ref Dialogue dialogue)
        {
        }

        public void PostExecute(ref ISubtitle subtitle)
        {
            var cues = subtitle.Where(x =>
            {
                var text = x.ToString(TokenTypes.ANY_DLG);
                foreach (var sc in ToolsConstants.specialChars)
                {
                    text = text.Replace(sc, "");
                }
                return !string.IsNullOrWhiteSpace(text);
            }).ToArray();

            ((ICollection<Dialogue>)subtitle).Clear();
            foreach (var cue in cues)
            {
                subtitle.Add(cue);
            }
        }

        public void PreExecute(ref ISubtitle subtitle)
        {
        }
        #endregion
    }
}
