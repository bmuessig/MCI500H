namespace nxgmci.Protocol.WADM
{
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
}
