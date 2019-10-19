using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jeeprom
{
    class WriteProgressEventArgs : EventArgs
    {
        public WriteProgressEventArgs(int progress)
        {
            Progress = progress;
        }

        public int Progress { get; set; }
    }
}
