namespace nxgmci.Protocol.WADM
{
    /// <summary>
    /// This request returns the current update ID. This update ID has to be included into all destructive requests.
    /// The update ID automatically increments by one after it has been used.
    /// </summary>
    public static class GetUpdateID
    {
        // GetUpdateID Parser
        private readonly static WADMParser parser = new WADMParser("getupdateid", "responseparameters", false);

        /// <summary>
        /// Assembles a GetUpdateID request to be passed to the stereo.
        /// </summary>
        /// <returns>A request string that can be passed to the stereo.</returns>
        public static string Build()
        {
            return "<getupdateid></getupdateid>";
        }

        /// <summary>
        /// Parses GetUpdateID's ResponseParameters and returns the result.
        /// </summary>
        /// <param name="Response">The response received from the stereo.</param>
        /// <param name="LazySyntax">Indicates whether to ignore minor syntax errors.</param>
        /// <returns>A result object that contains a serialized version of the response data.</returns>
        public static Result<ResponseParameters> Parse(string Response, bool LazySyntax = false)
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

            // And also make sure the state is correct
            if (parserResult.Product.Elements == null)
                return result.FailMessage("The list of parsed elements is null!");

            // Now, make sure the mandatory argument exist
            if (!parserResult.Product.Elements.ContainsKey("updateid"))
                return result.FailMessage("Could not locate parameter '{0}'!", "updateid");
            
            // Then, try to parse the parameter
            uint updateID;
            if (!uint.TryParse(parserResult.Product.Elements["updateid"], out updateID))
                return result.FailMessage("Could not parse parameter '{0}' as uint!", "updateid");

            // Finally, return the response
            return result.Succeed(new ResponseParameters(updateID));
        }

        /// <summary>
        /// GetUpdateID's ResponseParameters reply.
        /// </summary>
        public class ResponseParameters
        {
            /// <summary>
            /// The current update ID.
            /// </summary>
            public readonly uint UpdateID;

            /// <summary>
            /// Default internal constructor.
            /// </summary>
            /// <param name="UpdateID">The current update ID.</param>
            internal ResponseParameters(uint UpdateID)
            {
                this.UpdateID = UpdateID;
            }
        }
    }
}
