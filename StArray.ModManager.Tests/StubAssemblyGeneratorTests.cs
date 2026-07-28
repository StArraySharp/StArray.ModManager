namespace StArray.ModManager.Tests;

[TestFixture]
public sealed class StubAssemblyGeneratorTests
{
    [TestCase("System.String", "RuntimeString")]
    [TestCase("System.Int32", "int")]
    [TestCase("Unknown.Managed.Type", "nint")]
    public void MapsRuntimeTypesToStableStubTypes(string runtimeType, string expected)
    {
        Assert.That(StubAssemblyGenerator.MapType(runtimeType), Is.EqualTo(expected));
    }

    [Test]
    public void SanitizesIdentifiersAndEscapesGeneratedLiterals()
    {
        Assert.Multiple(() =>
        {
            Assert.That(StubAssemblyGenerator.SanitizeIdentifier("1Bad+Name"), Is.EqualTo("_1Bad_Name"));
            Assert.That(StubAssemblyGenerator.SanitizeNamespace("Game.Bad+Part"), Is.EqualTo("Game.Bad_Part"));
            Assert.That(StubAssemblyGenerator.EscapeString("a\\b\"c"), Is.EqualTo("a\\\\b\\\"c"));
        });
    }
}
