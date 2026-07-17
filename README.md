# Swevo.HttpClient.TestKit

[![NuGet](https://img.shields.io/nuget/v/Swevo.HttpClient.TestKit.svg)](https://www.nuget.org/packages/Swevo.HttpClient.TestKit)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Swevo.HttpClient.TestKit.svg)](https://www.nuget.org/packages/Swevo.HttpClient.TestKit)
[![CI](https://github.com/Swevo/HttpClient.TestKit/actions/workflows/build.yml/badge.svg)](https://github.com/Swevo/HttpClient.TestKit/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Unit-test `HttpClient` calls without mocking frameworks. Fluent setup, JSON responses, request recording, and built-in verification. Zero dependencies.

```csharp
var handler = new FakeHttpMessageHandler();

handler.SetupGet("https://api.example.com/users")
    .ReturnsJson(new[] { new User(1, "Alice") });

handler.SetupPost("https://api.example.com/users")
    .ReturnsJson(new User(2, "Bob"), HttpStatusCode.Created);

var client = new HttpClient(handler);

// Act
var users = await client.GetFromJsonAsync<User[]>("https://api.example.com/users");

// Verify
users.Should().ContainSingle(u => u.Name == "Alice");
handler.VerifyGetWasCalled("https://api.example.com/users");
handler.VerifyPostNeverCalled("https://api.example.com/users");
```

## Install

```bash
dotnet add package Swevo.HttpClient.TestKit
```

## Why not Moq?

| | Moq | `Swevo.HttpClient.TestKit` |
|---|---|---|
| Dependencies | Moq + Castle.Core | **Zero** |
| Setup syntax | `mock.Protected().Setup<Task<HttpResponseMessage>>("SendAsync", ...)` | `handler.SetupGet(url).ReturnsJson(...)` |
| JSON responses | Manual | `ReturnsJson<T>(value)` |
| Request recording | Manual | `handler.RecordedRequests` |
| Verification | `mock.Verify(...)` | `handler.VerifyGetWasCalled(url)` |

## Setup methods

```csharp
handler.SetupGet(url)
handler.SetupPost(url)
handler.SetupPut(url)
handler.SetupPatch(url)
handler.SetupDelete(url)
handler.Setup(HttpMethod.Options, url)   // any method
```

## Response builders

```csharp
.ReturnsJson(value)                          // 200 OK + application/json body
.ReturnsJson(value, HttpStatusCode.Created)  // 201 Created + application/json body
.ReturnsString("OK")                         // 200 OK + text/plain body
.ReturnsString("Bad Request", HttpStatusCode.BadRequest)
.ReturnsStatusCode(HttpStatusCode.NoContent) // no body
.Returns(response)                           // fixed HttpResponseMessage
.Returns(req => BuildResponse(req))          // factory — receives the original request
.Throws(new HttpRequestException("timeout")) // throws on request
```

## Request recording

```csharp
// All requests in order
handler.RecordedRequests.Should().HaveCount(3);

// Custom assertions
handler.RecordedRequests
    .Should().ContainSingle(r =>
        r.Method == HttpMethod.Post &&
        r.RequestUri!.PathAndQuery == "/users");
```

## Verification

```csharp
handler.VerifyWasCalled(HttpMethod.Get, url);        // called ≥ 1 time
handler.VerifyNeverCalled(HttpMethod.Delete, url);   // called 0 times
handler.VerifyCalledTimes(HttpMethod.Get, url, 3);   // called exactly 3 times

// Convenience
handler.VerifyGetWasCalled(url);
handler.VerifyPostWasCalled(url);
handler.VerifyPutWasCalled(url);
handler.VerifyPatchWasCalled(url);
handler.VerifyDeleteWasCalled(url);
handler.VerifyGetNeverCalled(url);
handler.VerifyPostNeverCalled(url);
handler.VerifyDeleteNeverCalled(url);
```

Verification methods throw `InvalidOperationException` on failure — compatible with any test framework.

## Unmatched requests

By default, unmatched requests return **404 Not Found**. Override to use strict mode:

```csharp
handler.UnmatchedRequestHandler = req =>
    throw new InvalidOperationException($"Unexpected request: {req.Method} {req.RequestUri}");
```

## URL matching

- Exact string match, case-insensitive
- If the same method+URL is set up twice, **the last setup wins**
- Query strings are part of the URL — include them in the setup if needed

## Testing a typed HttpClient

```csharp
public class UserService(HttpClient client)
{
    public Task<User[]> GetUsersAsync() =>
        client.GetFromJsonAsync<User[]>("/users")!;
}

// Test
var handler = new FakeHttpMessageHandler();
handler.SetupGet("https://api.example.com/users")
    .ReturnsJson(new[] { new User(1, "Alice") });

var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.example.com") };
var sut = new UserService(client);

var users = await sut.GetUsersAsync();
users.Should().HaveCount(1);
```

## Part of the Swevo testing toolkit

| Package | Purpose |
|---|---|
| [Swevo.HttpClient.TestKit](https://github.com/Swevo/HttpClient.TestKit) | This package |
| [Swevo.MassTransit.TestKit](https://github.com/Swevo/MassTransit.RequestClient.TestKit) | Fake MassTransit request clients |
| [AutoLog.Generator](https://github.com/Swevo/AutoLog.Generator) | Compile-time high-performance `LoggerMessage.Define` logging |
| [AutoHttpClient.Generator](https://github.com/Swevo/AutoHttpClient.Generator) | Compile-time typed HTTP client. AOT-safe Refit alternative |

## Related Packages

| Package | Downloads | Description |
|---|---|---|
| [Swevo.MassTransit.TestKit](https://www.nuget.org/packages/Swevo.MassTransit.TestKit) | [![Downloads](https://img.shields.io/nuget/dt/Swevo.MassTransit.TestKit.svg)](https://www.nuget.org/packages/Swevo.MassTransit.TestKit) | Lightweight test doubles for MassTransit request/response and publish patterns |
| [Swevo.AutoTestData](https://www.nuget.org/packages/Swevo.AutoTestData) | [![Downloads](https://img.shields.io/nuget/dt/Swevo.AutoTestData.svg)](https://www.nuget.org/packages/Swevo.AutoTestData) | Compile-time test data builders for  |

---

## License

MIT © 2026 Justin Bannister
