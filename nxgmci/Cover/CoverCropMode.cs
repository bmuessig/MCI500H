using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nxgmci.Cover
{
    public enum CoverCropMode : byte
    {
        /// <summary>
        /// Just plot the image at the upper left origin and don't scale it.
        /// </summary>
        None,

        /// <summary>
        /// Just center the image and don't scale it.
        /// </summary>
        Center,

        /// <summary>
        /// Stretches the image without keeping the original aspect ratio.
        /// </summary>
        Stretch,

        /// <summary>
        /// Crops width or height based on the least amount of wasted area.
        /// </summary>
        MaximizeArea,

        /// <summary>
        /// Crops the image's height to fit.
        /// </summary>
        CropHeight,

        /// <summary>
        /// Crops the image's width to fit.
        /// </summary>
        CropWidth,

        /// <summary>
        /// Zooms the image to cover the most area on a black background.
        /// </summary>
        ZoomBlack,

        /// <summary>
        /// Zooms the image to cover the most area on a white background.
        /// </summary>
        ZoomWhite,

        /// <summary>
        /// Zooms the image to cover the most area on a gray background.
        /// </summary>
        ZoomGray,

        /// <summary>
        /// Zooms the image to cover the most area on a blurred and streched version of the image as a background.
        /// </summary>
        ZoomModern
    }
}
