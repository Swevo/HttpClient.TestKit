using System.Net;

namespace Swevo.HttpClient.TestKit;

/// <summary>
/// A fake <see cref="HttpMessageHandler"/> for unit-testing code that uses <see cref="System.Net.Http.HttpClient"/>.
/// Configure responses with <c>Setup*()</c> methods, then verify interactions via
/// <c>Verify*()</c> or inspect <see cref="RecordedRequests"/> directly.
/// </summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly List<FakeRequestSetup> _setups = [];
    private readonly List<HttpRequestMessage> _recorded = [];

    /// <summary>
    /// All requests dispatched through this handler in order of arrival.
    /// Inspect for custom assertions beyond what <c>Verify*()</c> methods provide.
    /// </summary>
    public IReadOnlyList<HttpRequestMessage> RecordedRequests => _recorded.AsReadOnly();

    /// <summary>
    /// Response returned when no setup matches the incoming request.
    /// Defaults to <see cref="HttpStatusCode.NotFound"/> (404).
    /// Replace with a factory that throws to use strict mode.
    /// </summary>
    public Func<HttpRequestMessage, HttpResponseMessage> UnmatchedRequestHandler { get; set; }
        = _ => new HttpResponseMessage(HttpStatusCode.NotFound);

    // ── Setup ─────────────────────────────────────────────────────────────────

    public FakeRequestSetup Setup(HttpMethod method, string url)
    {
        var setup = new FakeRequestSetup(method, url);
        _setups.Add(setup);
        return setup;
    }

    public FakeRequestSetup SetupGet(string url)    => Setup(HttpMethod.Get,    url);
    public FakeRequestSetup SetupPost(string url)   => Setup(HttpMethod.Post,   url);
    public FakeRequestSetup SetupPut(string url)    => Setup(HttpMethod.Put,    url);
    public FakeRequestSetup SetupPatch(string url)  => Setup(HttpMethod.Patch,  url);
    public FakeRequestSetup SetupDelete(string url) => Setup(HttpMethod.Delete, url);

    // ── Dispatch ──────────────────────────────────────────────────────────────

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _recorded.Add(request);

        // Last matching setup wins (mirrors Moq behaviour for overrides)
        var setup = _setups.LastOrDefault(s =>
            s.Method == request.Method &&
            string.Equals(s.Url, request.RequestUri?.ToString(), StringComparison.OrdinalIgnoreCase));

        if (setup?.Factory is null)
            return Task.FromResult(UnmatchedRequestHandler(request));

        return Task.FromResult(setup.Factory(request));
    }

    // ── Verification ──────────────────────────────────────────────────────────

    /// <summary>Asserts that the given method+URL combination was called at least once.</summary>
    public void VerifyWasCalled(HttpMethod method, string url)
    {
        var count = CountMatching(method, url);
        if (count == 0)
            throw new InvalidOperationException(
                $"Expected {method} {url} to have been called at least once, but it was never called.");
    }

    /// <summary>Asserts that the given method+URL combination was never called.</summary>
    public void VerifyNeverCalled(HttpMethod method, string url)
    {
        var count = CountMatching(method, url);
        if (count > 0)
            throw new InvalidOperationException(
                $"Expected {method} {url} to never have been called, but it was called {count} time(s).");
    }

    /// <summary>Asserts that the given method+URL combination was called exactly <paramref name="expectedTimes"/> times.</summary>
    public void VerifyCalledTimes(HttpMethod method, string url, int expectedTimes)
    {
        var count = CountMatching(method, url);
        if (count != expectedTimes)
            throw new InvalidOperationException(
                $"Expected {method} {url} to have been called {expectedTimes} time(s), but it was called {count} time(s).");
    }

    // ── Convenience wrappers ──────────────────────────────────────────────────

    public void VerifyGetWasCalled(string url)    => VerifyWasCalled(HttpMethod.Get,    url);
    public void VerifyPostWasCalled(string url)   => VerifyWasCalled(HttpMethod.Post,   url);
    public void VerifyPutWasCalled(string url)    => VerifyWasCalled(HttpMethod.Put,    url);
    public void VerifyPatchWasCalled(string url)  => VerifyWasCalled(HttpMethod.Patch,  url);
    public void VerifyDeleteWasCalled(string url) => VerifyWasCalled(HttpMethod.Delete, url);

    public void VerifyGetNeverCalled(string url)    => VerifyNeverCalled(HttpMethod.Get,    url);
    public void VerifyPostNeverCalled(string url)   => VerifyNeverCalled(HttpMethod.Post,   url);
    public void VerifyDeleteNeverCalled(string url) => VerifyNeverCalled(HttpMethod.Delete, url);

    // ── Private ───────────────────────────────────────────────────────────────

    private int CountMatching(HttpMethod method, string url) =>
        _recorded.Count(r =>
            r.Method == method &&
            string.Equals(r.RequestUri?.ToString(), url, StringComparison.OrdinalIgnoreCase));
}
