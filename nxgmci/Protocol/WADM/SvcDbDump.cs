using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nxgmci.Protocol.WADM
{
    /// <summary>
    /// This request synchronizes all pending database changes to disk.
    /// </summary>
    public class SvcDbDump
    {
        // DiskSpace Parser
        private readonly static WADMParser parser = new WADMParser("svcDbDump", "responseparameters", false);

        /// <summary>
        /// Assembles a SvcDbDump request to be passed to the stereo.
        /// </summary>
        /// <returns>A request string that can be passed to the stereo.</returns>
        public static string Build()
        {
            return "<svcDbDump></svcDbDump>";
        }

        /// <summary>
        /// Parses SvcDbDump's ResponseParameters and returns the result.
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
                if (parserResult.Error != null)
                    return result.Fail("The parsing failed!", parserResult.Error);
                else
                    return result.FailMessage("The parsing failed for unknown reasons!");

            // Make sure the product is there
            if (parserResult.Product == null)
                return result.FailMessage("The parsing product was null!");

            // And also make sure that the state is correct
            if (parserResult.Product.Elements == null)
                return result.FailMessage("The list of parsed elements is null!");
            
            // Try to parse the status
            Result<WADMStatus> statusResult = WADMStatus.Parse(parserResult.Product.Elements, ValidateInput);

            // Check if it failed
            if (!statusResult.Success)
                if (statusResult.Error != null)
                    return result.Fail("The status code parsing failed!", statusResult.Error);
                else
                    return result.FailMessage("The status code parsing failed for unknown reasons!");

            // Make sure the product is there
            if (statusResult.Product == null)
                return result.FailMessage("The status code parsing product was null!");

            // Finally, return the response
            return result.Succeed(new ResponseParameters(statusResult.Product));
        }

        /// <summary>
        /// SvcDbDump's ResponseParameters reply.
        /// </summary>
        public class ResponseParameters
        {
            /// <summary>
            /// Stores the status code returned for the operation.
            /// </summary>
            public readonly WADMStatus Status;

            /// <summary>
            /// Default internal constructor.
            /// </summary>
            /// <param name="Status">Stores the status code returned for the operation.</param>
            internal ResponseParameters(WADMStatus Status)
            {
                this.Status = Status;
            }
        }
    }
}
