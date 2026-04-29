using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Security;

namespace Verso.PowerShellHost;

public sealed class VersoPowerShellHostUserInterface : PSHostUserInterface
{
    private const string PlainText = "text/plain";
    private const string InteractiveInputMessage = "Interactive PowerShell input is not supported by Verso yet.";

    private readonly PowerShellHostOutputCallbackProvider _outputCallbackProvider;
    private readonly VersoPowerShellRawUserInterface _rawUI = new();

    public VersoPowerShellHostUserInterface(PowerShellHostOutputCallbackProvider outputCallbackProvider)
    {
        _outputCallbackProvider = outputCallbackProvider ?? throw new ArgumentNullException(nameof(outputCallbackProvider));
    }

    public override PSHostRawUserInterface RawUI => _rawUI;

    public override bool SupportsVirtualTerminal => true;

    public override string ReadLine() => throw CreateInteractiveInputException();

    public override SecureString ReadLineAsSecureString() => throw CreateInteractiveInputException();

    public override Dictionary<string, PSObject> Prompt(
        string caption,
        string message,
        Collection<FieldDescription> descriptions) =>
        throw CreateInteractiveInputException();

    public override PSCredential PromptForCredential(
        string caption,
        string message,
        string userName,
        string targetName) =>
        throw CreateInteractiveInputException();

    public override PSCredential PromptForCredential(
        string caption,
        string message,
        string userName,
        string targetName,
        PSCredentialTypes allowedCredentialTypes,
        PSCredentialUIOptions options) =>
        throw CreateInteractiveInputException();

    public override int PromptForChoice(
        string caption,
        string message,
        Collection<ChoiceDescription> choices,
        int defaultChoice) =>
        throw CreateInteractiveInputException();

    public override void Write(string value)
    {
        Emit(value);
    }

    public override void Write(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
    {
        Emit(value);
    }

    public override void WriteLine()
    {
        Emit(Environment.NewLine);
    }

    public override void WriteLine(string value)
    {
        Emit(value);
    }

    public override void WriteLine(ConsoleColor foregroundColor, ConsoleColor backgroundColor, string value)
    {
        Emit(value);
    }

    public override void WriteErrorLine(string value)
    {
        Emit(value, isError: true, errorName: "PSError");
    }

    public override void WriteDebugLine(string message)
    {
        Emit($"DEBUG: {message}");
    }

    public override void WriteProgress(long sourceId, ProgressRecord record)
    {
        // Progress records are intentionally suppressed for now. Streaming every
        // update would create noisy notebook output and needs a dedicated UI shape.
    }

    public override void WriteVerboseLine(string message)
    {
        Emit($"VERBOSE: {message}");
    }

    public override void WriteWarningLine(string message)
    {
        Emit($"WARNING: {message}");
    }

    public override void WriteInformation(InformationRecord record)
    {
        // Write-Host reaches the host UI through Write/WriteLine. Emitting
        // InformationRecord as well would duplicate the same visible output.
    }

    private void Emit(string? content, bool isError = false, string? errorName = null)
    {
        if (string.IsNullOrEmpty(content))
            return;

        var callback = _outputCallbackProvider();
        if (callback is null)
            return;

        var output = new PowerShellHostOutput(PlainText, content, isError, errorName);
        callback(output).GetAwaiter().GetResult();
    }

    private static PSNotSupportedException CreateInteractiveInputException() =>
        new(InteractiveInputMessage);
}
