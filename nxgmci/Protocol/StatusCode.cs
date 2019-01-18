using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nxgmci.Protocol
{
    public enum StatusCode
    {
        None,
        Success,
        Failure,
        Unknown,
        Busy,
        ParameterError,
        UpdateIdMismatch,
        InvalidIndex,
        DatabaseFull,
        Idle
    }
}
