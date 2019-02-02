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
        /// <returns></returns>
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
        public static ActionResult<ResponseParameters> Parse(string Response, bool ValidateInput = true, bool LazySyntax = false)
        {
            // Make sure the response is not null
            if (string.IsNullOrWhiteSpace(Response))
                return new ActionResult<ResponseParameters>("The response may not be null!");

            // Then, parse the response
            Result<WADMProduct> result = parser.Parse(Response, LazySyntax);

            // Check if it failed
            if (!result.Success)
                if (!string.IsNullOrWhiteSpace(result.Message))
                    return new ActionResult<ResponseParameters>(result.ToString());
                else
                    return new ActionResult<ResponseParameters>("The parsing failed for unknown reasons!");

            // Make sure the product is there
            if (result.Product == null)
                return new ActionResult<ResponseParameters>("The parsing product was null!");

            // And also make sure our state is correct
            if (result.Product.Elements == null)
                return new ActionResult<ResponseParameters>("The list of parsed elements is null!");

            // Now, make sure our mandatory arguments exist
            if (!result.Product.Elements.ContainsKey("size"))
                return new ActionResult<ResponseParameters>(string.Format("Could not locate parameter '{0}'!", "size"));
            if (!result.Product.Elements.ContainsKey("totalsize"))
                return new ActionResult<ResponseParameters>(string.Format("Could not locate parameter '{0}'!", "totalsize"));
            
            // Then, try to parse the parameters
            ulong size, totalSize;
            if (!ulong.TryParse(result.Product.Elements["size"], out size))
                return new ActionResult<ResponseParameters>(string.Format("Could not parse parameter '{0}' as ulong!", "size"));
            if (!ulong.TryParse(result.Product.Elements["totalsize"], out totalSize))
                return new ActionResult<ResponseParameters>(string.Format("Could not parse parameter '{0}' as ulong!", "totalsize"));

            // Next, we will may have to perform some sanity checks
            if (ValidateInput)
            {
                if (size > totalSize)
                    return new ActionResult<ResponseParameters>("size < totalsize");
                if (totalSize == 0)
                    return new ActionResult<ResponseParameters>("size == 0");
            }

            // Finally, return the response
            return new ActionResult<ResponseParameters>(new ResponseParameters(size, totalSize));
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
