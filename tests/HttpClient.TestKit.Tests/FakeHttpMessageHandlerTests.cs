using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Swevo.HttpClient.TestKit;
using Xunit;

namespace HttpClient.TestKit.Tests;

file record User(int Id, string Name);

public class SetupAndResponseTests
{
    [Fact]
    public async Task SetupGet_ReturnsJson()
    {
        var handler = new FakeHttpMessageHandler();
        handler.SetupGet("https://api.example.com/users")
            .ReturnsJson(new[] { new User(1, "Alice") });

        var client = new System.Net.Http.HttpClient(handler);
        var users = await client.GetFromJsonAsync<User[]>("https://api.example.com/users");

        users.Should().ContainSingle(u => u.Name == "Alice");
    }

    [Fact]
    public async Task SetupPost_ReturnsJson_WithCreatedStatus()
    {
        var handler = new FakeHttpMessageHandler();
        handler.SetupPost("https://api.example.com/users")
            .ReturnsJson(new User(2, "Bob"), HttpStatusCode.Created);

        var client = new System.Net.Http.HttpClient(handler);
        var response = await client.PostAsJsonAsync("https://api.example.com/users", new User(0, "Bob"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var user = await response.Content.ReadFromJsonAsync<User>();
        user!.Name.Should().Be("Bob");
    }

    [Fact]
    public async Task SetupDelete_ReturnsNoContent()
    {
        var handler = new FakeHttpMessageHandler();
        handler.SetupDelete("https://api.example.com/users/1")
            .ReturnsStatusCode(HttpStatusCode.NoContent);

        var client = new System.Net.Http.HttpClient(handler);
        var response = await client.DeleteAsync("https://api.example.com/users/1");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task SetupPut_ReturnsJson()
    {
        var handler = new FakeHttpMessageHandler();
        handler.SetupPut("https://api.example.com/users/1")
            .ReturnsJson(new User(1, "Updated"));

        var client = new System.Net.Http.HttpClient(handler);
        var response = await client.PutAsJsonAsync("https://api.example.com/users/1", new User(1, "Updated"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SetupPatch_ReturnsOk()
    {
        var handler = new FakeHttpMessageHandler();
        handler.SetupPatch("https://api.example.com/users/1")
            .ReturnsStatusCode(HttpStatusCode.OK);

        var client = new System.Net.Http.HttpClient(handler);
        var response = await client.PatchAsJsonAsync("https://api.example.com/users/1", new { Name = "X" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReturnsString_BodyIsCorrect()
    {
        var handler = new FakeHttpMessageHandler();
        handler.SetupGet("https://api.example.com/health")
            .ReturnsString("OK");

        var client = new System.Net.Http.HttpClient(handler);
        var body = await client.GetStringAsync("https://api.example.com/health");

        body.Should().Be("OK");
    }

    [Fact]
    public async Task ReturnsStatusCode_NoBody()
    {
        var handler = new FakeHttpMessageHandler();
        handler.SetupDelete("https://api.example.com/cache")
            .ReturnsStatusCode(HttpStatusCode.NoContent);

        var client = new System.Net.Http.HttpClient(handler);
        var response = await client.DeleteAsync("https://api.example.com/cache");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().BeEmpty();
    }

    [Fact]
    public async Task Returns_CustomFactory_ReceivesRequest()
    {
        var handler = new FakeHttpMessageHandler();
        handler.SetupGet("https://api.example.com/echo?name=test")
            .Returns(req => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(req.RequestUri!.Query)
            });

        var client = new System.Net.Http.HttpClient(handler);
        var response = await client.GetAsync("https://api.example.com/echo?name=test");
        var body = await response.Content.ReadAsStringAsync();

        body.Should().Be("?name=test");
    }

    [Fact]
    public async Task UnmatchedRequest_Returns404ByDefault()
    {
        var handler = new FakeHttpMessageHandler();
        var client = new System.Net.Http.HttpClient(handler);

        var response = await client.GetAsync("https://api.example.com/not-setup");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnmatchedRequestHandler_CanBeOverridden()
    {
        var handler = new FakeHttpMessageHandler();
        handler.UnmatchedRequestHandler = _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        var client = new System.Net.Http.HttpClient(handler);

        var response = await client.GetAsync("https://api.example.com/anything");

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task UrlMatching_IsCaseInsensitive()
    {
        var handler = new FakeHttpMessageHandler();
        handler.SetupGet("https://api.example.com/Users")
            .ReturnsStatusCode(HttpStatusCode.OK);

        var client = new System.Net.Http.HttpClient(handler);
        var response = await client.GetAsync("https://api.example.com/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DifferentMethods_SameUrl_ResolveIndependently()
    {
        var handler = new FakeHttpMessageHandler();
        handler.SetupGet("https://api.example.com/items").ReturnsStatusCode(HttpStatusCode.OK);
        handler.SetupPost("https://api.example.com/items").ReturnsStatusCode(HttpStatusCode.Created);

        var client = new System.Net.Http.HttpClient(handler);
        var getResp  = await client.GetAsync("https://api.example.com/items");
        var postResp = await client.PostAsJsonAsync("https://api.example.com/items", new { });

        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        postResp.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task LastSetupWins_WhenSameUrlRegisteredTwice()
    {
        var handler = new FakeHttpMessageHandler();
        handler.SetupGet("https://api.example.com/users").ReturnsStatusCode(HttpStatusCode.OK);
        handler.SetupGet("https://api.example.com/users").ReturnsStatusCode(HttpStatusCode.Accepted);

        var client = new System.Net.Http.HttpClient(handler);
        var response = await client.GetAsync("https://api.example.com/users");

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task Throws_PropagatesExceptionOnRequest()
    {
        var handler = new FakeHttpMessageHandler();
        handler.SetupGet("https://api.example.com/boom")
            .Throws(new HttpRequestException("Network error"));

        var client = new System.Net.Http.HttpClient(handler);
        var act = () => client.GetAsync("https://api.example.com/boom");

        await act.Should().ThrowAsync<HttpRequestException>().WithMessage("Network error");
    }

    [Fact]
    public async Task MultipleRequests_SameSetup_EachGetsNewResponse()
    {
        var handler = new FakeHttpMessageHandler();
        handler.SetupGet("https://api.example.com/ping").ReturnsString("pong");

        var client = new System.Net.Http.HttpClient(handler);
        var r1 = await (await client.GetAsync("https://api.example.com/ping")).Content.ReadAsStringAsync();
        var r2 = await (await client.GetAsync("https://api.example.com/ping")).Content.ReadAsStringAsync();

        r1.Should().Be("pong");
        r2.Should().Be("pong");
    }
}

public class RecordedRequestsTests
{
    [Fact]
    public void RecordedRequests_IsEmptyInitially()
    {
        var handler = new FakeHttpMessageHandler();
        handler.RecordedRequests.Should().BeEmpty();
    }

    [Fact]
    public async Task RecordedRequests_TracksGetRequest()
    {
        var handler = new FakeHttpMessageHandler();
        var client = new System.Net.Http.HttpClient(handler);

        await client.GetAsync("https://api.example.com/users");

        handler.RecordedRequests.Should().ContainSingle(r =>
            r.Method == HttpMethod.Get &&
            r.RequestUri!.ToString() == "https://api.example.com/users");
    }

    [Fact]
    public async Task RecordedRequests_TracksDifferentMethods()
    {
        var handler = new FakeHttpMessageHandler();
        var client = new System.Net.Http.HttpClient(handler);

        await client.GetAsync("https://api.example.com/a");
        await client.PostAsJsonAsync("https://api.example.com/b", new { });
        await client.DeleteAsync("https://api.example.com/c");

        handler.RecordedRequests.Should().HaveCount(3);
        handler.RecordedRequests[0].Method.Should().Be(HttpMethod.Get);
        handler.RecordedRequests[1].Method.Should().Be(HttpMethod.Post);
        handler.RecordedRequests[2].Method.Should().Be(HttpMethod.Delete);
    }
}

public class VerificationTests
{
    [Fact]
    public async Task VerifyWasCalled_Passes_WhenCalled()
    {
        var handler = new FakeHttpMessageHandler();
        var client = new System.Net.Http.HttpClient(handler);
        await client.GetAsync("https://api.example.com/users");

        var act = () => handler.VerifyWasCalled(HttpMethod.Get, "https://api.example.com/users");
        act.Should().NotThrow();
    }

    [Fact]
    public void VerifyWasCalled_Throws_WhenNotCalled()
    {
        var handler = new FakeHttpMessageHandler();

        var act = () => handler.VerifyWasCalled(HttpMethod.Get, "https://api.example.com/users");
        act.Should().Throw<InvalidOperationException>().WithMessage("*never called*");
    }

    [Fact]
    public async Task VerifyNeverCalled_Passes_WhenNotCalled()
    {
        var handler = new FakeHttpMessageHandler();
        var client = new System.Net.Http.HttpClient(handler);
        await client.GetAsync("https://api.example.com/users");

        var act = () => handler.VerifyNeverCalled(HttpMethod.Delete, "https://api.example.com/users");
        act.Should().NotThrow();
    }

    [Fact]
    public async Task VerifyNeverCalled_Throws_WhenWasCalled()
    {
        var handler = new FakeHttpMessageHandler();
        var client = new System.Net.Http.HttpClient(handler);
        await client.DeleteAsync("https://api.example.com/users/1");

        var act = () => handler.VerifyNeverCalled(HttpMethod.Delete, "https://api.example.com/users/1");
        act.Should().Throw<InvalidOperationException>().WithMessage("*1 time(s)*");
    }

    [Fact]
    public async Task VerifyCalledTimes_Passes_WhenCountMatches()
    {
        var handler = new FakeHttpMessageHandler();
        var client = new System.Net.Http.HttpClient(handler);
        await client.GetAsync("https://api.example.com/users");
        await client.GetAsync("https://api.example.com/users");
        await client.GetAsync("https://api.example.com/users");

        var act = () => handler.VerifyCalledTimes(HttpMethod.Get, "https://api.example.com/users", 3);
        act.Should().NotThrow();
    }

    [Fact]
    public async Task VerifyCalledTimes_Throws_WhenCountMismatch()
    {
        var handler = new FakeHttpMessageHandler();
        var client = new System.Net.Http.HttpClient(handler);
        await client.GetAsync("https://api.example.com/users");

        var act = () => handler.VerifyCalledTimes(HttpMethod.Get, "https://api.example.com/users", 3);
        act.Should().Throw<InvalidOperationException>().WithMessage("*called 1 time(s)*");
    }

    [Fact]
    public async Task VerifyGetWasCalled_Convenience()
    {
        var handler = new FakeHttpMessageHandler();
        var client = new System.Net.Http.HttpClient(handler);
        await client.GetAsync("https://api.example.com/ping");

        var act = () => handler.VerifyGetWasCalled("https://api.example.com/ping");
        act.Should().NotThrow();
    }

    [Fact]
    public async Task VerifyPostWasCalled_Convenience()
    {
        var handler = new FakeHttpMessageHandler();
        var client = new System.Net.Http.HttpClient(handler);
        await client.PostAsJsonAsync("https://api.example.com/users", new { });

        var act = () => handler.VerifyPostWasCalled("https://api.example.com/users");
        act.Should().NotThrow();
    }

    [Fact]
    public async Task VerifyDeleteNeverCalled_Convenience()
    {
        var handler = new FakeHttpMessageHandler();
        var client = new System.Net.Http.HttpClient(handler);
        await client.GetAsync("https://api.example.com/users");

        var act = () => handler.VerifyDeleteNeverCalled("https://api.example.com/users");
        act.Should().NotThrow();
    }
}
