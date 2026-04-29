namespace Verso.PowerShellHost;

public sealed record PowerShellHostInputRequest(
    string Prompt,
    bool IsPassword = false);
