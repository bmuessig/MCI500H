using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nxgmci.Query
{
    internal class QueryInstruction
    {
        internal enum Operation : byte
        {
            /// <summary>
            /// Perform no operation.
            /// </summary>
            None,

            /// <summary>
            /// Pop two values, add them and push the result.
            /// </summary>
            Add,

            /// <summary>
            /// Pop two values, subtract the top value from the bottom value and push the result.
            /// </summary>
            Subtract,

            /// <summary>
            /// Pushes the signed integer value.
            /// This value can be a bool, int or a positive (1 based) index into the string table or a negative (-1 based) index into the variable table.
            /// The actual function and type is determined by the instruction used to work with the number.
            /// </summary>
            Push,

            /// <summary>
            /// Pops the topmost stack value and destroys it.
            /// </summary>
            Pop,

            /// <summary>
            /// Duplicates the topmost stack value.
            /// </summary>
            Duplicate,

            /// <summary>
            /// Swaps the two topmost stack values.
            /// </summary>
            Swap,

            /// <summary>
            /// Pops the value and pushes the negated bool value. If the original value is less than or equal to 0, 1 is pushed.
            /// Otherwise 0 will be pushed.
            /// </summary>
            Not,

            /// <summary>
            /// Pops two bool values and pushes the bool value 1 if the two were were both 1.
            /// Otherwise 0 is pushed.
            /// </summary>
            And,

            /// <summary>
            /// Pops two bool values and pushes the bool value 1 if one or both values were 1.
            /// If both values were 0, 0 is pushed.
            /// </summary>
            Or,

            /// <summary>
            /// Pushes a bool value of 1 if the active string was not empty.
            /// If the string is null or whitespace, 0 is pushed.
            /// </summary>
            StringNotEmpty,

            /// <summary>
            /// Pushes a bool value of 1 if the second string contains the active string.
            /// </summary>
            StringContains,

            /// <summary>
            /// Pushes a bool value of 1 if the second string starts with the active string.
            /// </summary>
            StringStartsWith,

            /// <summary>
            /// Pushes a bool value of 1 if the second string ends with the active string.
            /// </summary>
            StringEndsWith,

            /// <summary>
            /// Pushes a bool value of 1 if the two strings are equal.
            /// </summary>
            StringEquals,

            /// <summary>
            /// Converts the active string to lowercase.
            /// </summary>
            StringLowercase,

            /// <summary>
            /// Converts the active string to uppercase.
            /// </summary>
            StringUppercase,

            /// <summary>
            /// Pops a value, converts it to a string and loads it into the active string.
            /// </summary>
            StringNumber,

            /// <summary>
            /// Appends the active string to the second one.
            /// </summary>
            StringAdd,

            /// <summary>
            /// Subtracts the active string from the second one.
            /// This works by replacing all instances of the active string in the second string with void.
            /// </summary>
            StringSubtract,

            /// <summary>
            /// Loads the constant or variable into the active string.
            /// </summary>
            StringLoadActive,

            /// <summary>
            /// Loads the constant or variable into the second string.
            /// </summary>
            StringLoadSecond,

            /// <summary>
            /// Copies the active string to the second one.
            /// </summary>
            StringDuplicate,

            /// <summary>
            /// Swaps the active string with the second one.
            /// </summary>
            StringSwap,

            /// <summary>
            /// Pops two values and compares the two.
            /// Pushes a bool value of 1 if the two values were equal.
            /// </summary>
            Equal,

            /// <summary>
            /// Pops two values and compares the two.
            /// Pushes a bool value of 1 if the bottom value was greater than the top value.
            /// </summary>
            Greater,

            /// <summary>
            /// Pops two values and compares the two.
            /// Pushes a bool value of 1 if the bottom value was greater than or equal to the top value.
            /// </summary>
            GreaterEqual,

            /// <summary>
            /// Pops two values and compares the two.
            /// Pushes a bool value of 1 if the bottom value was less than the top value.
            /// </summary>
            Less,

            /// <summary>
            /// Pops two values and compares the two.
            /// Pushes a bool value of 1 if the bottom value was less than or equal to the top value.
            /// </summary>
            LessEqual,

            /// <summary>
            /// This enum value is equal to the number of valid instructions.
            /// </summary>
            InstructionCount
        }
    }
}
