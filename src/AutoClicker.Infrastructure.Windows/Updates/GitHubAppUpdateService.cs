using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoClicker.Core.Models;
using AutoClicker.Core.Services;

namespace AutoClicker.Infrastructure.Windows.Updates;

public sealed class GitHubAppUpdateService : IAppUpdateService
{
    private const string Owner = "MarsLuay";
    private const string Repository = "MultiTool";
    private static readonly Uri LatestReleaseUri = new($"https://api.github.com/repos/{Owner}/{Repository}/releases/latest");
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient httpClient;
    private readonly Func<string> currentVersionResolver;

    public GitHubAppUpdateService()
        : this(CreateHttpClient(), ResolveCurrentVersion)
    {
    }

    public GitHubAppUpdateService(HttpClient httpClient, Func<string> currentVersionResolver)
    {
        this.httpClient = httpClient;
        this.currentVersionResolver = currentVersionResolver;
    }

    public async Task<AppUpdateInfo> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var currentVersion = currentVersionResolver();

        using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseUri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new AppUpdateInfo(
                true,
                false,
                currentVersion,
                null,
                "No MultiTool release is published on GitHub yet.");
        }

        if (!response.IsSuccessStatusCode)
        {
            return new AppUpdateInfo(
                false,
                false,
                currentVersion,
                null,
                $"GitHub update check failed: {(int)response.StatusCode} {response.ReasonPhrase}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        var release = await JsonSerializer.DeserializeAsync<GitHubReleaseResponse>(stream, SerializerOptions, cancellationToken).ConfigureAwait(false);

        if (release is null || string.IsNullOrWhiteSpace(release.TagName))
        {
            return new AppUpdateInfo(
                false,
                false,
                currentVersion,
                null,
                "GitHub update check failed: latest release metadata was empty.");
        }

        if (!TryParseVersionParts(currentVersion, out var currentParts)
            || !TryParseVersionParts(release.TagName, out var latestParts))
        {
            return new AppUpdateInfo(
                false,
                false,
                currentVersion,
                release.TagName,
                $"GitHub update check failed: version '{release.TagName}' could not be compared.",
                release.HtmlUrl);
        }

        var latestVersion = NormalizeDisplayVersion(release.TagName);
        var comparison = CompareVersionParts(currentParts, latestParts);

        if (comparison < 0)
        {
            return new AppUpdateInfo(
                true,
                true,
                currentVersion,
                latestVersion,
                $"MultiTool {latestVersion} is available on GitHub.",
                release.HtmlUrl);
        }

        return new AppUpdateInfo(
            true,
            false,
            currentVersion,
            latestVersion,
            $"MultiTool is up to date ({currentVersion}).",
            release.HtmlUrl);
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("MultiTool/1.0");
        return client;
    }

    private static string ResolveCurrentVersion()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version
            ?? Assembly.GetExecutingAssembly().GetName().Version
            ?? new Version(1, 0, 0);
        return version.Build >= 0 ? version.ToString(3) : version.ToString();
    }

    private static bool TryParseVersionParts(string version, out int[] parts)
    {
        parts = [];
        if (string.IsNullOrWhiteSpace(version))
        {
            return false;
        }

        var trimmed = version.Trim();
        while (trimmed.Length > 0 && !char.IsDigit(trimmed[0]))
        {
            trimmed = trimmed[1..];
        }

        var core = trimmed.Split(['-', '+'], 2, StringSplitOptions.RemoveEmptyEntries)[0];
        var rawParts = core
            .Split('.', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => int.TryParse(part, out var value) ? value : (int?)null)
            .ToArray();

        if (rawParts.Length == 0 || rawParts.Any(part => part is null))
        {
            return false;
        }

        var parsed = rawParts.Select(part => part!.Value).ToList();
        while (parsed.Count > 1 && parsed[^1] == 0)
        {
            parsed.RemoveAt(parsed.Count - 1);
        }

        parts = [.. parsed];
        return true;
    }

    private static string NormalizeDisplayVersion(string version) =>
        NormalizeVersionCore(version) ?? version.Trim();

    private static int CompareVersionParts(IReadOnlyList<int> left, IReadOnlyList<int> right)
    {
        var maxLength = Math.Max(left.Count, right.Count);
        for (var index = 0; index < maxLength; index++)
        {
            var leftValue = index < left.Count ? left[index] : 0;
            var rightValue = index < right.Count ? right[index] : 0;
            var comparison = leftValue.CompareTo(rightValue);
            if (comparison != 0)
            {
                return comparison;
            }
        }

        return 0;
    }

    private static string? NormalizeVersionCore(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return null;
        }

        var trimmed = version.Trim();
        while (trimmed.Length > 0 && !char.IsDigit(trimmed[0]))
        {
            trimmed = trimmed[1..];
        }

        return trimmed.Split(['-', '+'], 2, StringSplitOptions.RemoveEmptyEntries)[0];
    }

    private sealed record GitHubReleaseResponse(
        [property: JsonPropertyName("tag_name")] string TagName,
        [property: JsonPropertyName("html_url")] string HtmlUrl);
}
