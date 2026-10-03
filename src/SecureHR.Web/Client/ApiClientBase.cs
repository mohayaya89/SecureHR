using SecureHR.Web.Services;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SecureHR.Web.Client
{
    /// <summary>
    /// Base for typed API clients. Attaches the current circuit's bearer token to each request
    /// and turns error responses into <see cref="ApiException"/> with a user-facing message.
    /// Typed clients are resolved from the circuit's DI scope, so <see cref="AuthTokenProvider"/>
    /// here is the signed-in user's own token store. (A DelegatingHandler would not work:
    /// IHttpClientFactory creates handlers in a separate scope.)
    /// </summary>
    public abstract class ApiClientBase(HttpClient httpClient, AuthTokenProvider tokenProvider)
    {
        protected async Task<T?> GetAsync<T>(string url)
        {
            using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
            await EnsureSuccessAsync(response);
            return await response.Content.ReadFromJsonAsync<T>();
        }

        protected async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest body)
        {
            using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(body)
            });
            await EnsureSuccessAsync(response);
            return await response.Content.ReadFromJsonAsync<TResponse>();
        }

        protected async Task PutAsync<TRequest>(string url, TRequest body)
        {
            using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Put, url)
            {
                Content = JsonContent.Create(body)
            });
            await EnsureSuccessAsync(response);
        }

        protected async Task DeleteAsync(string url)
        {
            using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Delete, url));
            await EnsureSuccessAsync(response);
        }

        private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            if (tokenProvider.HasToken)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenProvider.CurrentToken);

            try
            {
                return await httpClient.SendAsync(request);
            }
            catch (HttpRequestException)
            {
                throw new ApiException(HttpStatusCode.ServiceUnavailable, "Could not reach the server. Please try again.");
            }
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
                return;

            var message = response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => "Your session has expired. Please sign in again.",
                HttpStatusCode.Forbidden => "You don't have permission to do that.",
                HttpStatusCode.TooManyRequests => "Too many requests. Please wait a moment and try again.",
                >= HttpStatusCode.InternalServerError => "Something went wrong on the server. Please try again.",
                _ => await ReadProblemMessageAsync(response) ?? "The request could not be completed."
            };

            throw new ApiException(response.StatusCode, message);
        }

        // Reads an RFC 7807 ProblemDetails body: validation errors first, then "detail", then "title".
        private static async Task<string?> ReadProblemMessageAsync(HttpResponseMessage response)
        {
            try
            {
                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var root = doc.RootElement;

                if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    var messages = errors.EnumerateObject()
                        .SelectMany(field => field.Value.EnumerateArray().Select(m => m.GetString()))
                        .Where(m => !string.IsNullOrWhiteSpace(m));
                    var joined = string.Join(" ", messages);
                    if (joined.Length > 0)
                        return joined;
                }

                foreach (var name in new[] { "detail", "title" })
                {
                    if (root.TryGetProperty(name, out var value) && value.GetString() is { Length: > 0 } text)
                        return text;
                }
            }
            catch (JsonException)
            {
                // Not a ProblemDetails body
            }

            return null;
        }
    }
}
