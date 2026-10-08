using System.Net;

namespace SecureHR.Web.Client
{
    /// <summary>
    /// An API call failed. <see cref="Exception.Message"/> is safe to show to the user.
    /// </summary>
    public class ApiException(HttpStatusCode statusCode, string message) : Exception(message)
    {
        public HttpStatusCode StatusCode { get; } = statusCode;
    }
}
