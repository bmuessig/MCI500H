using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nxgmci
{
    /// <summary>
    /// Provides result data for events.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ResultEventArgs<T> : EventArgs
    {
        /// <summary>
        /// Stores the result of the operation signaled by the event.
        /// </summary>
        public Result<T> Result { get; private set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="Result">The result of the operation signaled by the event.</param>
        public ResultEventArgs(Result<T> Result)
        {
            this.Result = Result;
        }
    }
}
