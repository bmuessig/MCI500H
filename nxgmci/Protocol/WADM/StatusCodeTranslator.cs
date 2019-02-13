namespace nxgmci.Protocol.WADM
{
    /// <summary>
    /// This class provides functions to convert between the internal enum based error codes and the strings used by the WADM API.
    /// </summary>
    public static class StatusCodeTranslator
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

                case "updateidmismatch": // CAUTON, TODO, BUG: These are actually "reasons", returned in a different field...
                    return StatusCode.UpdateIDMismatch;

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

                case StatusCode.UpdateIDMismatch:
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
