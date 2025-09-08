using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;

namespace SubtitleTools.Commands
{
    public class SplitLine : ISubtitleCommand, IDialogueCommand
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

            var dlgText = dialogue.Text;

            var arr = dlgText.SplitRegex(@"\r?\n").Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

            var list = new string[arr.Length];
            Array.Copy(arr, list, arr.Length);

            if (arr.Length == 1)
            {
                var text = " " + arr.First().EscapeDot() + " ";

                var tArr = Regex.Replace(text, @"([\?\.!…]+)[\s]+", "$1\n").SplitRegex(@"\n").Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
                var tLen = tArr.Length;

                var noneSingle = tArr.Where(x => !Regex.IsMatch(x.Trim(), @"^[\s\?\.!…¶♪♫]+$")).ToArray();

                if (tLen > 1 && noneSingle.Length == 1)
                {
                    list = new string[1];
                    list[0] = tArr[0].Trim();

                    for (int i = 1; i < tArr.Length; i++)
                    {
                        if (Regex.IsMatch(tArr[i].Trim(), @"^[\s\?\.!…]+$"))
                        {
                            list[0] += tArr[i].Trim();
                        }
                        else
                        {
                            list[0] += " " + tArr[i].Trim();
                        }
                    }
                }
                else if (tLen == 2)
                {
                    list = new string[2];
                    list[0] = tArr[0].Trim();
                    list[1] = tArr[1].Trim();
                }
                else if (tLen > 2)
                {
                    list = new string[2];
                    list[0] = tArr[0].Trim() + " " + tArr[1].Trim();

                    for (int i = 2; i < tArr.Length; i++)
                    {
                        list[1] += " " + tArr[i].Trim();
                    }
                }
            }

            dialogue.Text = list.Join("\n").UnescapeDot();
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
