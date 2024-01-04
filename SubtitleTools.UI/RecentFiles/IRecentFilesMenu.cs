using System;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace SubtitleTools.UI.RecentFiles
{
    public interface IRecentFilesMenu : IDisposable
    {
        event EventHandler<RecentFileSelectedEventArgs> RecentFileSelected;

        void Initialize(MenuItem miRecentFiles);

        void AddRecentFile(string fileName);

        void RemoveRecentFile(string fileName);
    }
}
