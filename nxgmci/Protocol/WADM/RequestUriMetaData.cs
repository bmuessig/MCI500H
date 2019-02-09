using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace nxgmci.Protocol.WADM
{
    /// <summary>
    /// This request is used to retrieve lots of important flags from the stereo.
    /// For instance, this will provide the public directory of the media files.
    /// It will also provide the bitmask mask that needs to be applied to work with the node IDs.
    /// Apart from that it presents a list of supported file formats along with their type-ID mapping.
    /// </summary>
    public static class RequestUriMetaData
    {
        // RequestUriMetaData Parser
        private readonly static WADMParser parser = new WADMParser("requesturimetadata", "responseparameters", false);

        // MediaTypeKey Parser Regex
        private static readonly Regex mediaTypeKeyRegex = new Regex(@"(\d+)\s*=+\s*(\w+)(?:\s*,|$)",
            RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// Assembles a RequestUriMetaData request to be passed to the stereo.
        /// </summary>
        /// <returns>A request string that can be passed to the stereo.</returns>
        public static string Build()
        {
            return "<requesturimetadata></requesturimetadata>";
        }

        /// <summary>
        /// Parses RequestUriMetaData's ResponseParameters and returns the result.
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
                return result.FailMessage("The response may not be null!");

            // Then, parse the response
            Result<WADMProduct> parserResult = parser.Parse(Response, LazySyntax);

            // Check if it failed
            if (!parserResult.Success)
                if (!string.IsNullOrWhiteSpace(parserResult.Message))
                    return result.Fail("The parsing failed!", parserResult.Error);
                else
                    return result.FailMessage("The parsing failed for unknown reasons!");

            // Make sure the product is there
            if (parserResult.Product == null)
                return result.FailMessage("The parsing product was null!");

            // And also make sure our state is correct
            if (parserResult.Product.Elements == null)
                return result.FailMessage("The list of parsed elements is null!");

            // Now, make sure our mandatory arguments exist
            if (!parserResult.Product.Elements.ContainsKey("uripath"))
                return result.FailMessage("Could not locate parameter '{0}'!", "uripath");
            if (!parserResult.Product.Elements.ContainsKey("idmask"))
                return result.FailMessage("Could not locate parameter '{0}'!", "idmask");
            if (!parserResult.Product.Elements.ContainsKey("containersize"))
                return result.FailMessage("Could not locate parameter '{0}'!", "containersize");
            if (!parserResult.Product.Elements.ContainsKey("mediatypekey"))
                return result.FailMessage("Could not locate parameter '{0}'!", "mediatypekey");
            if (!parserResult.Product.Elements.ContainsKey("updateid"))
                return result.FailMessage("Could not locate parameter '{0}'!", "updateid");

            // Then, try to parse the parameters
            string uriPath, mediaTypeKey;
            uint idMask, containerSize, updateID;
            if (string.IsNullOrWhiteSpace((uriPath = parserResult.Product.Elements["uripath"])))
                return result.FailMessage("Could not parse parameter '{0}' as string!", "uripath");
            if (!uint.TryParse(parserResult.Product.Elements["idmask"], out idMask))
                return result.FailMessage("Could not parse parameter '{0}' as uint!", "idmask");
            if (!uint.TryParse(parserResult.Product.Elements["containersize"], out containerSize))
                return result.FailMessage("Could not parse parameter '{0}' as uint!", "containersize");
            if (string.IsNullOrWhiteSpace((mediaTypeKey = parserResult.Product.Elements["mediatypekey"])))
                return result.FailMessage("Could not parse parameter '{0}' as string!", "mediatypekey");
            if (!uint.TryParse(parserResult.Product.Elements["updateid"], out updateID))
                return result.FailMessage("Could not parse parameter '{0}' as uint!", "updateid");

            // We may have to perform some sanity checks
            if (ValidateInput)
            {
                if (string.IsNullOrWhiteSpace(uriPath))
                    return result.FailMessage("uripath is null or white-space!");
                if (string.IsNullOrWhiteSpace(mediaTypeKey))
                    return result.FailMessage("mediatypekey is null or white-space!");
                if (idMask == 0)
                    return result.FailMessage("idmask == 0");
                if (containerSize == 0)
                    return result.FailMessage("containersize == 0");
            }

            // Next, we will parse the mediaTypeKey - note that the designers were a bit lazy here
            // They did not want to make the parser more complex so instead of using perfectly capable XML, they went their own way.
            // Like done here with the custom XML parser and HTTP client... (if they had cared more about the standards that might have been redundant)
            MatchCollection mediaTypeMatches = mediaTypeKeyRegex.Matches(mediaTypeKey);
            if (mediaTypeMatches == null)
                return result.FailMessage("The mediatypekey match collection is null!");

            // Allocate the results dictionary
            Dictionary<uint, string> mediaTypeDict = new Dictionary<uint, string>();

            // Loop through the mediatypekeys
            foreach (Match mediaTypeMatch in mediaTypeMatches)
            {
                // Some validation first
                // Lazy syntax - we will skip invalid entries
                if (!mediaTypeMatch.Success)
                    continue;
                if (mediaTypeMatch.Groups.Count != 3)
                    continue;

                // Now, allocate key and value
                uint key;
                string value;
                
                // And extract them
                if (!uint.TryParse(mediaTypeMatch.Groups[1].Value.Trim(), out key))
                    continue;
                if (string.IsNullOrWhiteSpace((value = mediaTypeMatch.Groups[2].Value.Trim())))
                    continue;

                // Finally, store the entry
                mediaTypeDict.Add(key, value);
            }

            // Finally, return the response
            return result.Succeed(new ResponseParameters(uriPath, idMask, containerSize, mediaTypeDict, updateID));
        }

        /// <summary>
        /// RequestUriMetaData's ResponseParameters reply.
        /// </summary>
        public class ResponseParameters
        {
            /// <summary>
            /// This contains the absolute HTTP path to the root (no trailing /) of the media webserver.
            /// </summary>
            public readonly string URIPath;

            /// <summary>
            /// This ID-mask is used to get the universal IDs. Usually 16777215 and AND'ed with the node ID.
            /// </summary>
            public readonly uint IDMask;

            /// <summary>
            /// This describes the size of each folder container on the harddisk. It's usually 1000.
            /// </summary>
            public readonly uint ContainerSize;

            /// <summary>
            /// This is used to map the content type IDs to their actual file type.
            /// KeyValuePair[] of uint==string.
            /// </summary>
            public readonly Dictionary<uint, string> MediaTypeKey;

            /// <summary>
            /// UNKNOWN! e.g. 422.
            /// </summary>
            public readonly uint UpdateID;

            /// <summary>
            /// Default internal constructor.
            /// </summary>
            /// <param name="URIPath">The absolute HTTP path to the root of the media webserver.</param>
            /// <param name="IDMask">The ID-mask used to get the universal IDs.</param>
            /// <param name="ContainerSize">The size of each folder container on the harddisk.</param>
            /// <param name="MediaTypeKey">A KeyValuePair-collection that stores the supported file formats along their IDs.</param>
            /// <param name="UpdateID">Unknown ID.</param>
            internal ResponseParameters(string URIPath, uint IDMask, uint ContainerSize, Dictionary<uint, string> MediaTypeKey, uint UpdateID)
            {
                this.URIPath = URIPath;
                this.IDMask = IDMask;
                this.ContainerSize = ContainerSize;
                this.MediaTypeKey = MediaTypeKey;
                this.UpdateID = UpdateID;
            }
        }
    }
}
