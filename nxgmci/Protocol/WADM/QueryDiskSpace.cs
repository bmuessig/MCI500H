namespace nxgmci.Protocol.WADM
{
    /// <summary>
    /// This request returns both the free and used harddisk space.
    /// This information can be used to determine whether new files could be uploaded or not.
    /// </summary>
    public static class QueryDiskSpace
    {
        // DiskSpace Parser
        private readonly static WADMParser parser = new WADMParser("querydiskspace", "responseparameters", false);

        /// <summary>
        /// Assembles a QueryDiskSpace request to be passed to the stereo.
        /// </summary>
        /// <returns>A request string that can be passed to the stereo.</returns>
        public static string Build()
        {
            return "<querydiskspace></querydiskspace>";
        }

        /// <summary>
        /// Parses QueryDiskSpace's ResponseParameters and returns the result.
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

            // Now, make sure our mandatory arguments exist
            if (!parserResult.Product.Elements.ContainsKey("size"))
                return Result<ResponseParameters>.FailMessage(result, "Could not locate parameter '{0}'!", "size");
            if (!parserResult.Product.Elements.ContainsKey("totalsize"))
                return Result<ResponseParameters>.FailMessage(result, "Could not locate parameter '{0}'!", "totalsize");
            
            // Then, try to parse the parameters
            ulong size, totalSize;
            if (!ulong.TryParse(parserResult.Product.Elements["size"], out size))
                return Result<ResponseParameters>.FailMessage(result, "Could not parse parameter '{0}' as ulong!", "size");
            if (!ulong.TryParse(parserResult.Product.Elements["totalsize"], out totalSize))
                return Result<ResponseParameters>.FailMessage(result, "Could not parse parameter '{0}' as ulong!", "totalsize");

            // Next, we will may have to perform some sanity checks
            if (ValidateInput)
            {
                if (size > totalSize)
                    return Result<ResponseParameters>.FailMessage(result, "size < totalsize");
                if (totalSize == 0)
                    return Result<ResponseParameters>.FailMessage(result, "size == 0");
            }

            // Finally, return the response
            return Result<ResponseParameters>.SucceedProduct(result, new ResponseParameters(size, totalSize));
        }

        /// <summary>
        /// QueryDiskSpace's ResponseParameters reply.
        /// </summary>
        public class ResponseParameters
        {
            /// <summary>
            /// Free harddisk space in bytes.
            /// </summary>
            public readonly ulong Size;

            /// <summary>
            /// Total harddisk space in bytes.
            /// </summary>
            public readonly ulong TotalSize;

            /// <summary>
            /// Default internal constructor.
            /// </summary>
            /// <param name="Size">Free harddisk space in bytes.</param>
            /// <param name="TotalSize">Total harddisk space in bytes.</param>
            internal ResponseParameters(ulong Size, ulong TotalSize)
            {
                this.Size = Size;
                this.TotalSize = TotalSize;
            }
        }
    }
}
