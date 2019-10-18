using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jeeprom
{
    class EraseProgressEventArgs : EventArgs
    {
        public EraseProgressEventArgs(int progress)
        {
            Progress = progress;
        }

        public int Progress { get; set; }
    }
}
