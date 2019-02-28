using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace nxgmci.Protocol.WADM
{
    /// <summary>
    /// The exact function of this class is still unknown.
    /// </summary>
    public static class RequestSubscribe
    {
        // RequestSubscribe Parser
        private readonly static WADMParser parser = new WADMParser("requestsubscribe", "responseparameters", false);

        /// <summary>
        /// Assembles a RequestSubscribe request to be passed to the stereo.
        /// </summary>
        /// <returns>A request string that can be passed to the stereo.</returns>
        public static string Build(IPAddress DeliveryIP, ushort DeliveryPort, uint Timeout)
        {
            // If no IP is provided, give 0.0.0.0
            // It might be better to just return null and cancel
            if (DeliveryIP == null)
                DeliveryIP = new IPAddress(new byte[] {0, 0, 0, 0});
            
            // And build the request
            return string.Format(
                "<requestsubscribe><requestparameters>" +
                "<deliveryip>{0}</deliveryip>" +
                "<deliveryport>{1}</deliveryport>" +
                "<timeout>{2}</timeout>" +
                "</requestparameters></requestsubscribe>",
                WADMParser.TrimValue(WADMParser.EncodeValue(DeliveryIP.ToString()), true),
                DeliveryPort,
                Timeout);
        }
        
        /// <summary>
        /// Parses RequestSubscribe's ResponseParameters and returns the result.
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

            // Finally, return the response
            return Result<ResponseParameters>.SucceedProduct(result, new ResponseParameters(statusResult.Product));
        }

        /// <summary>
        /// RequestSubscribe's ResponseParameters reply.
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
