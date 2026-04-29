namespace Verso.PowerShellHost;

public delegate Task PowerShellHostOutputCallback(PowerShellHostOutput output);

public delegate PowerShellHostOutputCallback? PowerShellHostOutputCallbackProvider();
