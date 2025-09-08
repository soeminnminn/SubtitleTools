using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubtitleTools
{
    public interface IDialogueCommand
    {
        bool CanExecute(Dialogue dialogue);

        void Execute(ref Dialogue dialogue);
    }
}
