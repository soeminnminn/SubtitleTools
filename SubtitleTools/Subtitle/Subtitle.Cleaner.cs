using System;
using System.Collections;
using System.Collections.Generic;

namespace SubtitleTools
{
    public class Cleaner
    {
        #region Variables
        private readonly List<ISubtitleCommand> commands = new List<ISubtitleCommand>();
        #endregion

        #region Properties
        public List<ISubtitleCommand> Commands
        {
            get => commands;
        }
        #endregion

        #region Constructor
        public Cleaner()
        {
            commands.Add(new Commands.SimplyStyles());
            commands.Add(new Commands.RemoveHearingText());
            commands.Add(new Commands.RemoveEmptyLine());
            commands.Add(new Commands.TypoFix());
            commands.Add(new Commands.MergeLines());
            commands.Add(new Commands.SplitLine());
            commands.Add(new Commands.RemoveEmptyDialogues());
        }
        #endregion

        #region Methods
        public void Clean(ref ISubtitle subtitle)
        {
            foreach (var cmd in commands)
            {
                if (cmd.CanExecute(subtitle))
                {
                    cmd.PreExecute(ref subtitle);

                    int length = ((ICollection<Dialogue>)subtitle).Count;
                    for (int i = 0; i < length; i++)
                    {
                        var dlg = ((IList<Dialogue>)subtitle)[i];
                        cmd.ExecuteDialouge(ref dlg);
                    }

                    cmd.PostExecute(ref subtitle);
                }
            }
        }
        #endregion
    }
}
