using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nxgmci
{
    [Obsolete("This is now obsolete and has been replaced by Result<T>.")]
    public class ActionResult<T>
    {
        public readonly bool Success;
        public readonly string ErrorMessage;
        public readonly T Result;

        public ActionResult(T Result)
        {
            this.Success = true;
            this.Result = Result;
        }

        public ActionResult(string ErrorMessage)
        {
            this.Success = false;
            this.ErrorMessage = ErrorMessage;
        }
    }
}
