namespace MultiTool.Core.Results;

public sealed class ValidationResult
{
    public ValidationResult(IEnumerable<ValidationIssue> issues)
    {
        Issues = issues.ToArray();
    }

    public IReadOnlyList<ValidationIssue> Issues { get; }

    public bool IsValid => Issues.Count == 0;

    public string Summary => string.Join(Environment.NewLine, Issues.Select(issue => issue.Message));
}
