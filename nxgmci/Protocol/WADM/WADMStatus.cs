﻿using System;
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
        /// <summary>
        /// Stores the status code returned for an operation.
        /// </summary>
        public readonly StatusCode Status;
        
        /// <summary>
        /// Stores the raw status code returned for the operation. This is primarily used for debugging purposes.
        /// </summary>
        public readonly string RawStatus;

        /// <summary>
        /// Stores the error reason code returned for an operation.
        /// </summary>
        public readonly ErrorReason Reason;
        
        /// <summary>
        /// Stores the raw error reason code returned for the operation. This is primarily used for debugging purposes.
        /// </summary>
        public readonly string RawReason;

        /// <summary>
        /// Partial internal constructor. Used for status codes without reason.
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
        /// Full internal constructor. Used for status codes with reason.
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

        /// <summary>
        /// Returns a string representation of the object.
        /// </summary>
        /// <returns>A string representation of the object.</returns>
        public override string ToString()
        {
            // Allocate some temporary storage strings
            string statusText = string.Empty, reasonText = string.Empty;

            // Check for the best status descriminator
            if (Status == StatusCode.None)
            {
                if (!string.IsNullOrWhiteSpace(RawStatus))
                    statusText = RawStatus.Trim();
                else
                    statusText = Status.ToString();
            }
            else
                statusText = Status.ToString();

            // Check for the best reason descriminator
            if (Reason == ErrorReason.None)
            {
                if (!string.IsNullOrWhiteSpace(RawReason))
                    reasonText = RawReason.Trim();
            }
            else
                reasonText = Reason.ToString();

            // Format the output differently depending on the existence of a reason
            return string.Format(string.IsNullOrWhiteSpace(reasonText) ? "{0}" : "{0}: {1}", statusText, reasonText);
        }

        /// <summary>
        /// This should attempt to parse the status fields before all other code in the requests runs.
        /// By checking the status field first, it can be determined if a reason could be expected.
        /// If a reason is found, it is added to this class.
        /// Classes that use this should use this class first to parse the status. Then they should check if the parsing succeeded.
        /// If not, exit. If it suceeded, check the status code and proceed from there.
        /// </summary>
        /// <param name="NodeElements">The parser collection of node elements.</param>
        /// <param name="FailOnUnknown">Indicates whether to fail on an unknown status code or reason code.</param>
        /// <returns>A result object that contains a serialized version of the response data.</returns>
        public static Result<WADMStatus> Parse(Dictionary<string, string> NodeElements, bool FailOnUnknown = true)
        {
            // Allocate the result object
            Result<WADMStatus> result = new Result<WADMStatus>();

            // Perform input sanity checks
            if (NodeElements == null)
                return Result<WADMStatus>.FailMessage(result, "The dictionary of node elements is null!");
            if (NodeElements.Count == 0)
                return Result<WADMStatus>.FailMessage(result, "The dictionary of node elements is empty!");
                
            // Allocate storage variables
            string rawStatus, rawReason;
            bool hasReason;

            // Check, if the status field can be found
            if (!NodeElements.ContainsKey("status"))
                return Result<WADMStatus>.FailMessage(result, "Could not locate parameter '{0}'!", "status");

            // Check, if the status field is valid
            if (string.IsNullOrWhiteSpace(NodeElements["status"]))
                return Result<WADMStatus>.FailMessage(result, "Could not detect parameter '{0}' as string!", "status");

            // Copy the status string
            rawStatus = NodeElements["status"].Trim();

            // Check, if there is a reason field
            if ((hasReason = NodeElements.ContainsKey("reason")))
            {
                // Check, if the reason field is valid
                if (string.IsNullOrWhiteSpace(NodeElements["reason"]))
                    return Result<WADMStatus>.FailMessage(result, "Could not detect parameter '{0}' as string!", "reason");

                // Copy the status string
                rawReason = NodeElements["reason"].Trim();
            }
            else
                rawReason = string.Empty;

            // Parse the status field
            StatusCode status = ParseStatus(rawStatus);

            // Check, if the status code is unknown
            if (status == StatusCode.None)
            {
                // If the status code might not be unknown, fail
                if (FailOnUnknown)
                    return Result<WADMStatus>.FailMessage(result, "The status code could not be parsed!");
                else if (hasReason) // Otherwise return partial success (with reason)
                    return Result<WADMStatus>.SucceedProduct(result, new WADMStatus(StatusCode.None, rawStatus, ParseReason(rawReason), rawReason),
                        "The status code is unknown, but the syntax is valid!");
                else // Otherwise return partial success (without reason)
                    return Result<WADMStatus>.SucceedProduct(result, new WADMStatus(StatusCode.None, rawStatus),
                        "The status code is unknown, but the syntax is valid!");
            }

            // If there is no reason, return early
            if (!hasReason)
                return Result<WADMStatus>.SucceedProduct(result, new WADMStatus(status, rawStatus));

            // Parse the reason
            ErrorReason reason = ParseReason(rawReason);
            
            // Check, if the reason is known, return success
            if (reason != ErrorReason.None)
                return Result<WADMStatus>.SucceedProduct(result, new WADMStatus(status, rawStatus, reason, rawReason));

            // Otherwise, check if an error needs to be thrown
            if (FailOnUnknown)
                return Result<WADMStatus>.FailMessage(result, "The reason code could not be parsed!");

            // If not, return partial success
            return Result<WADMStatus>.SucceedProduct(result, new WADMStatus(StatusCode.None, rawStatus, ErrorReason.None, rawReason),
                "The reason code is unknown, but the syntax is valid!");
        }

        /// <summary>
        /// Attempts to parse a string to a status code.
        /// </summary>
        /// <param name="Code">String to parse</param>
        /// <returns>The matching status code or None if no one was found.</returns>
        public static StatusCode ParseStatus(string Code)
        {
            if (string.IsNullOrWhiteSpace(Code))
                return StatusCode.None;

            // Sanitize the input further
            Code = Code.Trim().ToLower();

            // Find the matching code
            switch (Code)
            {
                // Verified
                case "success":
                    return StatusCode.Success;

                case "fail":
                    return StatusCode.Failure;

                case "busy":
                    return StatusCode.Busy;

                // Unknown
                case "unknown":
                    return StatusCode.Unknown;

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
        public static string StringifyStatus(StatusCode Code)
        {
            // Find the matching string
            switch (Code)
            {
                // Verified
                case StatusCode.Success:
                    return "success";

                case StatusCode.Failure:
                    return "fail";

                case StatusCode.Busy:
                    return "busy";

                // Unknown
                case StatusCode.Unknown:
                    return "unknown";

                case StatusCode.Idle:
                    return "idle";

                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Parses the reason and returns the detected enum.
        /// </summary>
        /// <param name="Reason">Raw reason string to parse.</param>
        /// <returns>Reason enum value. Returns ErrorReason.None on error.</returns>
        private static ErrorReason ParseReason(string Reason)
        {
            // Normalize and verify the input
            if (string.IsNullOrWhiteSpace(Reason))
                return ErrorReason.None;
            Reason = Reason.Trim().ToLower();

            // Parse the status field
            switch (Reason)
            {
                // Verified
                case "updateidmismatch":
                    return ErrorReason.UpdateIDMismatch;

                case "invalidindex":
                    return ErrorReason.InvalidIndex;

                case "invalidmediatype":
                    return ErrorReason.InvalidMediaType;

                case "fieldtagmissing":
                    return ErrorReason.FieldTagMissing;

                // Unknown
                case "databasefull":
                    return ErrorReason.DatabaseFull;

                case "parametererror":
                    return ErrorReason.ParameterError;
            }

            return ErrorReason.None;
        }

        /// <summary>
        /// Returns the official string representation of the reason.
        /// </summary>
        /// <param name="Code">Status code input</param>
        /// <returns>String representation of the status code input. Returns an empty string on error.</returns>
        public static string StringifyReason(ErrorReason Code)
        {
            // Find the matching string
            switch (Code)
            {
                // Verified
                case ErrorReason.UpdateIDMismatch:
                    return "updateidmismatch";

                case ErrorReason.InvalidIndex:
                    return "invalidindex";

                case ErrorReason.InvalidMediaType:
                    return "invalidmediatype";

                case ErrorReason.FieldTagMissing:
                    return "fieldtagmissing";

                // Unknown
                case ErrorReason.DatabaseFull:
                    return "databasefull";

                case ErrorReason.ParameterError:
                    return "parametererror";

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
            /// The device is busy.
            /// </summary>
            Busy,

            // Unknown

            /// <summary>
            /// The operation failed due to an unknown error.
            /// </summary>
            Unknown,

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
            /// <summary>
            /// No status code was returned.
            /// </summary>
            None,

            // Verified
            
            /// <summary>
            /// The supplied media type was invalid.
            /// </summary>
            InvalidMediaType,

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
            /// The field tag of an RequestObjectUpdate is unset.
            /// </summary>
            FieldTagMissing,

            // Unknown

            /// <summary>
            /// The operation failed, as one or more parameters is invalid.
            /// </summary>
            ParameterError,

            /// <summary>
            /// The operation failed due to an unknown error.
            /// </summary>
            Unknown,

            /// <summary>
            /// The device is idle.
            /// </summary>
            Idle
        }
    }
}
