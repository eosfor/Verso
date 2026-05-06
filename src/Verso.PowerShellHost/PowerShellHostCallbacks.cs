namespace Verso.PowerShellHost;

public delegate Task PowerShellHostOutputCallback(PowerShellHostOutput output);

public delegate PowerShellHostOutputCallback? PowerShellHostOutputCallbackProvider();

public delegate Task<string?> PowerShellHostInputCallback(PowerShellHostInputRequest request);

public delegate PowerShellHostInputCallback? PowerShellHostInputCallbackProvider();
