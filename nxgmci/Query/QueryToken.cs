using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nxgmci.Query
{
    internal class QueryToken
    {

        internal enum TokenIntent : byte
        {
            Unknown,
            
            ConstantNumber,
            ConstantString,

            VariableDisplayed,
            VariableHidden,
            
            StringAdd,
            StringSubtract,
            StringEqual,
            StringStartsWith,
            StringEndsWith,
            StringContains,
            StringNotEmpty,
            StringToUppercase,
            StringToLowercase,

            NumbersAdd,
            NumbersSubtract,
            NumberEqual,
            NumberGreater,
            NumberGreaterEqual,
            NumberLess,
            NumberLessEqual,
            NumberGreaterThanZero,

            IntentCount
        }
    }
}
