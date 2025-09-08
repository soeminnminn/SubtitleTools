using System;

namespace SubtitleTools
{
    public interface ISubtitleCommand
    {
        bool CanExecute(ISubtitle subtitle);

        void PreExecute(ref ISubtitle subtitle);

        void PostExecute(ref ISubtitle subtitle);

        void ExecuteDialouge(ref Dialogue dialogue);
    }
}
