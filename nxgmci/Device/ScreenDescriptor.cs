namespace nxgmci.Device
{
    /// <summary>
    /// Provides information on screen, cover and thumbnail sizes, as well as the format of the cover art and encoding.
    /// </summary>
    public class ScreenDescriptor
    {
        /// <summary>
        /// The default width of the screen in pixels.
        /// </summary>
        public const ushort DEFAULT_SCREEN_WIDTH = 0;

        /// <summary>
        /// The default height of the screen in pixels.
        /// </summary>
        public const ushort DEFAULT_SCREEN_HEIGHT = 0;

        /// <summary>
        /// The default width of the cover in pixels.
        /// </summary>
        public const ushort DEFAULT_COVER_WIDTH = 0;

        /// <summary>
        /// The default height of the cover in pixels.
        /// </summary>
        public const ushort DEFAULT_COVER_HEIGHT = 0;

        /// <summary>
        /// The default width of the thumbnails in pixels.
        /// </summary>
        public const ushort DEFAULT_THUMB_WIDTH = 0;

        /// <summary>
        /// The default height of the thumbnails in pixels.
        /// </summary>
        public const ushort DEFAULT_THUMB_HEIGHT = 0;

        /// <summary>
        /// The default image format and encoding of the assets.
        /// </summary>
        public const AssetFormat DEFAULT_ASSET_FORMAT = AssetFormat.JPEG_500H;

        /// <summary>
        /// The width of the screen in pixels.
        /// </summary>
        public readonly ushort ScreenWidth;

        /// <summary>
        /// The height of the screen in pixels.
        /// </summary>
        public readonly ushort ScreenHeight;

        /// <summary>
        /// The width of album covers in pixels.
        /// </summary>
        public readonly ushort CoverWidth;

        /// <summary>
        /// The height of album covers in pixels.
        /// </summary>
        public readonly ushort CoverHeight;

        /// <summary>
        /// The width of thumbnail previews in pixels.
        /// </summary>
        public readonly ushort ThumbWidth;

        /// <summary>
        /// The height of thumbnail previews in pixels.
        /// </summary>
        public readonly ushort ThumbHeight;

        /// <summary>
        /// The image format and encoding used for cover art and thumbnails.
        /// </summary>
        public readonly AssetFormat Format;

        /// <summary>
        /// The default public constructor.
        /// </summary>
        /// <param name="ScreenWidth">The width of the screen in pixels.</param>
        /// <param name="ScreenHeight">The height of the screen in pixels.</param>
        /// <param name="CoverWidth">The width of album covers in pixels.</param>
        /// <param name="CoverHeight">The height of album covers in pixels.</param>
        /// <param name="ThumbWidth">The width of thumbnail previews in pixels.</param>
        /// <param name="ThumbHeight">The height of thumbnail previews in pixels.</param>
        /// <param name="Format">The image format and encoding used for cover art and thumbnails.</param>
        public ScreenDescriptor(ushort ScreenWidth = DEFAULT_SCREEN_WIDTH, ushort ScreenHeight = DEFAULT_SCREEN_HEIGHT,
            ushort CoverWidth = DEFAULT_COVER_WIDTH, ushort CoverHeight = DEFAULT_COVER_HEIGHT,
            ushort ThumbWidth = DEFAULT_THUMB_WIDTH, ushort ThumbHeight = DEFAULT_THUMB_HEIGHT,
            AssetFormat Format = DEFAULT_ASSET_FORMAT)
        {
            // TODO: Sanity checks!
            this.ScreenWidth = ScreenWidth;
            this.ScreenHeight = ScreenHeight;
            this.CoverWidth = CoverWidth;
            this.CoverHeight = CoverHeight;
            this.ThumbWidth = ThumbWidth;
            this.ThumbHeight = ThumbHeight;
            this.Format = Format;
        }
        
        /// <summary>
        /// Contains all supported image formats and encodings used for the cover art and thumbnails.
        /// </summary>
        public enum AssetFormat
        {
            /// <summary>
            /// Unknown format and encoding.
            /// </summary>
            Unknown,

            /// <summary>
            /// JPEG, not encoded.
            /// </summary>
            JPEG_Raw,

            /// <summary>
            /// JPEG; XOR encoded for the MCI500H
            /// </summary>
            JPEG_500H
        }
    }
}
