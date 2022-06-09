public class SummaryExporter
{
    private readonly IEnumerable<RepoAction> _repoActions;
    private readonly string _outputFilePath;
    private readonly int _year;
    private readonly int _month;

    public SummaryExporter(IEnumerable<RepoAction> repoActions,
        string outputFilePath,
        int year,
        int month
        )
    {
        _repoActions = repoActions;
        _outputFilePath = outputFilePath;
        _year = year;
        _month = month;
    }

    public async Task ExportSummaryAsync()
    {
        var parentDirectory = Path.GetDirectoryName(_outputFilePath);
        if (!Directory.Exists(parentDirectory))
        {
            Directory.CreateDirectory(parentDirectory!);
        }

        var minDate = new DateTimeOffset(_year, _month, 1, 0, 0, 0, TimeSpan.Zero);
        var maxDate = minDate.AddMonths(1);

        using var fs = File.CreateText(_outputFilePath);
        foreach (var repoAction in _repoActions
            .OrderBy(a => a.Date)
            .Where(ra => ra.Date >= minDate && ra.Date < maxDate))
        {
            if (repoAction.Content is Commit commit)
            {
                await fs.WriteLineAsync($"Direkter Commit: {commit.Message}");
                await fs.WriteLineAsync($"  ID: {commit.ShortObjectId}");
                await fs.WriteLineAsync($"  Fertiggestellt: {commit.AuthoredDate.ToLocalTime():dd.MM.yyyy HH:mm}");
                await fs.WriteLineAsync($"  Änderungen: {commit.ChangedFiles} Dateien, +{commit.Additions:#,##0}/ -{commit.Deletions:#,##0} Zeilen");
            }
            else if (repoAction.Content is MergedPullRequest pullRequest)
            {
                await fs.WriteLineAsync($"#{pullRequest.Number}: {pullRequest.Title}");
                await fs.WriteLineAsync($"  Commits gesamt: {pullRequest.NumberOfCommits}");
                await fs.WriteLineAsync($"  Fertiggestellt: {pullRequest.MergedAt.ToLocalTime():dd.MM.yyyy HH:mm}");
                await fs.WriteLineAsync($"  Änderungen: {pullRequest.ChangedFiles} Dateien, +{pullRequest.Additions:#,##0}/ -{pullRequest.Deletions:#,##0} Zeilen");

                if (pullRequest.ClosedIssues?.Any() == true)
                {
                    await fs.WriteLineAsync($"  Geschlossene Issues:");
                    foreach (var closedIssue in pullRequest.ClosedIssues)
                    {
                        await fs.WriteLineAsync($"  - #{closedIssue.Number}: {closedIssue.Title}");
                    }
                }
            }
            else
            {
                throw new NotImplementedException();
            }

            await fs.WriteLineAsync();
        }
    }
}
