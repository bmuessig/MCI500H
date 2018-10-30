using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nxgmci
{
    public class MediaLibrary
    {
        public MediaElement[] Tracks { get; private set; }

        public MCI500H Device { get; private set; }

        
    }
}
