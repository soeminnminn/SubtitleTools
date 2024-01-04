using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibVLCSharp.Shared;

namespace SubtitleTools.UI.Models
{
    internal class PlayerModel : IDisposable
    {
        private LibVLC vlcLib = null;

        public PlayerModel(ConfigModel config)
        {
            try
            {
                vlcLib = new LibVLC();
            }
            catch(Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
        }

        public void Dispose()
        {
            if (vlcLib != null)
            {
                vlcLib.Dispose();
            }
        }
    }
}
