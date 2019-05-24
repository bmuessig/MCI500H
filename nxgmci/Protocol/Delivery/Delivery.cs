using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace nxgmci.Protocol.Delivery
{
    public static class Delivery
    {
        // TODO: Turn this into Deliveryclient with upload status events

        // Internal timeout storage variables
        private static uint connectTimeoutMilliseconds = 5000;

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
        /// Uploads a piece of media that has no associated (album) art.
        /// </summary>
        /// <param name="EndPoint">The IP endpoint to post to.</param>
        /// <param name="RemotePath">Remote path to upload to.</param>
        /// <param name="MediaBytes">Buffer of the media file to be uploaded.</param>
        /// <returns>A result object that indicates success or failure.</returns>
        public static Result PutMediaWithoutArt(IPEndPoint EndPoint, string RemotePath, byte[] MediaBytes)
        {
            return PutMediaWithArt(EndPoint, RemotePath, MediaBytes, null, null);
        }

        /// <summary>
        /// Uploads a piece of media that has associated (album) art.
        /// </summary>
        /// <param name="EndPoint">The IP endpoint to post to.</param>
        /// <param name="RemotePath">Remote path to upload to.</param>
        /// <param name="MediaBytes">Buffer of the media file to be uploaded.</param>
        /// <param name="AlbumArt">Encrypted buffer of the cropped, primary, full size album art in JPEG format.</param>
        /// <param name="AlbumArtThumbnail">Encrypted buffer of the cropped, thumbnail album art in JPEG format.</param>
        /// <returns>A result object that indicates success or failure.</returns>
        public static Result PutMediaWithArt(IPEndPoint EndPoint, string RemotePath, byte[] MediaBytes, byte[] AlbumArt, byte[] AlbumArtThumbnail)
        {
            // Allocate the result
            Result result = new Result();

            // Check, if album art has to be uploaded
            bool uploadArt = (AlbumArt != null) && (AlbumArtThumbnail != null);

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
            if (MediaBytes == null)
                return Result.FailError(result, new ArgumentNullException("MediaStream"));
            if (AlbumArt == null && uploadArt)
                return Result.FailError(result, new ArgumentNullException("AlbumArt"));
            if (AlbumArtThumbnail == null && uploadArt)
                return Result.FailError(result, new ArgumentNullException("AlbumArtThumbnail"));
            if (MediaBytes.Length == 0)
                return Result.FailMessage(result, "The media buffer is empty!");
            if (uploadArt)
            {
                if (AlbumArt.Length == 0)
                    return Result.FailMessage(result, "The album art buffer is empty!");
                if (AlbumArtThumbnail.Length == 0)
                    return Result.FailMessage(result, "The album art thumbnail buffer is empty!");
            }

            try
            {
                // Try setting up a new TcpClient to connect to the delivery server
                TcpClient client = new TcpClient()
                {
                    SendBufferSize = 25 * 1024 * 1024,
                };

                // Connect "async" to give us control over the connection process and also support a timeout
                IAsyncResult asyncResult = client.BeginConnect(EndPoint.Address, EndPoint.Port, null, null);
                if (!asyncResult.AsyncWaitHandle.WaitOne((int)ConnectTimeoutMilliseconds, true))
                {
                    // Timeout error
                    client.Close();
                    return Result.FailMessage(result, "Timeout connecting to server!");
                }
                // Finish connecting
                client.EndConnect(asyncResult);

                // Make sure we are now connected
                if (!client.Connected)
                    return Result.FailMessage(result, "Error connecting to server!");

                // Get us the underlying stream
                NetworkStream stream = client.GetStream();
                // And create a textwriter object to simplify working with the partially ASCII based protocol
                TextWriter writer = new StreamWriter(stream, Encoding.ASCII);

                // Depending on if art should be uploaded, one of two headers is sent
                if (uploadArt)
                    writer.Write("put {0} {1} {2} {3}", RemotePath, MediaBytes.Length, AlbumArt.Length, AlbumArtThumbnail.Length);
                else
                    writer.Write("put {0} {1}", RemotePath, MediaBytes.Length);

                // Make sure the writer has transmitted everything
                writer.Flush();

                // Indicate, that the header is done
                stream.WriteByte(0);

                // Now, send the media file
                stream.Write(MediaBytes, 0, MediaBytes.Length);

                // And make sure again it's all transmitted
                stream.Flush();

                // Now, if desired, send the album art and thumbnail
                if (uploadArt)
                {
                    // First, send the full sized album art
                    stream.Write(AlbumArt, 0, AlbumArt.Length);

                    // Finally, send the thumbnail album art
                    stream.Write(AlbumArtThumbnail, 0, AlbumArtThumbnail.Length);

                    // And make sure again it's all transmitted
                    stream.Flush();
                }
                
                // Finally, close the streams
                stream.Close();
                client.Close();
            }
            catch (Exception ex)
            {
                // Return failure
                return Result.FailError(result, ex);
            }

            // And return success
            return Result.Succeed(result);
        }
    }
}
