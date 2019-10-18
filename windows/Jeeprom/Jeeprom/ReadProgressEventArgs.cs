using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jeeprom
{
    class ReadProgressEventArgs : EventArgs
    {
        public ReadProgressEventArgs(int progress, string data)
        {
            Progress = progress;
            Data = data;
        }

        public int Progress { get; set; }

        public string Data { get; set; }
    }
}
