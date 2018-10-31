using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using nxgmci.XML;

namespace nxgmci.Protocol
{
    public class QueryDiskSpace
    {
        // DiskSpace Parser
        private readonly static TinyParser parser = new TinyParser("querydiskspace", "responseparameters", false);

        // QueryDiskSpace-Reqest:
        public static string Build()
        {
            return "<querydiskspace></querydiskspace>";
        }

        // QueryDiskSpace-Response:
        // size	        (uint): Free harddisk space in bytes
        // totalsize	(uint): Total harddisk space in bytes
        public static ParseResult<ResponseParameters> Parse(string Response, bool ValidateInput = true, bool LazySyntax = false)
        {
            // Make sure the response is not null
            if (string.IsNullOrWhiteSpace(Response))
                return new ParseResult<ResponseParameters>("The response may not be null!");

            // Then, parse the response
            TinyResult result = parser.Parse(Response, LazySyntax);

            // Check if it failed
            if (!result.Success)
                if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                    return new ParseResult<ResponseParameters>(result.ErrorMessage);
                else
                    return new ParseResult<ResponseParameters>("The parsing failed for unknown reasons!");

            // And also make sure our state is correct
            if (result.Elements == null)
                return new ParseResult<ResponseParameters>("The list of parsed elements is null!");

            // Now, make sure our mandatory arguments exist
            if (!result.Elements.ContainsKey("size"))
                return new ParseResult<ResponseParameters>("Could not locate parameter 'size'!");
            if (!result.Elements.ContainsKey("totalsize"))
                return new ParseResult<ResponseParameters>("Could not locate parameter 'totalsize'!");
            
            // Then, try to parse the parameters
            ulong size, totalSize;
            if (!ulong.TryParse(result.Elements["size"], out size))
                return new ParseResult<ResponseParameters>("Could not parse parameter 'size' as ulong!");
            if (!ulong.TryParse(result.Elements["totalsize"], out totalSize))
                return new ParseResult<ResponseParameters>("Could not parse parameter 'totalsize' as ulong!");

            // Finally, return the response
            return new ParseResult<ResponseParameters>(new ResponseParameters(size, totalSize));
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
