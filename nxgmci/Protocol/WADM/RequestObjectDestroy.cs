﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nxgmci.Protocol.WADM
{
    /// <summary>
    /// Attempts to delete a media file.
    /// </summary>
    public class RequestObjectDestroy
    {
        // RequestObjectDestroy Parser
        private readonly static WADMParser parser = new WADMParser("requestobjectdestroy", "responseparameters", false);

        /// <summary>
        /// Assembles a RequestObjectDestroy request to be passed to the stereo.
        /// </summary>
        /// <param name="UpdateID">The modification update ID passed as a token.</param>
        /// <param name="Index">The universal index of the media file to be deleted.</param>
        /// <returns>A request string that can be passed to the stereo.</returns>
        public static string Build(uint UpdateID, uint Index)
        {
            // Sanity check the input
            if (Index == 0)
                return null;

            // And build the request
            return string.Format(
                "<requestobjectdestroy><requestparameters>" +
                "<updateid>{0}</updateid>" +
                "<index>{1}</index>" +
                "</requestparameters></requestobjectdestroy>",
                UpdateID,
                Index);
        }

        /// <summary>
        /// Parses RequestObjectDestroy's ResponseParameters and returns the result.
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
            if (!parserResult.Product.Elements.ContainsKey("updateid"))
                return Result<ResponseParameters>.FailMessage(result, "Could not locate parameter '{0}'!", "updateid");

            // Then, try to parse the parameters
            uint index, updateID;

            if (!uint.TryParse(parserResult.Product.Elements["index"], out index))
                return Result<ResponseParameters>.FailMessage(result, "Could not parse parameter '{0}' as uint!", "index");
            if (!uint.TryParse(parserResult.Product.Elements["updateid"], out updateID))
                return Result<ResponseParameters>.FailMessage(result, "Could not parse parameter '{0}' as uint!", "updateid");

            // Finally, return the response
            return Result<ResponseParameters>.SucceedProduct(result, new ResponseParameters(index, statusResult.Product, updateID));
        }

        /// <summary>
        /// RequestObjectDestroy's ResponseParameters reply.
        /// </summary>
        public class ResponseParameters
        {
            /// <summary>
            /// The universal index of the media file to be deleted.
            /// </summary>
            public readonly uint Index;

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
            /// <param name="Index">The universal index of the media file to be deleted.</param>
            /// <param name="Status">The status code returned for the query.</param>
            /// <param name="UpdateID">The modification update ID passed as a token. Equal to the originally supplied update ID + 1.</param>
            internal ResponseParameters(uint Index, WADMStatus Status, uint UpdateID)
            {
                // Sanity check the input
                if (Status == null)
                    throw new ArgumentNullException("Status");

                this.Index = Index;
                this.Status = Status;
                this.UpdateID = UpdateID;
            }
        }
    }
}
