namespace Verso.Blazor.Shared.Tests;

[TestClass]
public sealed class PanelDisplayNamesTests
{
    [TestMethod]
    public void For_Properties_ReturnsCellProperties()
    {
        Assert.AreEqual("CELL PROPERTIES", PanelDisplayNames.For("properties"));
    }

    [TestMethod]
    public void For_KnownPanels_ReturnsUppercaseLabels()
    {
        Assert.AreEqual("METADATA", PanelDisplayNames.For("metadata"));
        Assert.AreEqual("EXTENSIONS", PanelDisplayNames.For("extensions"));
        Assert.AreEqual("VARIABLES", PanelDisplayNames.For("variables"));
        Assert.AreEqual("SETTINGS", PanelDisplayNames.For("settings"));
    }

    [TestMethod]
    public void For_UnknownPanel_FallsBackToUppercase()
    {
        Assert.AreEqual("CUSTOM-PANEL", PanelDisplayNames.For("custom-panel"));
    }
}
