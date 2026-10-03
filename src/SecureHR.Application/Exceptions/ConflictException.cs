namespace SecureHR.Application.Exceptions
{
    /// <summary>
    /// The request conflicts with the current state of the data
    /// (e.g. a duplicate email or name). Returned to clients as HTTP 409.
    /// </summary>
    public class ConflictException(string message) : Exception(message)
    {
    }
}
