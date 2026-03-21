namespace MultiTool.Core.Results;

public sealed record ValidationIssue(string PropertyName, string Message);
