namespace nxgmci.Protocol.WADM
{
    public static class QueryDiskSpace
    {
        // This request returns both free and used harddisk space.
        // We can use this information to determine whether we can upload new files or not.

        // DiskSpace Parser
        private readonly static WADMParser parser = new WADMParser("querydiskspace", "responseparameters", false);

        // QueryDiskSpace-Reqest:
        public static string Build()
        {
            return "<querydiskspace></querydiskspace>";
        }

        // QueryDiskSpace-Response:
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
            if (!result.Elements.ContainsKey("size"))
                return new ActionResult<ResponseParameters>(string.Format("Could not locate parameter '{0}'!", "size"));
            if (!result.Elements.ContainsKey("totalsize"))
                return new ActionResult<ResponseParameters>(string.Format("Could not locate parameter '{0}'!", "totalsize"));
            
            // Then, try to parse the parameters
            ulong size, totalSize;
            if (!ulong.TryParse(result.Elements["size"], out size))
                return new ActionResult<ResponseParameters>(string.Format("Could not parse parameter '{0}' as ulong!", "size"));
            if (!ulong.TryParse(result.Elements["totalsize"], out totalSize))
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

        // QueryDiskSpace-ResponseParameters-Structure:
        // size	        (ulong): Free harddisk space in bytes
        // totalsize	(ulong): Total harddisk space in bytes
        public class ResponseParameters
        {
            public readonly ulong Size;
            public readonly ulong TotalSize;

            internal ResponseParameters(ulong Size, ulong TotalSize)
            {
                this.Size = Size;
                this.TotalSize = TotalSize;
            }
        }
    }
}
