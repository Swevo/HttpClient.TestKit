using System.Net;
using System.Text;
using System.Text.Json;

namespace Swevo.HttpClient.TestKit;

/// <summary>
/// Fluent setup for a single method+URL combination on <see cref="FakeHttpMessageHandler"/>.
/// Chain <c>Returns*</c> calls to configure the response; each call to the matched endpoint
/// creates a fresh <see cref="HttpResponseMessage"/> so multiple requests never share a stream.
/// </summary>
public sealed class FakeRequestSetup
{
    internal HttpMethod Method { get; }
    internal string Url { get; }
    internal Func<HttpRequestMessage, HttpResponseMessage>? Factory { get; private set; }

    internal FakeRequestSetup(HttpMethod method, string url)
    {
        Method = method;
        Url    = url;
    }

    /// <summary>Returns the <see cref="HttpResponseMessage"/> produced by <paramref name="factory"/> for each request.</summary>
    public FakeRequestSetup Returns(Func<HttpRequestMessage, HttpResponseMessage> factory)
    {
        Factory = factory;
        return this;
    }

    /// <summary>Returns a fixed <see cref="HttpResponseMessage"/>. Note: the same instance is reused across calls.</summary>
    public FakeRequestSetup Returns(HttpResponseMessage response)
        => Returns(_ => response);

    /// <summary>Returns a JSON-serialised <paramref name="value"/> with <c>application/json</c> content type.</summary>
    public FakeRequestSetup ReturnsJson<T>(T value, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var json = JsonSerializer.Serialize(value);
        Factory = _ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        return this;
    }

    /// <summary>Returns a plain-text response body.</summary>
    public FakeRequestSetup ReturnsString(string content, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        Factory = _ => new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, "text/plain")
        };
        return this;
    }

    /// <summary>Returns a response with no body.</summary>
    public FakeRequestSetup ReturnsStatusCode(HttpStatusCode statusCode)
    {
        Factory = _ => new HttpResponseMessage(statusCode);
        return this;
    }

    /// <summary>Throws <paramref name="exception"/> when the matched request is made.</summary>
    public FakeRequestSetup Throws(Exception exception)
    {
        Factory = _ => throw exception;
        return this;
    }
}
