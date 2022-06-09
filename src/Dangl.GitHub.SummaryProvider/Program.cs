// See https://aka.ms/new-console-template for more information

using CommandLine;
using CommandLine.Text;

HeadingInfo.Default.WriteMessage("Visit https://www.dangl-it.com to find out more about this exporter");
HeadingInfo.Default.WriteMessage("This generator is available on GitHub: https://github.com/Dangl-IT/Dangl.GitHub.SummaryProvider");
HeadingInfo.Default.WriteMessage($"Version {VersionInfo.Version}");
await Parser.Default.ParseArguments<ExportOptions>(args)
    .MapResult(async options =>
    {
        try
        {
            var repoInspector = new GitHubRepoInspector(options.GitHubOrganization!,
                options.GitHubRepository!,
                options.DevelopBranch!,
                options.GitHubPersonalAccessToken!);
            Console.WriteLine("Inspecting GitHub repository, getting all commits...");
            var commits = await repoInspector.GetCommitsForDevelopBranchAsync();
            Console.WriteLine("Getting all pull requests...");
            var pullRequests = await repoInspector.GetPullRequestDataAsync();

            var allActions = commits
                .Where(c => !pullRequests.Any(pr => pr.CommitObjectIds.Contains(c.ObjectId))
                    && !(c.Message?.StartsWith("Merge pull request #") ?? false))
                .Select(c => new RepoAction { Date = c.AuthoredDate, Content = c })
                .Concat(pullRequests.Select(p => new RepoAction { Date = p.MergedAt, Content = p }));

            var outputFilePath = Path.Combine(options.ExportBaseFolder!, $"{options.DocumentExportYear:0000}-{options.DocumentExportMonth:00} GitHubExport.txt");
            var summaryExporter = new SummaryExporter(allActions, outputFilePath, options.DocumentExportYear, options.DocumentExportMonth);
            Console.WriteLine($"Writing output to: {outputFilePath}");
            await summaryExporter.ExportSummaryAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    },
    errors =>
    {
        Console.WriteLine("Could not parse CLI arguments");
        return Task.CompletedTask;
    }).ConfigureAwait(false);


