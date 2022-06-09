public class MergedPullRequest
{
    public int Number { get; set; }
    
    public string? Title { get; set; }

    public DateTimeOffset MergedAt { get; set; }

    public int NumberOfCommits { get; set; }

    public int ChangedFiles { get; set; }

    public int Deletions { get; set; }

    public int Additions { get; set; }

    public List<ClosedIssue>? ClosedIssues { get; set; }

    public List<string> CommitObjectIds { get; set; }
}
