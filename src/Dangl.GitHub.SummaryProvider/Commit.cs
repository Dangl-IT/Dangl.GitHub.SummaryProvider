public class Commit
{
    public string? Message { get; set; }

    public string? ShortObjectId { get; set; }

    public DateTimeOffset AuthoredDate { get; set; }

    public bool HasPullRequestAssociated { get; set; }

    public int ChangedFiles { get; set; }

    public int Deletions { get; set; }

    public int Additions { get; set; }

    public string ObjectId { get; set; }
}
