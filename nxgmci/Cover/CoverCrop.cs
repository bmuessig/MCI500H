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

        public static bool MakeThumbnail(Bitmap OriginalImage, out Bitmap ResultImage)
        {
            ResultImage = null;

            // Make sure out input it not null
            if (OriginalImage == null)
                return false;

            // It's just not worth scaling anything below 16x16
            if (OriginalImage.Width < 16 || OriginalImage.Height < 16)
                return false;

            int newWidth = 0, newHeight = 0;
            Color backColor = Color.Transparent;

            return false;
        }
        /*
        private static bool Calculate(Size OriginalSize, Size ThumbnailSize, CoverCropMode CropMode,
            out Rectangle NewSource, out Rectangle NewTarget, out Color BackgroundColor)
        {
            NewTarget = Rectangle.Empty;
            NewSource = Rectangle.Empty;
            BackgroundColor = Color.Black;

            if (OriginalSize.Width < 1 || OriginalSize.Height < 1)
                return false;
            if(ThumbnailSize.Width < 1 || ThumbnailSize.Height < 1)
                return false;

            switch (CropMode)
            {
                case CoverCropMode.None:
                    NewSource = new Rectangle(0, 0, ThumbnailSize.Width, ThumbnailSize.Height);
                    NewTarget = new Rectangle(0, 0, ThumbnailSize.Width, ThumbnailSize.Height);
                    return true;

                case CoverCropMode.Center:
                    //NewSource = new Rectangle((OriginalSize.Width <= ThumbnailSize.Width) ? OriginalSize.Width : (OriginalSize.Width - ThumbnailSize.Width)
                    //NewTarget = new Rectangle(
                case CoverCropMode.Stretch:
                    NewSource = new Rectangle(0, 0, ThumbnailSize.Width, ThumbnailSize.Height);
                    NewTarget = new Rectangle(0, 0, ThumbnailSize.Width, ThumbnailSize.Height);
                    return true;
                case CoverCropMode.MaximizeArea:
                    if (OriginalSize.Width > OriginalSize.Height)
                        return Calculate(OriginalSize, ThumbnailSize, CoverCropMode.CropWidth, out NewSource, out NewTarget, out BackgroundColor);
                    else if(OriginalSize.Height > OriginalSize.Width)
                        return Calculate(OriginalSize, ThumbnailSize, CoverCropMode.CropHeight, out NewSource, out NewTarget, out BackgroundColor);
                    else
                        return Calculate(OriginalSize, ThumbnailSize, CoverCropMode.Stretch, out NewSource, out NewTarget, out BackgroundColor);
                case CoverCropMode.CropHeight:

                case CoverCropMode.CropWidth:

                case CoverCropMode.ZoomBlack:
                    BackgroundColor = Color.Black;
                case CoverCropMode.ZoomGray:
                    BackgroundColor = Color.DarkGray;
                case CoverCropMode.ZoomWhite:
                    BackgroundColor = Color.White;
                case CoverCropMode.ZoomModern:

                default:
                    return false;
            }
        }*/
    }
}
