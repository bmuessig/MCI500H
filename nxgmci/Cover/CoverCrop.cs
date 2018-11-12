using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace nxgmci.Cover
{
    public static class CoverCrop
    {
        // Global constants
        private const uint COVER_WIDTH = 320;
        private const uint COVER_HEIGHT = 240;
        private const uint THUMB_WIDTH = 48;
        private const uint THUMB_HEIGHT = 48;

        // The global crop mode
        public static CoverCropMode CropMode
        {
            get;
            set;
        }
    }
}
