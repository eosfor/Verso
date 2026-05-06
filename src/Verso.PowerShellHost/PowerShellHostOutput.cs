namespace Verso.PowerShellHost;

public sealed record PowerShellHostOutput(
    string MimeType,
    string Content,
    bool IsError = false,
    string? ErrorName = null);
