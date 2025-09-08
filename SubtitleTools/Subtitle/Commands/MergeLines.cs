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
        internal static int FindCutPoint(string text)
        {
            if (string.IsNullOrEmpty(text)) return -1;
            if (text.Length < ToolsConstants.MaxLineLength) return -1;

            List<string> cutChars = new List<string>() { ". ", "? ", "! ", "… " };

            int halfIdx = (int)Math.Floor(text.Length * 0.5);

            if (halfIdx < ToolsConstants.MaxLineLength)
            {
                string temp = text.EscapeDot();
                temp = Regex.Replace(temp, @"([^\s]),([^\s])", "$1\u05A5$2");

                for (var i = halfIdx; i < ToolsConstants.MaxLineLength && i < temp.Length; i++)
                {
                    var check = temp[i].ToString() + temp[i + 1].ToString();
                    if (cutChars.Contains(check))
                    {
                        return i + 1;
                    }
                }

                int spaceCount = 0;
                int fromEnd = temp.Length - ToolsConstants.MaxLineLength;
                for (var i = halfIdx; i > fromEnd && i > 0; i--)
                {
                    if (temp[i] == ' ')
                    {
                        spaceCount++;
                        if (spaceCount == 3) break;
                    }

                    var check = temp[i].ToString() + temp[i + 1].ToString();
                    if (cutChars.Contains(check))
                    {
                        return i + 1;
                    }
                }
            }

            var listSP = new List<int> 
            {
                text.Substring(0, halfIdx).LastIndexOf(' '),
                text.IndexOf(' ', halfIdx)
            };

            int closestSP = listSP.OrderBy(item => Math.Abs(halfIdx - item)).First();
            if (text[halfIdx] == ' ') closestSP = halfIdx;

            var listCM = new List<int>
            {
                text.Substring(0, halfIdx).LastIndexOf(','),
                text.IndexOf(',', halfIdx)
            };
            int closestCM = listCM.OrderBy(item => Math.Abs(halfIdx - item)).First();

            if (closestCM < closestSP)
            {
                int t = text.Substring(0, closestSP - 1).LastIndexOf(' ');
                if (closestCM == t - 1) return t;
            }
            else if (closestCM > closestSP)
            {
                int t = text.IndexOf(' ', closestSP + 1);
                if (closestCM == t - 1) return t;
            }

            return closestSP;
        }

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

            if (dialogue.LineCount > 1)
            {
                text = ToolsConstants.mergeLinesRe.Replace(text, "$1 $2");
            }

            var arr = text.SplitRegex(@"\r?\n").Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

            var list = new string[arr.Length];
            Array.Copy(arr, list, arr.Length);

            if (arr.Length < ToolsConstants.MaxLineCount)
            {
                for (int i = 0; i < arr.Length; i++)
                {
                    if (arr[i].Length > ToolsConstants.MaxLineLength)
                    {
                        int pt = FindCutPoint(arr[i]);
                        if (pt > 0)
                        {
                            list[i] = arr[i].Insert(pt, "\n").Split('\n').Select(x => x.Trim()).Join("\n");
                        }
                    }
                }
            }

            dialogue.Text = list.Join("\n");
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
