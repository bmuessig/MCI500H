using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nxgmci.Protocol.WADM
{
    /// <summary>
    /// Attempts to rename a playlist.
    /// </summary>
    public static class RequestPlaylistRename
    {
        // RequestPlaylistRename Parser
        private readonly static WADMParser parser = new WADMParser("requestplaylistrename", "responseparameters", false);

        /// <summary>
        /// Assembles a RequestPlaylistRename request to be passed to the stereo.
        /// If the new playlist name is null or white-space it will be replaced with 'New Playlist'.
        /// </summary>
        /// <param name="UpdateID">The update ID.</param>
        /// <param name="Index">The index of the playlist.</param>
        /// <param name="Name">The new name of the playlist.</param>
        /// <param name="OriginalName">The original name of the playlist.</param>
        /// <returns>A request string that can be passed to the stereo.</returns>
        public static string Build(uint UpdateID, uint Index, string Name, string OriginalName = null)
        {
            // Normalize and sanity check the names
            if (OriginalName == null)
                return null;
            if (string.IsNullOrWhiteSpace(Name))
                Name = "New Playlist";
            else
                Name = Name.Trim();

            // And build the request
            return string.Format(
                "<requestplaylistrename><requestparameters>" +
                "<updateid>{0}</updateid>" +
                "<index>{1}</index>" +
                "<originalname>{2}</originalname>" +
                "<name>{3}</name>" +
                "</requestparameters></requestplaylistrename>",
                UpdateID,
                Index,
                WADMParser.TrimValue(WADMParser.EncodeValue(OriginalName), true),
                WADMParser.TrimValue(WADMParser.EncodeValue(Name), true));
        }

        /// <summary>
        /// Parses RequestPlaylistRename's ResponseParameters and returns the result.
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

            // And also make sure our state is correct
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
            if (!parserResult.Product.Elements.ContainsKey("index"))
                return Result<ResponseParameters>.FailMessage(result, "Could not locate parameter '{0}'!", "index");
            if (!parserResult.Product.Elements.ContainsKey("name"))
                return Result<ResponseParameters>.FailMessage(result, "Could not locate parameter '{0}'!", "name");
            if (!parserResult.Product.Elements.ContainsKey("offset"))
                return Result<ResponseParameters>.FailMessage(result, "Could not locate parameter '{0}'!", "offset");
            if (!parserResult.Product.Elements.ContainsKey("updateid"))
                return Result<ResponseParameters>.FailMessage(result, "Could not locate parameter '{0}'!", "updateid");

            // Then, try to parse the parameters
            string name;
            uint index, updateID;
            int offset;

            if (!uint.TryParse(parserResult.Product.Elements["index"], out index))
                return Result<ResponseParameters>.FailMessage(result, "Could not parse parameter '{0}' as uint!", "index");
            if (parserResult.Product.Elements["name"] != null)
                name = parserResult.Product.Elements["name"]; // NOTE: Trim is omitted here because whitespace might be part of the name
            else
                return Result<ResponseParameters>.FailMessage(result, "Could not detect parameter '{0}' as string!", "name");
            if (!int.TryParse(parserResult.Product.Elements["offset"], out offset))
                return Result<ResponseParameters>.FailMessage(result, "Could not parse parameter '{0}' as int!", "offset");
            if (!uint.TryParse(parserResult.Product.Elements["updateid"], out updateID))
                return Result<ResponseParameters>.FailMessage(result, "Could not parse parameter '{0}' as uint!", "updateid");

            // Finally, return the response
            return Result<ResponseParameters>.SucceedProduct(result, new ResponseParameters(statusResult.Product, index, name, offset, updateID));
        }

        /// <summary>
        /// RequestPlaylistCreate's ResponseParameters reply.
        /// </summary>
        public class ResponseParameters
        {
            /// <summary>
            /// The status code returned for the query.
            /// </summary>
            public readonly WADMStatus Status;

            /// <summary>
            /// The index of the playlist renamed. Contained inside the default playlist item namespace.
            /// </summary>
            public readonly uint Index;

            /// <summary>
            /// The name of the playlist renamed.
            /// </summary>
            public readonly string Name;

            /// <summary>
            /// Unknown offset. May be negative.
            /// </summary>
            public readonly int Offset;

            /// <summary>
            /// The update ID passed as a token. Equal to the originally supplied update ID + 1.
            /// </summary>
            public readonly uint UpdateID;

            /// <summary>
            /// Default internal constructor.
            /// </summary>
            /// <param name="Status">The status code returned for the query.</param>
            /// <param name="Index">The index of the playlist created. Contained inside the default playlist item namespace.</param>
            /// <param name="Name">The name of the playlist created.</param>
            /// <param name="Offset">Unknown offset. May be negative.</param>
            /// <param name="UpdateID">The update ID passed as a token. Equal to the originally supplied update ID + 1.</param>
            internal ResponseParameters(WADMStatus Status, uint Index, string Name, int Offset, uint UpdateID)
            {
                this.Status = Status;
                this.Index = Index;
                this.Name = Name;
                this.Offset = Offset;
                this.UpdateID = UpdateID;
            }
        }
    }
}
