using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SubtitleTools.UI.Helpers
{
    internal static class VideoFiles
    {
        #region Variables
        private static Regex videoFilesRegex = new Regex(@"\.(?:(?:3g2)|(?:3gp)|(?:asf)|(?:avi)|(?:bdm)|(?:cpi)|(?:divx)|(?:flv)|(?:m4v)|(?:mkv)|(?:mod)|(?:mov)|(?:mp3)|(?:mp4)|(?:mpeg)|(?:mpg)|(?:mts)|(?:ogg)|(?:ogm)|(?:ogv)|(?:rm)|(?:rmvb)|(?:spx)|(?:vob)|(?:wav)|(?:webm)|(?:wma)|(?:wmv)|(?:xvid))$", RegexOptions.IgnoreCase);
        #endregion

        #region Methods
        public static List<string> GetVideoFilesAtPath(string path)
        {
            List<string> videoFiles = new List<string>();

            if ((path == null) || (path == string.Empty))
                return videoFiles;

            string[] allFiles = Directory.GetFiles(path, "*.*");
            foreach (string file in allFiles)
            {
                if (videoFilesRegex.IsMatch(file))
                    videoFiles.Add(file);
            }
            return videoFiles;
        }

        public static string FindMatchingVideo(string file)
        {
            string fileDir = Path.GetDirectoryName(file);
            List<string> videoFiles = GetVideoFilesAtPath(fileDir);
            string filename = Path.GetFileNameWithoutExtension(file);

            foreach (string videoFile in videoFiles)
            {
                string video = Path.GetFileNameWithoutExtension(videoFile);
                if (video == filename)
                {
                    string videoFilename = Path.GetFileName(videoFile);
                    return Path.Combine(fileDir, videoFilename);
                }
            }

            return null;
        }
        #endregion
    }
}
