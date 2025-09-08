using System;
using System.Collections.Generic;
using System.IO;

namespace SubtitleTools
{
    public interface ISubtitleParser
    {
        string FileExtension { get; set; }

        bool IsSupported(string input);

        bool Parse(string text, ref ISubtitle result);
    }
}
