using System;
using System.Collections;
using System.Text.RegularExpressions;

namespace SubtitleTools.Commands
{
    public class SplitLine : ISubtitleCommand, IDialogueCommand
    {
        #region Variables
        #endregion

        #region Methods
        private static int FindCutPoint(string text, int maxLineLen)
        {
            if (string.IsNullOrEmpty(text)) return -1;
            if (text.Length < maxLineLen) return -1;

            List<string> cutChars = new List<string>() { ". ", "? ", "! ", "… " };

            int halfIdx = (int)Math.Floor(text.Length * 0.5);

            if (halfIdx < maxLineLen)
            {
                string temp = text.EscapeDot();
                temp = Regex.Replace(temp, @"([^\s]),([^\s])", "$1\u05A5$2");

                for (var i = halfIdx; i < maxLineLen && i < (temp.Length - 1); i++)
                {
                    var check = temp[i].ToString() + temp[i + 1].ToString();
                    if (cutChars.Contains(check))
                    {
                        return i + 1;
                    }
                }

                int spaceCount = 0;
                int fromEnd = temp.Length - maxLineLen;
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

            var dlgText = dialogue.Text;

            var arr = dlgText.SplitRegex(@"\r?\n").Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

            var list = new string[arr.Length];
            Array.Copy(arr, list, arr.Length);

            if (arr.Length == 1)
            {
                var escaped = arr.First().EscapeDot();
                var text = " " + escaped + " ";

                var tArr = Regex.Replace(text, @"([\?\.!…]+)[\s]+", "$1\n").SplitRegex(@"\n").Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
                var tLen = tArr.Length;

                var noneSingle = tArr.Where(x => !Regex.IsMatch(x.Trim(), @"^[\s\?\.!…¶♪♫\""]+$")).ToArray();
                var cutPoint = FindCutPoint(escaped, ToolsConstants.MaxLineLength);

                if (tLen == 1 && cutPoint > 0)
                {
                    list = new string[2];
                    list[0] = escaped.Substring(0, cutPoint).Trim();
                    list[1] = escaped.Substring(cutPoint).Trim();
                }
                else if (tLen > 1)
                {
                    if (noneSingle.Length == 1)
                    {
                        list = new string[1];
                        list[0] = tArr[0].Trim();

                        for (int i = 1; i < tArr.Length; i++)
                        {
                            if (Regex.IsMatch(tArr[i].Trim(), @"^[\s\?\.!…]+$"))
                                list[0] += tArr[i].Trim();
                            else
                                list[0] += " " + tArr[i].Trim();
                        }
                    }
                    else if (noneSingle.Length > 1)
                    {
                        list = new string[2];
                        list[0] = string.Empty;
                        list[1] = string.Empty;

                        if (noneSingle.Length == 2)
                        {
                            list[0] = tArr[0].Trim();
                            list[1] = tArr[1].Trim();
                        }
                        else
                        {
                            int len = 0;
                            for (int i = 0; i < tArr.Length; i++)
                            {
                                var temp = tArr[i].Trim();
                                len += (len == 0 ? 0 : 1) + temp.Length;

                                if (len < cutPoint)
                                    list[0] += " " + temp;
                                else
                                    list[1] += " " + temp;
                            }

                            list[0] = list[0].Trim();
                            list[1] = list[1].Trim();
                        }
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
