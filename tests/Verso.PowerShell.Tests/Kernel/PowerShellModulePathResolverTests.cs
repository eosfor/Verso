using Verso.PowerShell.Kernel;

namespace Verso.PowerShell.Tests.Kernel;

[TestClass]
public class PowerShellModulePathResolverTests
{
    private string _tempRoot = null!;

    [TestInitialize]
    public void Setup()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "verso-psmodulepath-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    [TestMethod]
    public void AddModulePathIfUseful_AppendsPathWithPowerShellGet()
    {
        var candidate = CreateModuleRoot("PowerShellGet");
        var existing = Path.Combine(_tempRoot, "existing");
        Directory.CreateDirectory(existing);

        var result = PowerShellModulePathResolver.AddModulePathIfUseful(existing, candidate);

        var paths = SplitModulePath(result);
        CollectionAssert.AreEqual(new[] { existing, candidate }, paths);
    }

    [TestMethod]
    public void AddModulePathIfUseful_AppendsPathWithPSResourceGet()
    {
        var candidate = CreateModuleRoot("Microsoft.PowerShell.PSResourceGet");

        var result = PowerShellModulePathResolver.AddModulePathIfUseful(null, candidate);

        var paths = SplitModulePath(result);
        CollectionAssert.AreEqual(new[] { candidate }, paths);
    }

    [TestMethod]
    public void AddModulePathIfUseful_DoesNotAppendPathWithoutModuleManagementModules()
    {
        var candidate = Path.Combine(_tempRoot, "pwsh", "Modules");
        Directory.CreateDirectory(candidate);
        var existing = Path.Combine(_tempRoot, "existing");

        var result = PowerShellModulePathResolver.AddModulePathIfUseful(existing, candidate);

        Assert.AreEqual(existing, result);
    }

    [TestMethod]
    public void AddModulePathIfUseful_DoesNotDuplicateExistingPath()
    {
        var candidate = CreateModuleRoot("PackageManagement");
        var current = string.Join(Path.PathSeparator, new[] { candidate, Path.Combine(_tempRoot, "other") });

        var result = PowerShellModulePathResolver.AddModulePathIfUseful(current, candidate);

        var paths = SplitModulePath(result);
        Assert.AreEqual(2, paths.Length);
        Assert.AreEqual(candidate, paths[0]);
    }

    private string CreateModuleRoot(string moduleName)
    {
        var moduleRoot = Path.Combine(_tempRoot, "pwsh", "Modules");
        Directory.CreateDirectory(Path.Combine(moduleRoot, moduleName));
        return moduleRoot;
    }

    private static string[] SplitModulePath(string modulePath)
    {
        return modulePath.Split(
            Path.PathSeparator,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
