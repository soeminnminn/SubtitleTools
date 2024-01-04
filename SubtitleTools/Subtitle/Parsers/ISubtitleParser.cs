using System;
using System.Collections.Generic;
using System.IO;

namespace SubtitleTools
{
    public interface ISubtitleParser
    {
        string FileExtension { get; set; }

        bool Parse(Stream stream, out Subtitle result);
    }
}
