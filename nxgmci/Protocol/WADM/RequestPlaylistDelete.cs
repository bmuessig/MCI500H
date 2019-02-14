using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nxgmci.Protocol.WADM
{
    /// <summary>
    /// Attempts to delete a playlist.
    /// </summary>
    public static class RequestPlaylistDelete
    {
        // RequestPlaylistDelete Parser
        private readonly static WADMParser parser = new WADMParser("requestplaylistdelete", "responseparameters", false);

        /// <summary>
        /// Assembles a RequestPlaylistDelete request to be passed to the stereo.
        /// </summary>
        /// <param name="UpdateID">The update ID.</param>
        /// <param name="Index">The index of the playlist.</param>
        /// <param name="OriginalName">The original name of the playlist.</param>
        /// <returns>A request string that can be passed to the stereo.</returns>
        public static string Build(uint UpdateID, uint Index, string OriginalName = null)
        {
            // Normalize the names
            if (OriginalName == null)
                OriginalName = string.Empty;

            // And build the request
            return string.Format(
                "<requestplaylistdelete><requestparameters>" +
                "<updateid>{0}</updateid>" +
                "<index>{1}</index>" +
                "<originalname>{2}</originalname>" +
                "</requestparameters></requestplaylistdelete>",
                UpdateID,
                Index,
                WADMParser.TrimValue(WADMParser.EncodeValue(OriginalName), true));
        }

        /// <summary>
        /// Parses RequestPlaylistDelete's ResponseParameters and returns the result.
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
            if (!parserResult.Product.Elements.ContainsKey("index"))
                return result.FailMessage("Could not locate parameter '{0}'!", "index");
            if (!parserResult.Product.Elements.ContainsKey("status"))
                return result.FailMessage("Could not locate parameter '{0}'!", "status");
            if (!parserResult.Product.Elements.ContainsKey("updateid"))
                return result.FailMessage("Could not locate parameter '{0}'!", "updateid");

            // Then, try to parse the parameters
            StatusCode status;
            string rawStatus;
            uint index, updateID;

            if (!uint.TryParse(parserResult.Product.Elements["index"], out index))
                return result.FailMessage("Could not parse parameter '{0}' as uint!", "index");
            if (string.IsNullOrWhiteSpace(parserResult.Product.Elements["status"]))
                return result.FailMessage("Could not detect parameter '{0}' as string!", "status");
            rawStatus = parserResult.Product.Elements["status"].Trim();
            status = StatusCodeTranslator.Parse(rawStatus);
            if (!uint.TryParse(parserResult.Product.Elements["updateid"], out updateID))
                return result.FailMessage("Could not parse parameter '{0}' as uint!", "updateid");

            // Next, if desired, perform a sanity check
            if (ValidateInput && status == StatusCode.None)
                return result.FailMessage("Status code invalid!");

            // Finally, return the response
            return result.Succeed(new ResponseParameters(index, status, rawStatus, updateID));
        }

        /// <summary>
        /// RequestPlaylistDelete's ResponseParameters reply.
        /// </summary>
        public class ResponseParameters
        {
            /// <summary>
            /// The index of the playlist deleted. Contained inside the default playlist item namespace.
            /// </summary>
            public readonly uint Index;

            /// <summary>
            /// The status code returned for the query.
            /// </summary>
            public readonly StatusCode Status;

            /// <summary>
            /// Stores the raw status code string for debugging purposes.
            /// </summary>
            public readonly string RawStatus;

            /// <summary>
            /// The update ID passed as a token. Equal to the originally supplied update ID + 1.
            /// </summary>
            public readonly uint UpdateID;

            /// <summary>
            /// Default internal constructor.
            /// </summary>
            /// <param name="Index">The index of the playlist created. Contained inside the default playlist item namespace.</param>
            /// <param name="Status">The status code returned for the query.</param>
            /// <param name="RawStatus">Stores the raw status code string for debugging purposes.</param>
            /// <param name="UpdateID">The update ID passed as a token. Equal to the originally supplied update ID + 1.</param>
            internal ResponseParameters(uint Index, StatusCode Status, string RawStatus, uint UpdateID)
            {
                this.Index = Index;
                this.Status = Status;
                this.RawStatus = RawStatus;
                this.UpdateID = UpdateID;
            }
        }
    }
}
