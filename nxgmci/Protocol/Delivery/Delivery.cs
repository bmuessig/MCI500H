using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;

namespace nxgmci.Protocol.Delivery
{
    public static class Delivery
    {
        // Internal timeout storage variables
        private static uint connectTimeoutMilliseconds = 5000,
            responseTimeoutMilliseconds = 1000,
            receiveTimeoutMilliseconds = 1500;

        // Internal timeout lock
        private volatile static object timeoutLock = new object();

        /// <summary>
        /// Returns or sets the maximum period in milliseconds that the connection process may take.
        /// This is thread-safe.
        /// </summary>
        public static uint ConnectTimeoutMilliseconds
        {
            // A lock is required to make this thread-safe
            get
            {
                lock (timeoutLock)
                    return connectTimeoutMilliseconds;
            }

            set
            {
                lock (timeoutLock)
                    connectTimeoutMilliseconds = value;
            }
        }

        /// <summary>
        /// Returns or sets the maximum period in milliseconds that the connection response may take.
        /// This is thread-safe.
        /// </summary>
        public static uint ResponseTimeoutMilliseconds
        {
            // A lock is required to make this thread-safe
            get
            {
                lock (timeoutLock)
                    return responseTimeoutMilliseconds;
            }

            set
            {
                lock (timeoutLock)
                    responseTimeoutMilliseconds = value;
            }
        }

        /// <summary>
        /// Returns or sets the maximum period in milliseconds that the connection response may take after init.
        /// This is thread-safe.
        /// </summary>
        public static uint ReceiveTimeoutMilliseconds
        {
            // A lock is required to make this thread-safe
            get
            {
                lock (timeoutLock)
                    return receiveTimeoutMilliseconds;
            }

            set
            {
                lock (timeoutLock)
                    receiveTimeoutMilliseconds = value;
            }
        }

        /// <summary>
        /// Uploads a piece of media that has no associated (album) art.
        /// </summary>
        /// <param name="EndPoint">The IP endpoint to post to.</param>
        /// <param name="RemotePath">Remote path to upload to.</param>
        /// <param name="MediaStream">Stream to read the media file to be uploaded from.</param>
        /// <returns></returns>
        public static Result PutMediaWithoutArt(IPEndPoint EndPoint, string RemotePath, Stream MediaStream)
        {
            return PutMediaWithArt(EndPoint, RemotePath, MediaStream, null, null);
        }

        /// <summary>
        /// Uploads a piece of media that has associated (album) art.
        /// </summary>
        /// <param name="EndPoint">The IP endpoint to post to.</param>
        /// <param name="RemotePath">Remote path to upload to.</param>
        /// <param name="MediaStream">Stream to read the media file to be uploaded from.</param>
        /// <param name="AlbumArt">Encrypted buffer of the cropped, primary, full size album art in JPEG format.</param>
        /// <param name="AlbumArtThumbnail">Encrypted buffer of the cropped, thumbnail album art in JPEG format.</param>
        /// <returns></returns>
        public static Result PutMediaWithArt(IPEndPoint EndPoint, string RemotePath, Stream MediaStream, byte[] AlbumArt, byte[] AlbumArtThumbnail)
        {
            // Allocate the result
            Result result = new Result();

            // Check, if album art has to be uploaded
            bool uploadArt = (AlbumArt == null) && (AlbumArtThumbnail == null);

            // Sanity-check the parameters
            if (EndPoint.Address == null)
                return Result.FailError(result, new ArgumentNullException("EndPoint"));
            if (EndPoint.Port == 0 || EndPoint.Port >= ushort.MaxValue)
                return Result.FailMessage(result, "The endpoint contains an invalid port or address!");
            if (RemotePath == null)
                return Result.FailError(result, new ArgumentNullException("RemotePath"));
            if (string.IsNullOrWhiteSpace(RemotePath) || RemotePath.Length < 6 || RemotePath.Contains(' ') || RemotePath.Contains('\t') || RemotePath.Contains((char)0))
                return Result.FailMessage(result, "The remote path size is invalid or it contains forbidden characters!");
            if (RemotePath[RemotePath.Length - 4] != '.' || RemotePath[0] != '/')
                return Result.FailMessage(result, "The remote path format is invalid!");
            if (MediaStream == null)
                return Result.FailError(result, new ArgumentNullException("MediaStream"));
            if (!MediaStream.CanRead)
                return Result.FailMessage(result, "The media stream has to be readable!");
            if (AlbumArt == null && uploadArt)
                return Result.FailError(result, new ArgumentNullException("AlbumArt"));
            if (AlbumArtThumbnail == null && uploadArt)
                return Result.FailError(result, new ArgumentNullException("AlbumArtThumbnail"));

            throw new NotImplementedException();
        }
    }
}
