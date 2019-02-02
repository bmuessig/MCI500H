namespace nxgmci.Protocol.WADM
{
    /// <summary>
    /// This enum provides a list of status codes returned by the WADM API.
    /// </summary>
    public enum StatusCode
    {
        None,
        Success,
        Failure,
        Unknown,
        Busy,
        ParameterError,
        UpdateIDMismatch,
        InvalidIndex,
        DatabaseFull,
        Idle
    }
}
