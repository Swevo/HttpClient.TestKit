# Changelog

## [1.0.0] - 2026-06-26

### Added
- `FakeHttpMessageHandler` extending `HttpMessageHandler`
- `SetupGet/Post/Put/Patch/Delete(url)` + generic `Setup(method, url)` fluent entry points
- `FakeRequestSetup` with `ReturnsJson<T>`, `ReturnsString`, `ReturnsStatusCode`, `Returns(response)`, `Returns(factory)`, `Throws(exception)`
- `RecordedRequests` — ordered list of all dispatched `HttpRequestMessage` instances
- `VerifyWasCalled`, `VerifyNeverCalled`, `VerifyCalledTimes` + per-method convenience overloads
- `UnmatchedRequestHandler` — configurable fallback (default: 404 Not Found)
- Last-setup-wins semantics for duplicate method+URL registrations
- URL matching is exact and case-insensitive
- Zero external dependencies
