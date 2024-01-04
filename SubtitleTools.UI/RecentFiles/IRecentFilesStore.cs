using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubtitleTools.UI.RecentFiles
{
    public interface IRecentFilesStore : IDisposable
    {
        Task<List<string>> GetRecentFiles();

        void AddRecentFile(string fileName, int maxFiles);

        void RemoveRecentFile(string fileName);

        void ClearRecentFiles();
    }
}
