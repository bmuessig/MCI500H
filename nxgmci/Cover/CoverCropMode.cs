using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nxgmci.Cover
{
    public enum CoverCropMode : byte
    {
        // Just center the image and don't scale it
        None,

        // Crops width or height based on the least amount of wasted area
        MaximizeArea,

        // Crops the image's height to fit
        CropHeight,

        // Crops the image's width to fit
        CropWidth,

        // Zooms the image to cover the most area on a black background
        ZoomBlack,

        // Zooms the image to cover the most area on a white background
        ZoomWhite,

        // Zooms the image to cover the most area on a gray background
        ZoomGray,

        // Stretches the image without keeping the original aspect ratio
        Stretch
    }
}
