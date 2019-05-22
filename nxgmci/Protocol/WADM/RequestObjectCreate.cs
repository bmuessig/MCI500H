using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Text.RegularExpressions;

namespace nxgmci.Protocol.WADM
{
    public class RequestObjectCreate
    {
        // RequestObjectCreate Parser
        private readonly static WADMParser parser = new WADMParser("requestobjectcreate", "responseparameters", false);

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

        /// <summary>
        /// Parses RequestObjectCreate's ResponseParameters and returns the result.
        /// </summary>
        /// <param name="Response">The response received from the stereo.</param>
        /// <param name="ValidateInput">Indicates whether to validate the data values received.</param>
        /// <param name="LazySyntax">Indicates whether to ignore minor syntax errors.</param>
        /// <returns>A result object that contains a serialized version of the response data.</returns>
        public static Result<ResponseParameters> Parse(string Response, bool ValidateInput = true, bool LazySyntax = false)
        {
            // Allocate the result object
            Result<ResponseParameters> result = new Result<ResponseParameters>();

            // Make sure the response is not null
            if (string.IsNullOrWhiteSpace(Response))
                return Result<ResponseParameters>.FailMessage(result, "The response may not be null!");

            // Then, parse the response
            Result<WADMProduct> parserResult = parser.Parse(Response, LazySyntax);

            // Check if it failed
            if (!parserResult.Success)
                if (parserResult.Error != null)
                    return Result<ResponseParameters>.FailErrorMessage(result, parserResult.Error, "The parsing failed!");
                else
                    return Result<ResponseParameters>.FailMessage(result, "The parsing failed for unknown reasons!");

            // Make sure the product is there
            if (parserResult.Product == null)
                return Result<ResponseParameters>.FailMessage(result, "The parsing product was null!");

            // And also make sure that the state is correct
            if (parserResult.Product.Elements == null)
                return Result<ResponseParameters>.FailMessage(result, "The list of parsed elements is null!");

            // Try to parse the status
            Result<WADMStatus> statusResult = WADMStatus.Parse(parserResult.Product.Elements, ValidateInput);

            // Check if it failed
            if (!statusResult.Success)
                if (statusResult.Error != null)
                    return Result<ResponseParameters>.FailErrorMessage(result, statusResult.Error, "The status code parsing failed!");
                else
                    return Result<ResponseParameters>.FailMessage(result, "The status code parsing failed for unknown reasons!");

            // Make sure the product is there
            if (statusResult.Product == null)
                return Result<ResponseParameters>.FailMessage(result, "The status code parsing product was null!");

            // Now, make sure our mandatory arguments exist
            if (!parserResult.Product.Elements.ContainsKey("updateid"))
                return Result<ResponseParameters>.FailMessage(result, "Could not locate parameter '{0}'!", "updateid");
            if (!parserResult.Product.Elements.ContainsKey("index"))
                return Result<ResponseParameters>.FailMessage(result, "Could not locate parameter '{0}'!", "index");
            if (!parserResult.Product.Elements.ContainsKey("importresource"))
                return Result<ResponseParameters>.FailMessage(result, "Could not locate parameter '{0}'!", "importresource");

            // Then, try to parse the parameters
            uint updateID, index;
            string importResource;

            if (!uint.TryParse(parserResult.Product.Elements["updateid"], out updateID))
                return Result<ResponseParameters>.FailMessage(result, "Could not parse parameter '{0}' as uint!", "updateid");
            if (!uint.TryParse(parserResult.Product.Elements["index"], out index))
                return Result<ResponseParameters>.FailMessage(result, "Could not parse parameter '{0}' as uint!", "index");
            
            // And parse the virtual import resource path
            Result<RemotePath> remotePathResult = RemotePath.Parse(parserResult.Product.Elements["importresource"], ValidateInput);

            // Finally, return the response
            return null; // Result<ResponseParameters>.SucceedProduct(result, new ResponseParameters(statusResult.Product));
        }

        /// <summary>
        /// RequestObjectCreate's ResponseParameters reply.
        /// </summary>
        public class ResponseParameters
        {
            /// <summary>
            /// The modification update ID passed as a token. Equal to the originally supplied update ID + 1.
            /// </summary>
            public readonly uint UpdateID;

            /// <summary>
            /// The universal database index of the new object to be created.
            /// </summary>
            public readonly uint Index;

            /// <summary>
            /// The virtual path that the media needs to be uploaded to.
            /// </summary>
            public readonly RemotePath ImportResource;

            /// <summary>
            /// Stores the status code returned for the operation.
            /// </summary>
            public readonly WADMStatus Status;

            /// <summary>
            /// Default internal constructor.
            /// </summary>
            /// <param name="UpdateID">The modification update ID passed as a token. Equal to the originally supplied update ID + 1.</param>
            /// <param name="Index">The universal database index of the new object to be created.</param>
            /// <param name="ImportResource">The virtual path that the media needs to be uploaded to.</param>
            /// <param name="Status">Stores the status code returned for the operation.</param>
            internal ResponseParameters(uint UpdateID, uint Index, RemotePath ImportResource, WADMStatus Status)
            {
                // Sanity check the input
                if (Status == null)
                    throw new ArgumentNullException("Status");
                if (ImportResource == null)
                    throw new ArgumentNullException("ImportResource");

                this.UpdateID = UpdateID;
                this.Index = Index;
                this.ImportResource = ImportResource;
                this.Status = Status;
            }
        }

        public class RemotePath
        {
            /// <summary>
            /// The "fake" remote URL to push the new media via DeliveryClient to.
            /// </summary>
            public readonly string URL;

            // TODO: Decide whether to use endpoint or IP address

            public readonly EndPoint ep;

            public readonly IPAddress IPAddress;

            public readonly ushort Port;

            // Regex for dissecting the URL
            // First group: IPv4 string - for verification with the existing info only
            // Second group: Optional port number - has precedence over the default one, if present and non-zero
            // Third group: Remote upload path with leading slash to be passed to the DeliveryClient
            private const string QUERY_REGEX = @"^\s*http:\/\/((?:\d+\.){3}\d+)(?::(\d+))?([\w-\/\.]+\.\w{3})\s*$";

            private static Regex queryRegex = new Regex(QUERY_REGEX, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

            /// <summary>
            /// Private constructor. Parse is used to create this object.
            /// </summary>
            /// <param name="URL"></param>
            private RemotePath(string URL)
            {

            }

            /// <summary>
            /// This will attempt to parse and partially validate the virtual remote URL used for uploading media.
            /// </summary>
            /// <param name="ImportResourceURL">The raw ImportResource URL from the request.</param>
            /// <param name="ValidateInput">Indicates whether to formally check the individual fields.</param>
            /// <returns>A result object that contains a parsed version of the response data.</returns>
            public static Result<RemotePath> Parse(string ImportResourceURL, bool ValidateInput)
            {
                throw new NotImplementedException();
            }
        }
    }
}
