using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace nxgmci.Protocol.WADM
{
    public class RequestObjectCreate
    {

        /*
        <requestobjectcreate>
            <requestparameters>
                <updateid>534</updateid>
                <artist>AAA</artist>
                <album>BBB</album>
                <genre>Rock</genre>
                <name>CCC</name>
                <tracknum></tracknum>
                <year></year>
                <mediatype>mp3</mediatype>
                <dmmcookie>1438240788</dmmcookie>
                <timeout>10</timeout>
                <sortdatabase>1</sortdatabase>
            </requestparameters>
        </requestobjectcreate>
         * ...
         * <timeout/>
         * <albumarthash>c8350b47212248bc3c6b1e559bd822d1</albumarthash>
        <albumartfilesize>11293</albumartfilesize>
        <albumarttnfilesize>1252</albumarttnfilesize>
         * <sortdatabase/>
         */

        /// <summary>
        /// Assembles a RequestObjectCreate request without album art to be passed to the stereo.
        /// </summary>
        /// <param name="UpdateID">The update ID.</param>
        /// <param name="Artist">The artist of the track.</param>
        /// <param name="Album">The album of the track.</param>
        /// <param name="Genre">The genre of the track according to the genre list.</param>
        /// <param name="Name">The title of the track.</param>
        /// <param name="TrackNum">The number of the track.</param>
        /// <param name="Year">The year that the track was from.</param>
        /// <param name="MediaType">The three letter file extension of the media file.</param>
        /// <param name="DMMCookie">The unknown DMMCookie (seems to be ignored).</param>
        /// <param name="Timeout">The upload timeout in seconds.</param>
        /// <param name="SortDatabase">Indicates whether to sort the database after uploading.</param>
        /// <returns>A request string that can be passed to the stereo.</returns>
        public static string BuildWithoutAlbumArt(uint UpdateID,
            string Artist, string Album, string Genre, string Name, uint TrackNum, uint Year,
            string MediaType, uint DMMCookie, uint Timeout, bool SortDatabase = true)
        {
            // Check for input errors
            if (string.IsNullOrWhiteSpace(Artist) || string.IsNullOrWhiteSpace(Album) || string.IsNullOrWhiteSpace(Genre) || string.IsNullOrWhiteSpace(Name)
                || string.IsNullOrWhiteSpace(MediaType) || Timeout == 0)
                return null;

            // And build the request
            return string.Format(
                "<requestobjectcreate><requestparameters>" +
                "<updateid>{0}</updateid>" +
                "<artist>{1}</artist>" +
                "<album>{2}</album>" +
                "<genre>{3}</genre>" +
                "<name>{4}</name>" +
                "<tracknum>{5}</tracknum>" +
                "<year>{6}</year> " +
                "<mediatype>{7}</mediatype>" +
                "<dmmcookie>{8}</dmmcookie>" +
                "<timeout>{9}</timeout>" +
                "<sortdatabase>{10}</sortdatabase>" +
                "</requestparameters></requestobjectcreate>",
                UpdateID,
                WADMParser.TrimValue(WADMParser.EncodeValue(Artist), true),
                WADMParser.TrimValue(WADMParser.EncodeValue(Album), true),
                WADMParser.TrimValue(WADMParser.EncodeValue(Genre), true),
                WADMParser.TrimValue(WADMParser.EncodeValue(Name), true),
                TrackNum,
                Year,
                WADMParser.TrimValue(WADMParser.EncodeValue(MediaType), true),
                DMMCookie,
                Timeout,
                SortDatabase ? 1 : 0);
        }

        /// <summary>
        /// Assembles a RequestObjectCreate request with album art to be passed to the stereo.
        /// </summary>
        /// <param name="UpdateID">The update ID.</param>
        /// <param name="Artist">The artist of the track.</param>
        /// <param name="Album">The album of the track.</param>
        /// <param name="Genre">The genre of the track according to the genre list.</param>
        /// <param name="Name">The title of the track.</param>
        /// <param name="TrackNum">The number of the track.</param>
        /// <param name="Year">The year that the track was from.</param>
        /// <param name="MediaType">The three letter file extension of the media file.</param>
        /// <param name="DMMCookie">The unknown DMMCookie (seems to be ignored).</param>
        /// <param name="Timeout">The upload timeout in seconds.</param>
        /// <param name="AlbumArtHash">The MD5 hash of the primariy album art.</param>
        /// <param name="AlbumArtFileSize">The file size in bytes of the primary album art.</param>
        /// <param name="AlbumArtTnFileSize">The file size in bytes of the thumbnail of the album art.</param>
        /// <param name="SortDatabase">Indicates whether to sort the database after uploading.</param>
        /// <returns>A request string that can be passed to the stereo.</returns>
        public static string BuildWithAlbumArt(uint UpdateID,
            string Artist, string Album, string Genre, string Name, uint TrackNum, uint Year,
            string MediaType, uint DMMCookie, uint Timeout,
            string AlbumArtHash, uint AlbumArtFileSize, uint AlbumArtTnFileSize,
            bool SortDatabase = true)
        {
            // Check for input errors
            if (string.IsNullOrWhiteSpace(Artist) || string.IsNullOrWhiteSpace(Album) || string.IsNullOrWhiteSpace(Genre) || string.IsNullOrWhiteSpace(Name)
                || string.IsNullOrWhiteSpace(MediaType) || string.IsNullOrWhiteSpace(AlbumArtHash) || AlbumArtFileSize == 0 || AlbumArtTnFileSize == 0 || Timeout == 0)
                return null;

            // Check, if the hash length is invalid
            if (AlbumArtHash.Length != 32)
                return null;

            // And build the request
            return string.Format(
                "<requestobjectcreate><requestparameters>" +
                "<updateid>{0}</updateid>" +
                "<artist>{1}</artist>" +
                "<album>{2}</album>" +
                "<genre>{3}</genre>" +
                "<name>{4}</name>" +
                "<tracknum>{5}</tracknum>" +
                "<year>{6}</year> " +
                "<mediatype>{7}</mediatype>" +
                "<dmmcookie>{8}</dmmcookie>" +
                "<timeout>{9}</timeout>" +
                "<albumarthash>{10}</albumarthash>" +
                "<albumartfilesize>{11}</albumartfilesize>" +
                "<albumarttnfilesize>{12}</albumarttnfilesize>" +
                "<sortdatabase>{13}</sortdatabase>" +
                "</requestparameters></requestobjectcreate>",
                UpdateID,
                WADMParser.TrimValue(WADMParser.EncodeValue(Artist), true),
                WADMParser.TrimValue(WADMParser.EncodeValue(Album), true),
                WADMParser.TrimValue(WADMParser.EncodeValue(Genre), true),
                WADMParser.TrimValue(WADMParser.EncodeValue(Name), true),
                TrackNum,
                Year,
                WADMParser.TrimValue(WADMParser.EncodeValue(MediaType), true),
                DMMCookie,
                Timeout,
                WADMParser.TrimValue(WADMParser.EncodeValue(AlbumArtHash), true),
                AlbumArtFileSize,
                AlbumArtTnFileSize,
                SortDatabase ? 1 : 0);
        }

        public class RemotePath
        {
            /// <summary>
            /// The "fake" remote URL to push the new media via DeliveryClient to.
            /// </summary>
            public readonly string URL;

            public readonly EndPoint ep;

            public readonly IPAddress IPAddress;

            public readonly ushort Port;

            // Regex for dissecting the URL
            // First group: IPv4 string - for verification with the existing info only
            // Second group: Optional port number - has precedence over the default one, if present and non-zero
            // Third group: Remote upload path with leading slash to be passed to the DeliveryClient
            private const string QUERY_REGEX = @"^\s*http:\/\/((?:\d+\.){3}\d+)(?::(\d+))?([\w-\/\.]+\.\w{3})\s*$";

            public RemotePath(string URL)
            {

            }
        }
    }
}
