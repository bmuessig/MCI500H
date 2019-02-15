using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace nxgmci.Protocol.WADM
{
    /// <summary>
    /// This class provides functions for parsing and representing WADM API status codes.
    /// </summary>
    public class WADMStatus
    {
        public readonly StatusCode Status;
        public readonly string RawStatus;

        public readonly ErrorReason Reason;
        public readonly string RawReason;

        /// <summary>
        /// Partial internal constructor. Used for success.
        /// </summary>
        /// <param name="Status">The status code returned.</param>
        /// <param name="RawStatus">The raw, unprocessed status string returned.</param>
        internal WADMStatus(StatusCode Status, string RawStatus)
        {
            this.Status = Status;
            this.RawStatus = RawStatus;
            this.Reason = ErrorReason.None;
            this.RawReason = string.Empty;
        }

        /// <summary>
        /// Full internal constructor. Used for failure.
        /// </summary>
        /// <param name="Status">The status code returned.</param>
        /// <param name="RawStatus">The raw, unprocessed status string returned.</param>
        /// <param name="Reason">The reason returned.</param>
        /// <param name="RawReason">The raw, unprocessed reason string returned.</param>
        internal WADMStatus(StatusCode Status, string RawStatus, ErrorReason Reason, string RawReason)
        {
            this.Status = Status;
            this.RawStatus = RawStatus;
            this.Reason = Reason;
            this.RawReason = RawReason;
        }

        // This should attempt to parse the status first (before all other code in the requests runs)
        // By checking the status field first, it can be determined if a reason could be expected
        // If a reason is found, it is added to this class
        // This class should probably be renamed to a better name like WADMStatus
        // Classes that use this should use this class first to parse the status
        // Then they check if the parsing succeeded
        // If not, exit. If it suceeded, check the status code and proceed from there.
        public static Result<WADMStatus> Parse(Dictionary<string, string> NodeElements, bool FailOnUnknown)
        {
            // Allocate the result object
            Result<WADMStatus> result = new Result<WADMStatus>();

            // Perform input sanity checks
            if (NodeElements == null)
                return result.FailMessage("The dictionary of node elements is null!");
            if (NodeElements.Count == 0)
                return result.FailMessage("The dictionary of node elements is empty!");
                
            // Allocate storage variables
            string status, reason;

            // Check, if the status field can be found
            if (!NodeElements.ContainsKey("status"))
                return result.FailMessage("Could not locate parameter '{0}'!", "status");

            // Check, if the status field is valid
            if (string.IsNullOrWhiteSpace(NodeElements["status"]))
                return result.FailMessage("Could not detect parameter '{0}' as string!", "status");

            // Copy the status string
            status = NodeElements["status"].Trim().ToLower();

            // Check, if there is a reason field
            if (NodeElements.ContainsKey("reason"))
            {
                // Check, if the reason field is valid
                if (string.IsNullOrWhiteSpace(NodeElements["reason"]))
                    return result.FailMessage("Could not detect parameter '{0}' as string!", "reason");

                // Copy the status string
                reason = NodeElements["reason"].Trim().ToLower();
            }
            else
                reason = string.Empty;

            // Parse the status field
            switch (status)
            {
                case "success":
                    return result.Succeed(new WADMStatus(StatusCode.Success, status));
                case "fail":
                    return result.Succeed(new WADMStatus(StatusCode.Failure, status, ParseReason(reason), reason));
            }

            // If the status code might not be unknown, fail
            if (FailOnUnknown)
                return result.FailMessage("The status code could not be parsed!");

            // Otherwise return partial success
            return result.Succeed(new WADMStatus(StatusCode.Unknown, status), "The status code is unknown, but the syntax is valid!");
        }

        private static ErrorReason ParseReason(string Reason)
        {
            // Normalize and verify the input
            if (string.IsNullOrWhiteSpace(Reason))
                return ErrorReason.None;
            Reason = Reason.Trim().ToLower();

            // Parse the status field
            switch (Reason)
            {
                case "invalidmediatype":
                    return ErrorReason.InvalidMediaType;
            }

            return ErrorReason.None;
        }

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
            switch (Code)
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
        
        /// <summary>
        /// This enum provides a list of status codes returned by the WADM API.
        /// </summary>
        public enum StatusCode
        {
            /// <summary>
            /// No status code was returned.
            /// </summary>
            None,

            /// <summary>
            /// The operation succeeded.
            /// </summary>
            Success,

            /// <summary>
            /// The operation failed.
            /// </summary>
            Failure,

            /// <summary>
            /// The operation failed due to an unknown error.
            /// </summary>
            Unknown,

            /// <summary>
            /// The device is busy.
            /// </summary>
            Busy,

            /// <summary>
            /// The operation failed, as one or more parameters is invalid.
            /// </summary>
            ParameterError,

            /// <summary>
            /// The operation failed, as the supplied ID did not match.
            /// </summary>
            UpdateIDMismatch,

            /// <summary>
            /// The operation failed, as the supplied index was invalid.
            /// </summary>
            InvalidIndex,

            /// <summary>
            /// The operation failed, as the database was full.
            /// </summary>
            DatabaseFull,

            /// <summary>
            /// The device is idle.
            /// </summary>
            Idle
        }

        /// <summary>
        /// This enum provides a list of status codes returned by the WADM API.
        /// </summary>
        public enum ErrorReason
        {
            // Verified
            /// <summary>
            /// The supplied media type was invalid.
            /// </summary>
            InvalidMediaType,

            // Obsolete
            /// <summary>
            /// No status code was returned.
            /// </summary>
            None,

            /// <summary>
            /// The operation succeeded.
            /// </summary>
            Success,

            /// <summary>
            /// The operation failed.
            /// </summary>
            Failure,

            /// <summary>
            /// The operation failed due to an unknown error.
            /// </summary>
            Unknown,

            /// <summary>
            /// The device is busy.
            /// </summary>
            Busy,

            /// <summary>
            /// The operation failed, as one or more parameters is invalid.
            /// </summary>
            ParameterError,

            /// <summary>
            /// The operation failed, as the supplied ID did not match.
            /// </summary>
            UpdateIDMismatch,

            /// <summary>
            /// The operation failed, as the supplied index was invalid.
            /// </summary>
            InvalidIndex,

            /// <summary>
            /// The operation failed, as the database was full.
            /// </summary>
            DatabaseFull,

            /// <summary>
            /// The device is idle.
            /// </summary>
            Idle
        }
    }
}
