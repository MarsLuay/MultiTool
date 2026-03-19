using System.Net;
using System.Net.Http;
using System.Text;
using MultiTool.Infrastructure.Windows.Updates;
using FluentAssertions;

namespace MultiTool.Infrastructure.Windows.Tests;

public sealed class GitHubAppUpdateServiceTests
{
    [Fact]
    public async Task CheckForUpdatesAsync_ShouldReportAvailableUpdate()
    {
        var service = new GitHubAppUpdateService(
            CreateHttpClient(HttpStatusCode.OK, """{"tag_name":"v1.2.0","html_url":"https://github.com/MarsLuay/MultiTool/releases/tag/v1.2.0"}"""),
            () => "1.0.0");

        var result = await service.CheckForUpdatesAsync();

        result.CheckedSuccessfully.Should().BeTrue();
        result.IsUpdateAvailable.Should().BeTrue();
        result.LatestVersion.Should().Be("1.2.0");
        result.Message.Should().Be("MultiTool 1.2.0 is available on GitHub.");
    }

    [Fact]
    public async Task CheckForUpdatesAsync_ShouldReportUpToDateWhenVersionsMatch()
    {
        var service = new GitHubAppUpdateService(
            CreateHttpClient(HttpStatusCode.OK, """{"tag_name":"v1.0.0","html_url":"https://github.com/MarsLuay/MultiTool/releases/tag/v1.0.0"}"""),
            () => "1.0.0");

        var result = await service.CheckForUpdatesAsync();

        result.CheckedSuccessfully.Should().BeTrue();
        result.IsUpdateAvailable.Should().BeFalse();
        result.Message.Should().Be("MultiTool is up to date (1.0.0).");
    }

    [Fact]
    public async Task CheckForUpdatesAsync_ShouldHandleMissingRelease()
    {
        var service = new GitHubAppUpdateService(
            CreateHttpClient(HttpStatusCode.NotFound, "{}"),
            () => "1.0.0");

        var result = await service.CheckForUpdatesAsync();

        result.CheckedSuccessfully.Should().BeTrue();
        result.IsUpdateAvailable.Should().BeFalse();
        result.Message.Should().Be("No MultiTool release is published on GitHub yet.");
    }

    private static HttpClient CreateHttpClient(HttpStatusCode statusCode, string json)
    {
        var handler = new StubHttpMessageHandler(
            _ => new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });

        return new HttpClient(handler);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(handler(request));
    }
}
