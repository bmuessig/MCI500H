using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nxgmci.Query
{
    internal class QueryToken
    {
        public string ContentText;
        public int ContentNumber;
        public TokenIntent Intent;
        public uint Position;

        public enum TokenIntent : byte
        {
            Unknown,
            Invalid,
            
            Operator,

            ConstantNumber,
            ConstantString,

            VariableDisplayed,
            VariableHidden,

            ParanthesisOpening,
            ParanthesisClosing,

            Or,
            And,
            
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
            NumberModuloNot,
            NumberModulo,

            IntentCount
        }
    }
}
