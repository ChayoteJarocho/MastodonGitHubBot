using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Octokit;
using X.Bluesky;

namespace BlueskyGitHubBot;

internal class Publisher
{
    private readonly Log _log;
    private readonly Server _server;
    private readonly IBlueskyClient _bluesky;
    private readonly GitHubClient _github;

    private readonly ApiOptions _firstPageApiOptions;
    private readonly RepositoryIssueRequest _latestIssuesConfiguration;

    private readonly bool _debug;
    private readonly int _rateLimitingSeconds;

    public static async Task<Publisher> CreateAsync(Log log, HttpClient sharedHttpClient, Settings settings, Server server)
    {
        string password = await server.GetBlueskyPasswordAsync();
        var bluesky = new BlueskyClient(server.BlueskyUserName, password);
        var github = await GitHub.GetClientAsync(log, server);
        return new Publisher(log, settings, server, bluesky, github);
    }

    internal Publisher(Log log, Settings settings, Server server, BlueskyClient bluesky, GitHubClient github)
    {
        _log = log ?? throw new ArgumentNullException(nameof(log));
        _server = server;
        _bluesky = bluesky ?? throw new ArgumentNullException(nameof(bluesky));
        _github = github ?? throw new ArgumentNullException(nameof(github));

        _firstPageApiOptions = new() { PageCount = 1, PageSize = 250, StartPage = 1 };
        _latestIssuesConfiguration = new() { SortDirection = SortDirection.Descending, State = ItemStateFilter.Open, SortProperty = IssueSort.Created };

        _debug = settings.Debug;
        _rateLimitingSeconds = settings.RateLimitingSeconds;
    }

    private void PrintStatus()
    {
        // Prior to first API call, this will be null, because it only deals with the last call.
        ApiInfo? apiInfo = _github.GetLastApiInfo();
        RateLimit? rateLimit = apiInfo?.RateLimit;

        _log.WriteInfo($"How many requests per hour?: {rateLimit?.Limit}{Environment.NewLine}" +
                       $"How many requests left?: {rateLimit?.Remaining}{Environment.NewLine}" +
                       $"When does the limit reset?: {rateLimit?.Reset}{Environment.NewLine}");
        _log.WriteInfo("----------");

    }

    public async Task PublishAsync()
    {
        List<Issue> issues = [.. (await GetUnpublishedIssuesAsync())];
        _log.WriteInfo($"Found {issues.Count()} unpublished issues.");
        _log.WriteInfo("-----");
        for (int i = 0; i < issues.Count(); i++)
        {
            Issue issue = issues[i];
            string text = $"{issue.Title} ({issue.Number}) {issue.HtmlUrl}";

            _log.WriteInfo($"{issue.CreatedAt:yy/MM/dd HH:mm:ss} - {text}");
            if (!_debug)
            {
                _log.WriteInfo($"Attempting to publish to Bluesky...");
                try
                {
                    await _bluesky.Post(text);
                    _log.WriteSuccess($"Skeet published to Bluesky.");
                }
                catch (Exception exception)
                {
                    _log.WriteError("Bluesky rate limited reached.");
                    _log.WriteError(exception.Message);
                    if (exception.StackTrace != null)
                    {
                        _log.WriteError(exception.StackTrace);
                    }
                    i--;
                    
                    _log.WriteInfo($"Waiting {_rateLimitingSeconds} seconds before publishing another skeet...");
                    Thread.Sleep(TimeSpan.FromSeconds(_rateLimitingSeconds));
                }
            }
            else
            {
                _log.WriteSuccess("Debug is enabled. Pretend the skeet was published successfully.");
            }
            
            _log.WriteInfo("-----");

            _server.GitHubLatestIssueNumber = issue.Number;
        }
        PrintStatus();
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
