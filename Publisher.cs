using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Mastonet;
using Mastonet.Entities;
using Octokit;

namespace MastodonGitHubBot;

internal class Publisher
{
    private readonly Log _log;
    private readonly Server _server;
    private readonly MastodonClient _mastodon;
    private readonly GitHubClient _github;

    private readonly ApiOptions _firstPageApiOptions;
    private readonly RepositoryIssueRequest _latestIssuesConfiguration;

    private readonly int _sleepSeconds;
    private readonly bool _debug;

    public static async Task<Publisher> CreateAsync(Log log, HttpClient sharedHttpClient, Settings settings, Server server)
    {
        var mastodon = await Mastodon.GetClientAsync(log, server, sharedHttpClient);
        var github = await GitHub.GetClientAsync(log, server);
        return new Publisher(log, settings, server, mastodon, github);
    }

    internal Publisher(Log log, Settings settings, Server server, MastodonClient mastodon, GitHubClient github)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _server = server;
        _mastodon = mastodon ?? throw new ArgumentNullException(nameof(mastodon));
        _github = github ?? throw new ArgumentNullException(nameof(github));

        _firstPageApiOptions = new() { PageCount = 1, PageSize = 250, StartPage = 1 };
        _latestIssuesConfiguration = new() { SortDirection = SortDirection.Descending, State = ItemStateFilter.Open, SortProperty = IssueSort.Created };

        _sleepSeconds = settings.SleepSeconds;
        _debug = settings.Debug;
    }

    private void PrintStatus()
    {
        // Prior to first API call, this will be null, because it only deals with the last call.
        ApiInfo? apiInfo = _github.GetLastApiInfo();
        RateLimit? rateLimit = apiInfo?.RateLimit;

        _log.WriteInfo($"How many requests per hour?: {rateLimit?.Limit}{Environment.NewLine}" +
                       $"How many requests left?: {rateLimit?.Remaining}{Environment.NewLine}" +
                       $"When does the limit reset?: {rateLimit?.Reset}{Environment.NewLine}");
        Console.WriteLine("----------");

    }

    public async Task PublishAsync()
    {
        IOrderedEnumerable<Issue> issues = await GetUnpublishedIssuesAsync();
        _log.WriteInfo($"Found {issues.Count()} unpublished issues.");
        foreach (Issue issue in issues)
        {
            string text = $"{issue.Title} ({issue.Number}) {issue.HtmlUrl}";

            _log.WriteSuccess($"{issue.CreatedAt:yy/MM/dd HH:mm:ss} - {text}");
            if (!_debug)
            {
                Status status = await _mastodon.PublishStatus(text, _server.Visibility);
                _log.WriteSuccess($"Toot published to Mastodon. ID: {status.Url}");
                Thread.Sleep(1000);
            }
            _server.GitHubLatestIssueNumber = issue.Number;
        }
        PrintStatus();

        _log.WriteInfo($"Sleeping for {_sleepSeconds} seconds...");
        Thread.Sleep(TimeSpan.FromSeconds(_sleepSeconds));
    }

    // Includes PRs
    private Issue GetLastPublishedIssue(IEnumerable<Issue> latestIssues)
    {
        Issue? lastPublishedIssue = null;
        if (_server.GitHubLatestIssueNumber != 0)
        {
            for (int number = _server.GitHubLatestIssueNumber; number < (_server.GitHubLatestIssueNumber + 10); number++)
            {
                lastPublishedIssue = latestIssues.SingleOrDefault(i => i.Number == number);
                if (lastPublishedIssue != null)
                {
                    _log.WriteInfo($"Found the specified latest issue: {number}");
                    break;
                }

                // Can be null if it's too old and latestIssues does not contain it
                _log.WriteError($"Unable to find the specified latest issue: {number}. Trying with {number + 1} now...");
            }
        }

        if (lastPublishedIssue == null)
        {
            _log.WriteError($"Unable to find the the specified latest issue or the next 10 after that: {_server.GitHubLatestIssueNumber}");
            lastPublishedIssue = latestIssues.Any() ? latestIssues.First() : throw new NullReferenceException("No issues found.");
            _log.WriteWarning($"Assigning the latest found issue as the latest one: {lastPublishedIssue.Number}");
            _server.GitHubLatestIssueNumber = lastPublishedIssue.Number;
        }

        return lastPublishedIssue;
    }

    // Includes PRs
    private async Task<IOrderedEnumerable<Issue>> GetUnpublishedIssuesAsync()
    {
        IReadOnlyList<Issue> latestIssues = await _github.Issue.GetAllForRepository(_server.GitHubRepoOrg, _server.GitHubRepoName, _latestIssuesConfiguration, _firstPageApiOptions);

        Issue? lastPublishedIssue = GetLastPublishedIssue(latestIssues);

        return latestIssues
            .Where(i => i.Number > lastPublishedIssue.Number)
            .OrderBy(i => i.Number); // Re-sorted ascending so they get published in order of creation
    }
}
