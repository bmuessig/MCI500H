using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nxgmci.Protocol
{
    public static class StatusCodeMediator
    {
        /// <summary>
        /// Attempts to parse a string to a status code.
        /// </summary>
        /// <param name="Code">String to parse</param>
        /// <returns>The matching status code or None if no one was found.</returns>
        public static StatusCode Parse(string Code)
        {
            if (string.IsNullOrWhiteSpace(Code))
                return StatusCode.None;

            // Sanitize the input further
            Code = Code.Trim().ToLower();

            // Find the matching code
            switch(Code)
            {
                case "success":
                    return StatusCode.Success;

                case "fail":
                    return StatusCode.Failure;

                case "unknown":
                    return StatusCode.Unknown;

                case "busy":
                    return StatusCode.Busy;

                case "parametererror":
                    return StatusCode.ParameterError;

                case "updateidmismatch":
                    return StatusCode.UpdateIdMismatch;

                case "invalidindex":
                    return StatusCode.InvalidIndex;

                case "databasefull":
                    return StatusCode.DatabaseFull;

                case "idle":
                    return StatusCode.Idle;

                default:
                    return StatusCode.None;
            }
        }

        /// <summary>
        /// Returns the official string representation of the status code.
        /// </summary>
        /// <param name="Code">Status code input</param>
        /// <returns>String representation of the status code input. Returns am empty string on error.</returns>
        public static string Stringify(StatusCode Code)
        {
            // Find the matching string
            switch (Code)
            {
                case StatusCode.Success:
                    return "success";

                case StatusCode.Failure:
                    return "fail";

                case StatusCode.Unknown:
                    return "unknown";

                case StatusCode.Busy:
                    return "busy";

                case StatusCode.ParameterError:
                    return "parametererror";

                case StatusCode.UpdateIdMismatch:
                    return "updateidmismatch";

                case StatusCode.InvalidIndex:
                    return "invalidindex";

                case StatusCode.DatabaseFull:
                    return "databasefull";

                case StatusCode.Idle:
                    return "idle";

                default:
                    return string.Empty;
            }
        }
    }
}
