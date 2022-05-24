using CommandLine;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

public class ExportOptions
{
    private string? _exportDate;    
    
    [Option('o', "organization", Required = true, HelpText = "The GitHub parent organization")]
    public string? GitHubOrganization { get; set; }

    [Option('r', "repository", Required = true, HelpText = "The GitHub repository to scan")]
    public string? GitHubRepository { get; set; }

    [Option('b', "branch", Required = true, HelpText = "The develop branch unto which beta versions are merged into")]
    public string? DevelopBranch { get; set; }

    [Option('t', "token", Required = true, HelpText = "Your GitHub Personal Access Token")]
    public string? GitHubPersonalAccessToken { get; set; }

    [Option('f', "folder", Required = true, HelpText = "Base path under which to place the data export")]
    public string? ExportBaseFolder { get; set; }

    [Option('d', "date", Required = true, HelpText = "Date for which month to download documents, must be in format MM/yyyy, e.g. 05/2020")]
    public string? ExportDate
    {
        get => _exportDate;
        set
        {
            var dateRegex = @"^(0[1-9]|1[012])[\/]\d{4}$";
            if (value == null || !Regex.IsMatch(value, dateRegex))
            {
                throw new TargetInvocationException("The format to specifiy the date for which exports are run must be MM/yyyy, e.g. 05/2020", null);
            }
            _exportDate = value;
        }
    }

    internal int DocumentExportMonth => Convert.ToInt32(_exportDate!.Substring(0, 2), CultureInfo.InvariantCulture);
    internal int DocumentExportYear => Convert.ToInt32(_exportDate!.Substring(3), CultureInfo.InvariantCulture);
}
