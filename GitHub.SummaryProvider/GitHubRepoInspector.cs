using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class GitHubRepoInspector
{
    private readonly string _organization;
    private readonly string _repository;
    private readonly string _branchName;
    private readonly string _token;

    public GitHubRepoInspector(string organization,
        string repository,
        string branchName,
        string token)
    {
        _organization = organization;
        _repository = repository;
        _branchName = branchName;
        _token = token;
    }

    private HttpClient GetGitHubHttpClient()
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
        httpClient.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("Dangl.GitHub.SummaryProvider", "1.0"));
        return httpClient;
    }

    public async Task<List<MergedPullRequest>> GetPullRequestDataAsync()
    {
        var httpClient = GetGitHubHttpClient();

        var lastCursor = string.Empty;
        var hasMore = true;
        var mergedPullRequests = new List<MergedPullRequest>();
        while (hasMore)
        {
            var query = GetGraphQlQueryForPullRequests(lastCursor);
            var jsonQuery = JsonConvert.SerializeObject(new
            {
                query
            });

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.github.com/graphql")
            {
                Content = new StringContent(jsonQuery)
            };

            var response = await httpClient.SendAsync(request);
            var responseJson = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to get data from GitHub, status code: {response.StatusCode}{Environment.NewLine}{responseJson}");
            }

            var responseObject = JsonConvert.DeserializeObject<dynamic>(responseJson);

            hasMore = responseObject!.data.repository.pullRequests.pageInfo.hasNextPage;
            lastCursor = responseObject.data.repository.pullRequests.pageInfo.endCursor;

            var parsedArray = JObject.Parse(responseJson);
            var elementsArray = parsedArray["data"]!["repository"]!["pullRequests"]!["nodes"] as JArray;
            foreach (var jsonElement in elementsArray!.OfType<JObject>())
            {
                var mergedPullRequest = GetMergedPullRequestFromGitHubApiObject(jsonElement);
                mergedPullRequests.Add(mergedPullRequest);
            }
        }

        return mergedPullRequests;
    }

    public async Task<List<Commit>> GetCommitsForDevelopBranchAsync()
    {
        var httpClient = GetGitHubHttpClient();

        var lastCursor = string.Empty;
        var hasMore = true;
        var commits = new List<Commit>();
        while (hasMore)
        {
            var query = GetGraphlQlQueryForCommits(lastCursor, _branchName);
            var jsonQuery = JsonConvert.SerializeObject(new
            {
                query
            });

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.github.com/graphql")
            {
                Content = new StringContent(jsonQuery)
            };

            var response = await httpClient.SendAsync(request);
            var responseJson = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to get data from GitHub, status code: {response.StatusCode}{Environment.NewLine}{responseJson}");
            }

            var responseObject = JsonConvert.DeserializeObject<dynamic>(responseJson);

            hasMore = responseObject!.data.repository.@ref.target.history.pageInfo.hasNextPage;
            lastCursor = responseObject.data.repository.@ref.target.history.pageInfo.endCursor;

            var parsedArray = JObject.Parse(responseJson);
            var elementsArray = parsedArray["data"]!["repository"]!["ref"]!["target"]!["history"]!["edges"] as JArray;
            foreach (var jsonElement in elementsArray!.OfType<JObject>())
            {
                var commit = GetCommitFromGitHubApiObject(jsonElement as JObject);
                if (commit?.HasPullRequestAssociated == true)
                {
                    commits.Add(commit);
                }
            }
        }

        return commits;
    }

    private string GetGraphlQlQueryForCommits(string lastCursor, string branchName)
    {
        var query = $@"{{
  repository(owner:""{_organization}"", name:""{_repository}"") {{
    ref(qualifiedName: ""{branchName}"") {{
                target {{
                ... on Commit {{
                    history(first:100{(string.IsNullOrWhiteSpace(lastCursor) ? string.Empty : $", after: \"{lastCursor}\"")}) {{
                      pageInfo {{
                        hasNextPage
                        endCursor
                      }}
                      edges {{
                          node {{
                              message
                                abbreviatedOid
                              authoredDate
                                additions
                                deletions
                                changedFiles
                              associatedPullRequests  {{
                                  totalCount
                              }}
                          }}
                      }}
                    }}
                }}
        }}
    }}
  }}
}}";

        return query;
    }

    private string GetGraphQlQueryForPullRequests(string lastCursor)
    {
        var query = $@"{{
  repository(owner:""{_organization}"", name:""{_repository}"") {{
    pullRequests(first:100, {(string.IsNullOrWhiteSpace(lastCursor) ? string.Empty : $"after: \"{lastCursor}\"")}states:MERGED, orderBy: {{field: CREATED_AT, direction: ASC}}) {{
      pageInfo {{
        hasNextPage
        endCursor
      }}
      nodes {{
        title
        number
        url
        createdAt
        mergedAt
        author {{
          login
        }},
        commits  {{
          totalCount
        }}
        deletions
        additions
        changedFiles
        closingIssuesReferences(first:100) {{
          nodes {{
            title
            number
          }}
        }}
      }}
    }}
  }}
}}";

        return query;
    }

    private Commit? GetCommitFromGitHubApiObject(JObject gitHubApiObject)
    {
        try
        {
            var commit = new Commit
            {
                Message = gitHubApiObject["node"]!["message"]!.ToString(),
                AuthoredDate = gitHubApiObject["node"]!["authoredDate"]!.ToObject<DateTimeOffset>(),
                HasPullRequestAssociated = gitHubApiObject["node"]!["associatedPullRequests"]!["totalCount"]!.Value<int>() > 0,
                Additions = gitHubApiObject["node"]!["additions"]!.Value<int>(),
                ChangedFiles = gitHubApiObject["node"]!["changedFiles"]!.Value<int>(),
                Deletions = gitHubApiObject["node"]!["deletions"]!.Value<int>(),
                ShortObjectId = gitHubApiObject["node"]!["abbreviatedOid"]!.ToString(),
            };

            return commit;
        }
        catch
        {
            return null;
        }
    }

    private MergedPullRequest GetMergedPullRequestFromGitHubApiObject(JObject gitHubApiObject)
    {
        var mergedPullRequest = new MergedPullRequest
        {
            NumberOfCommits = gitHubApiObject["commits"]!["totalCount"]!.ToObject<int>(),
            Title = gitHubApiObject["title"]!.ToString(),
            Number = gitHubApiObject["number"]!.ToObject<int>(),
            MergedAt = gitHubApiObject["mergedAt"]!.ToObject<DateTimeOffset>(),
            Deletions = gitHubApiObject["deletions"]!.ToObject<int>(),
            Additions = gitHubApiObject["additions"]!.ToObject<int>(),
            ChangedFiles = gitHubApiObject["changedFiles"]!.ToObject<int>(),
            ClosedIssues = gitHubApiObject["closingIssuesReferences"]!["nodes"]!.Select(x => new ClosedIssue
            {
                Title = x["title"]!.ToString(),
                Number = x["number"]!.ToObject<int>()
            })
            .ToList()
        };

        return mergedPullRequest;
    }
}
