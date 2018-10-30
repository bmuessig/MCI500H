using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nxgmci.WADM
{
    public class ParseResult<T>
    {
        public readonly bool Success;
        public readonly string ErrorMessage;
        public readonly T Result;

        public ParseResult(T Result)
        {
            this.Success = true;
            this.Result = Result;
        }

        public ParseResult(string ErrorMessage)
        {
            this.Success = false;
            this.ErrorMessage = ErrorMessage;
        }
    }
}
