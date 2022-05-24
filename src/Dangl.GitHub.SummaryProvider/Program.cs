// See https://aka.ms/new-console-template for more information
var repoInspector = new GitHubRepoInspector("organization",
    "repository",
    "branch",
    "gitHubToken");
var commits = await repoInspector.GetCommitsForDevelopBranchAsync();
var pullRequests = await repoInspector.GetPullRequestDataAsync();

var allActions = commits.Select(c => new RepoAction { Date = c.AuthoredDate, Content = c})
    .Concat(pullRequests.Select(p => new RepoAction { Date = p.MergedAt, Content = p }));

var summaryExporter = new SummaryExporter(allActions, "outputFilePath", 2022, 5);
await summaryExporter.ExportSummaryAsync();
