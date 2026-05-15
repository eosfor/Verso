using System.Diagnostics;

namespace Verso.PowerShell.Kernel;

internal static class PowerShellModulePathResolver
{
    private static readonly string[] ModuleManagementModules =
    {
        "PowerShellGet",
        "PackageManagement",
        "Microsoft.PowerShell.PSResourceGet"
    };

    public static string EnhanceWithExternalPowerShellModules(string? currentModulePath)
    {
        var externalModulePath = TryGetExternalPowerShellModulePath();
        return AddModulePathIfUseful(currentModulePath, externalModulePath);
    }

    internal static string AddModulePathIfUseful(string? currentModulePath, string? candidateModulePath)
    {
        if (string.IsNullOrWhiteSpace(candidateModulePath) ||
            !Directory.Exists(candidateModulePath) ||
            !ContainsModuleManagementModule(candidateModulePath))
        {
            return currentModulePath ?? string.Empty;
        }

        var separator = Path.PathSeparator;
        var comparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        var paths = (currentModulePath ?? string.Empty)
            .Split(separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        if (paths.Any(path => comparer.Equals(NormalizePath(path), NormalizePath(candidateModulePath))))
        {
            return string.Join(separator, paths);
        }

        paths.Add(candidateModulePath);
        return string.Join(separator, paths);
    }

    private static string? TryGetExternalPowerShellModulePath()
    {
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "pwsh",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            process.StartInfo.ArgumentList.Add("-NoLogo");
            process.StartInfo.ArgumentList.Add("-NoProfile");
            process.StartInfo.ArgumentList.Add("-NonInteractive");
            process.StartInfo.ArgumentList.Add("-Command");
            process.StartInfo.ArgumentList.Add("$PSHOME");

            if (!process.Start())
            {
                return null;
            }

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit(3000))
            {
                try { process.Kill(entireProcessTree: true); }
                catch { /* best effort */ }
                return null;
            }

            if (process.ExitCode != 0)
            {
                return null;
            }

            _ = errorTask.GetAwaiter().GetResult();
            var psHome = outputTask.GetAwaiter().GetResult()
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(psHome))
            {
                return null;
            }

            return Path.Combine(psHome, "Modules");
        }
        catch
        {
            return null;
        }
    }

    private static bool ContainsModuleManagementModule(string modulePath)
    {
        return ModuleManagementModules.Any(moduleName =>
            Directory.Exists(Path.Combine(modulePath, moduleName)));
    }

    private static string NormalizePath(string path)
    {
        try
        {
            return Path.GetFullPath(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }
        catch
        {
            return path;
        }
    }
}
