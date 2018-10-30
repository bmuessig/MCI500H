using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nxgmci
{
    public class CompletedEventArgs : EventArgs
    {
        public bool Success;
        public Exception Error;

        internal CompletedEventArgs()
            : base()
        {
            this.Success = true;
        }

        internal CompletedEventArgs(bool Success)
            : base()
        {
            this.Success = Success;
        }

        internal CompletedEventArgs(Exception Error)
            : base()
        {
            this.Success = false;
            this.Error = Error;
        }
    }
}
