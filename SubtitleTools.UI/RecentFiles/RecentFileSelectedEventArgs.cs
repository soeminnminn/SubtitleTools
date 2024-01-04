using System;

namespace SubtitleTools.UI.RecentFiles
{
    public sealed class RecentFileSelectedEventArgs
    {
        internal RecentFileSelectedEventArgs(string fileName) => FileName = fileName;

        public string FileName { get; }
    }
}
