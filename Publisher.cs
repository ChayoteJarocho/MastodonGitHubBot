using Mastonet;
using Mastonet.Entities;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MastodonGitHubBot;

internal class Publisher
{
    private readonly Settings _settings;
    private readonly MastodonClient _mastodon;
    private readonly GitHubClient _github;

    private readonly ApiOptions _firstPageApiOptions;
    private readonly RepositoryIssueRequest _latestIssuesConfiguration;

    private readonly TimeSpan _sleepSecondsSpan;

    public Publisher(Settings settings, MastodonClient mastodon, GitHubClient github)
    {
        _settings = settings;
        _mastodon = mastodon;
        _github = github;

        _firstPageApiOptions = new() { PageCount = 1, PageSize = 25, StartPage = 1 };
        _latestIssuesConfiguration = new() { SortDirection = SortDirection.Descending, State = ItemStateFilter.Open, SortProperty = IssueSort.Created };

        _sleepSecondsSpan = TimeSpan.FromSeconds(_settings.SleepSeconds);
    }

    public async Task StartAsync()
    {
        // Prior to first API call, this will be null, because it only deals with the last call.
        ApiInfo? apiInfo = _github.GetLastApiInfo();
        RateLimit? rateLimit = apiInfo?.RateLimit;
        int? howManyRequestsCanIMakePerHour = rateLimit?.Limit;
        int? howManyRequestsDoIHaveLeft = rateLimit?.Remaining;
        DateTimeOffset? whenDoesTheLimitReset = rateLimit?.Reset; // UTC time

        Console.WriteLine($"How many requests per hour?: {howManyRequestsCanIMakePerHour}");
        Console.WriteLine($"How many requests left?: {howManyRequestsDoIHaveLeft}");
        Console.WriteLine($"When does the limit reset?: {whenDoesTheLimitReset}");

        Console.WriteLine($"Starting loop...");
        while (true)
        {
            IOrderedEnumerable<Issue> issues = await GetUnpublishedIssuesAsync();
            Console.WriteLine($"Found {issues.Count()} unpublished issues.");
            foreach (Issue issue in issues)
            {
                await PublishAsync(issue);
                _settings.GitHubLatestIssueNumber = issue.Number;
                _settings.Flush(); // Save latest issue number in json file
            }

            Console.WriteLine($"Sleeping for {_settings.SleepSeconds} seconds...");
            Thread.Sleep(_sleepSecondsSpan);
        }
    }

    // Includes PRs
    private Issue GetLastPublishedIssue(IEnumerable<Issue> latestIssues)
    {
        Issue? lastPublishedIssue = null;
        if (_settings.GitHubLatestIssueNumber != 0)
        {
            // Can be null if it's too old and latestIssues does not contain it
            lastPublishedIssue = latestIssues.SingleOrDefault(i => i.Number == _settings.GitHubLatestIssueNumber);
        }

        if (lastPublishedIssue == null)
        {
            lastPublishedIssue = latestIssues.Any() ? latestIssues.First() : throw new NullReferenceException("No issues found."); ;
            _settings.GitHubLatestIssueNumber = lastPublishedIssue.Number;
            _settings.Flush();
        }

        return lastPublishedIssue;
    }

    // Includes PRs
    private async Task<IOrderedEnumerable<Issue>> GetUnpublishedIssuesAsync()
    {
        IReadOnlyList<Issue> latestIssues = await _github.Issue.GetAllForRepository(_settings.GitHubRepoOrg, _settings.GitHubRepoName, _latestIssuesConfiguration, _firstPageApiOptions);

        Issue? lastPublishedIssue = GetLastPublishedIssue(latestIssues);

        return latestIssues
            .Where(i => i.CreatedAt > lastPublishedIssue.CreatedAt && i.Number > lastPublishedIssue.Number)
            .OrderBy(i => i.Number); // Re-sorted ascending so they get published in order of creation
    }

    private async Task PublishAsync(Issue issue)
    {
        string text = $"{issue.Title} ({issue.Number}) {issue.HtmlUrl}";

        Console.WriteLine($"{issue.CreatedAt:yy/MM/dd HH:mm:ss} - {text}");
        if (!_settings.Debug)
        {
            Status status = await _mastodon.PublishStatus(text, Visibility.Unlisted);
            Console.WriteLine($"    Status published to Mastodon. ID: {status.Url}");
            Thread.Sleep(1000);
        }
    }
}
