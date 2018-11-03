using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using nxgmci.Parsers;

namespace nxgmci.Protocol
{
    public class RequestUriMetaData
    {
        // This request is used to retrieve lots of important flags from the stereo
        // For instance, this will tell us the public directory of the files.
        // It will also tell us the mask we need to apply to the nodeids to get their file, as well as the directory size.
        // Apart from that it tells us what file formats are supported and returns the mapping of type-ids to extension.

        // RequestUriMetaData Parser
        private readonly static WADMParser parser = new WADMParser("requesturimetadata", "responseparameters", false);

        // MediaTypeKey Parser Regex
        private static readonly Regex mediaTypeKeyRegex = new Regex(@"(\d+)\s*=+\s*(\w+)(?:\s*,|$)",
            RegexOptions.Singleline | RegexOptions.Compiled);

        // RequestUriMetaData-Reqest:
        public static string Build()
        {
            return "<requesturimetadata></requesturimetadata>";
        }

        // RequestUriMetaData-Response:
        public static ActionResult<ResponseParameters> Parse(string Response, bool ValidateInput = true, bool LazySyntax = false)
        {
            // Make sure the response is not null
            if (string.IsNullOrWhiteSpace(Response))
                return new ActionResult<ResponseParameters>("The response may not be null!");

            // Then, parse the response
            WADMResult result = parser.Parse(Response, LazySyntax);

            // Check if it failed
            if (!result.Success)
                if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                    return new ActionResult<ResponseParameters>(result.ErrorMessage);
                else
                    return new ActionResult<ResponseParameters>("The parsing failed for unknown reasons!");

            // And also make sure our state is correct
            if (result.Elements == null)
                return new ActionResult<ResponseParameters>("The list of parsed elements is null!");

            // Now, make sure our mandatory arguments exist
            if (!result.Elements.ContainsKey("uripath"))
                return new ActionResult<ResponseParameters>(string.Format("Could not locate parameter '{0}'!", "uripath"));
            if (!result.Elements.ContainsKey("idmask"))
                return new ActionResult<ResponseParameters>(string.Format("Could not locate parameter '{0}'!", "idmask"));
            if (!result.Elements.ContainsKey("containersize"))
                return new ActionResult<ResponseParameters>(string.Format("Could not locate parameter '{0}'!", "containersize"));
            if (!result.Elements.ContainsKey("mediatypekey"))
                return new ActionResult<ResponseParameters>(string.Format("Could not locate parameter '{0}'!", "mediatypekey"));
            if (!result.Elements.ContainsKey("updateid"))
                return new ActionResult<ResponseParameters>(string.Format("Could not locate parameter '{0}'!", "updateid"));

            // Then, try to parse the parameters
            string uriPath, mediaTypeKey;
            uint idMask, containerSize, updateID;
            if (string.IsNullOrWhiteSpace((uriPath = result.Elements["uripath"])))
                return new ActionResult<ResponseParameters>(string.Format("Could not parse parameter '{0}' as string!", "uripath"));
            if (!uint.TryParse(result.Elements["idmask"], out idMask))
                return new ActionResult<ResponseParameters>(string.Format("Could not parse parameter '{0}' as uint!", "idmask"));
            if (!uint.TryParse(result.Elements["containersize"], out containerSize))
                return new ActionResult<ResponseParameters>(string.Format("Could not parse parameter '{0}' as uint!", "containersize"));
            if (string.IsNullOrWhiteSpace((mediaTypeKey = result.Elements["mediatypekey"])))
                return new ActionResult<ResponseParameters>(string.Format("Could not parse parameter '{0}' as string!", "mediatypekey"));
            if (!uint.TryParse(result.Elements["updateid"], out updateID))
                return new ActionResult<ResponseParameters>(string.Format("Could not parse parameter '{0}' as uint!", "updateid"));

            // We may have to perform some sanity checks
            if (ValidateInput)
            {
                if (string.IsNullOrWhiteSpace(uriPath))
                    return new ActionResult<ResponseParameters>("uripath is null or white-space!");
                if (string.IsNullOrWhiteSpace(mediaTypeKey))
                    return new ActionResult<ResponseParameters>("mediatypekey is null or white-space!");
                if (idMask == 0)
                    return new ActionResult<ResponseParameters>("idmask == 0");
                if (containerSize == 0)
                    return new ActionResult<ResponseParameters>("containersize == 0");
            }

            // Next, we will parse the mediaTypeKey - note that the designers were a bit lazy here
            // They did not want to make the parser more complex so instead of using perfectly capable XML, they went their own way.
            // Like done here with the custom XML parser and HTTP client... (if they had cared more about the standards that might have been redundant)
            MatchCollection mediaTypeMatches = mediaTypeKeyRegex.Matches(mediaTypeKey);
            if (mediaTypeMatches == null)
                return new ActionResult<ResponseParameters>("The mediatypekey match collection is null!");

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
            return new ActionResult<ResponseParameters>(new ResponseParameters(uriPath, idMask, containerSize, mediaTypeDict, updateID));
        }

        // RequestUriMetaData-ResponseParameters-Structure:
        // uripath		    (string):				This contains the absolute http path to the root (no trailing /) of the media webserver.
        // idmask		    (int):				    This idmask is used to get the file name from title IDs. Usually 16777215 and AND'ed with the title ID.
        // containersize    (int):				    This describes the size of each folder container on the harddisk. It's usually 1000.
        // mediatypekey	    (kvp<int==string>[]):	This is used to map the content type IDs to their actual file type.
        // updateid		    (int):				    UNKNOWN! e.g. 422
        public class ResponseParameters
        {
            public readonly string URIPath;
            public readonly uint IDMask;
            public readonly uint ContainerSize;
            public readonly Dictionary<uint, string> MediaTypeKey;
            public readonly uint UpdateID;

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
