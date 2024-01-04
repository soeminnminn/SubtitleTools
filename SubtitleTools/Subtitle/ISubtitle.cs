using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SubtitleTools
{
    public interface ISubtitle : IList<Dialogue>, IList, IReadOnlyList<Dialogue>
    {
        Encoding CurrentEncoding { get; set; }

        bool Parse(string input);
    }
}
