using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nxgmci.Protocol.WADM
{
    /// <summary>
    /// This API call allows adding tracks to playlists, as well as changing the order of tracks in playlists.
    /// </summary>
    public static class RequestPlaylistTrackInsert
    {
        // RequestPlaylistTrackInsert Parser
        private readonly static WADMParser parser = new WADMParser("requestplaylisttrackinsert", "responseparameters", false);

        /// <summary>
        /// Assembles a RequestPlaylistTrackInsert request for adding tracks to playlists to be passed to the stereo.
        /// </summary>
        /// <param name="UpdateID">The modification update ID passed as a token.</param>
        /// <param name="TargetIndex">The index of the playlist to insert the track into.</param>
        /// <param name="SourceIndex">The index of the track to insert into the playlist.</param>
        /// <returns>A request string that can be passed to the stereo.</returns>
        public static string BuildAdd(uint UpdateID, uint TargetIndex, uint SourceIndex)
        {
            // Sanity check the input
            if (SourceIndex == 0 || SourceIndex == TargetIndex)
                return null;

            // And build the request
            return string.Format(
                "<requestplaylisttrackinsert><requestparameters>" +
                "<updateid>{0}</updateid>" +
                "<targetindex>{1}</targetindex>" + // The target ID uses Playlist child nodes of Playlist - as expected
                "<sourceindex>{2}</sourceindex>" + // The source ID uses the All-track -> Track namespace. TODO: Check, if others may be used.
                "<offset>-1</offset>" + // Perhaps, offset will allow you to define an order rightaway - it's usually always set to -1 though.
                "<movetrack>0</movetrack>" +
                "</requestparameters></requestplaylisttrackinsert>",
                UpdateID,
                TargetIndex,
                SourceIndex);
        }

        /// <summary>
        /// Assembles a RequestPlaylistTrackInsert request for moving playlist items to be passed to the stereo.
        /// </summary>
        /// <param name="UpdateID">The modification update ID passed as a token.</param>
        /// <param name="TargetIndex">The the parent playlist ID of the item to be moved.</param>
        /// <param name="SourceIndex">The ID of the item to be moved inside the parent's namespace.</param>
        /// <param name="Offset">Unknown offset. Might be offset from the top.</param>
        /// <returns>A request string that can be passed to the stereo.</returns>
        public static string BuildMove(uint UpdateID, uint TargetIndex, uint SourceIndex, uint Offset)
        {
            // Sanity check the input
            if (SourceIndex == 0 || SourceIndex == TargetIndex)
                return null;

            // And build the request
            return string.Format(
                "<requestplaylisttrackinsert><requestparameters>" +
                "<updateid>{0}</updateid>" +
                "<targetindex>{1}</targetindex>" + // The target ID uses Playlist child nodes of Playlist. The parent of the item to be moved.
                "<sourceindex>{2}</sourceindex>" + // The source ID uses Track child-nodes of Playlist node - TODO: Check, if others may be used.
                "<offset>{3}</offset>" + // TOOD: Figure out how this works
                "<movetrack>1</movetrack>" +
                "</requestparameters></requestplaylisttrackinsert>",
                UpdateID,
                TargetIndex,
                SourceIndex,
                Offset); // TODO: Check, if offset can be negative and how it works
        }

        /// <summary>
        /// Parses RequestPlaylistTrackInsert's ResponseParameters and returns the result.
        /// </summary>
        /// <param name="Response">The response received from the stereo.</param>
        /// <param name="ValidateInput">Indicates whether to validate the data values received.</param>
        /// <param name="LazySyntax">Indicates whether to ignore minor syntax errors.</param>
        /// <returns>A result object that contains a serialized version of the response data.</returns>
        public static Result<ResponseParameters> Parse(string Response, bool ValidateInput = true, bool LazySyntax = false)
        {
            // TODO: Check if Index is opitonal and if so, make it so

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
            if (!parserResult.Product.Elements.ContainsKey("sourceindex"))
                return Result<ResponseParameters>.FailMessage(result, "Could not locate parameter '{0}'!", "sourceindex");
            if (!parserResult.Product.Elements.ContainsKey("updateid"))
                return Result<ResponseParameters>.FailMessage(result, "Could not locate parameter '{0}'!", "updateid");

            // Then, try to parse the parameters
            uint sourceIndex, updateID;

            if (!uint.TryParse(parserResult.Product.Elements["sourceindex"], out sourceIndex))
                return Result<ResponseParameters>.FailMessage(result, "Could not parse parameter '{0}' as uint!", "sourceindex");
            if (!uint.TryParse(parserResult.Product.Elements["updateid"], out updateID))
                return Result<ResponseParameters>.FailMessage(result, "Could not parse parameter '{0}' as uint!", "updateid");

            // Finally, return the response
            return Result<ResponseParameters>.SucceedProduct(result, new ResponseParameters(sourceIndex, statusResult.Product, updateID));
        }

        /// <summary>
        /// RequestPlaylistTrackInsert's ResponseParameters reply.
        /// </summary>
        public class ResponseParameters
        {
            /// <summary>
            /// The echo of the index of the file to add.
            /// </summary>
            public readonly uint SourceIndex;

            /// <summary>
            /// The status code returned for the query.
            /// </summary>
            public readonly WADMStatus Status;

            /// <summary>
            /// The modification update ID passed as a token. Equal to the originally supplied update ID + 1.
            /// </summary>
            public readonly uint UpdateID;

            /// <summary>
            /// Default internal constructor.
            /// </summary>
            /// <param name="SourceIndex">The echo of the index of the file to add.</param>
            /// <param name="Status">The status code returned for the query.</param>
            /// <param name="UpdateID">The modification update ID passed as a token. Equal to the originally supplied update ID + 1.</param>
            internal ResponseParameters(uint SourceIndex, WADMStatus Status, uint UpdateID)
            {
                // Sanity check the input
                if (Status == null)
                    throw new ArgumentNullException("Status");

                this.SourceIndex = SourceIndex;
                this.Status = Status;
                this.UpdateID = UpdateID;
            }
        }
    }
}
