using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using SubtitleTools.UI.Helpers;

namespace SubtitleTools.UI.Models
{
    internal class PlayerModel : IDisposable
    {
        #region Variables
        private readonly ConfigModel config;
        private LibVLC vlcLib = null;
        private MediaPlayer player = null;
        #endregion

        #region Constructors
        public PlayerModel(ConfigModel config)
        {
            this.config = config;
        }
        #endregion

        #region Properties
        public MediaPlayer Player
        {
            get => player;
        }
        #endregion

        #region Methods
        public void Initialize()
        {
            if (vlcLib != null) return;

            var task = Task.Run(() => 
            {
                List<string> options = new List<string>() 
                {
#if DEBUG
                    "--verbose=3",
#else
                    "--verbose=0",
#endif
                    "--no-osd"
                };

#if DEBUG
                LibVLC libVlc = new(true, options.ToArray());
#else
                LibVLC libVlc = new(false, options.ToArray());
#endif
                return libVlc;
            });

            task.GetAwaiter().OnCompleted(() => 
            {
                vlcLib = task.Result;
                if (vlcLib != null)
                {
                    player = new MediaPlayer(vlcLib);
                }                
            });
        }

        public bool Open(string filePath)
        {
            if (player == null) return false;
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return false;

            try
            {
                if (player.IsPlaying)
                {
                    player.Stop();
                }

                var media = new Media(vlcLib, filePath, FromType.FromPath);
                player.Media = media;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }

            return true;
        }

        public bool OpenFromSubtitle(string filePath)
        {
            if (player == null) return false;
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return false;

            var videoFile = VideoFiles.FindMatchingVideo(filePath);
            return Open(videoFile);
        }

        public bool Play()
        {
            if (player == null) return false;

            if (player.Media != null && player.Play())
            {
                return true;
            }

            return false;
        }

        public void Pause()
        {
            if (player == null) return;

            if (player.Media != null && player.IsPlaying)
            {
                player.Pause();
            }
        }

        public void Restart(bool seekToPos = true)
        {
            if (player == null) return;

            
        }

        public void Stop()
        {
            if (player == null) return;

            if (player.Media != null)
            {
                player.Stop();
                player.Media = null;
            }
        }

        public void Dispose()
        {
            if (player != null)
            {
                player.Dispose();
            }

            if (vlcLib != null)
            {
                vlcLib.Dispose();
            }
        }
        #endregion
    }
}
