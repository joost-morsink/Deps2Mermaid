using Deps;
using Version = Deps.Version;

namespace Deps2Mermaid.Tests;

[TestClass]
public sealed class VersionTests
{
    [TestMethod]
    [DataRow("1.0.0")]
    [DataRow("1.0.0-test")]
    [DataRow("1.0.0-*")]
    [DataRow("1.*")]
    [DataRow("1.0.*")]
    [DataRow("1.0.z", false)]
    [DataRow("abc", false)]
    public void ParseVersionTest(string value, bool shouldSucceed = true, string? expected = null)
    {
        var v = Version.TryParse(value);
        Assert.AreEqual(shouldSucceed, v is not null);
        if (shouldSucceed)
            Assert.AreEqual(expected ?? value, v.ToString());
    }

    [TestMethod]
    [DataRow(1, 0, 0, null, "1.0.0")]
    [DataRow(1, VersionPart.STAR, 123, null, "1.*")]
    [DataRow(1, 2, VersionPart.STAR, null, "1.2.*")]
    [DataRow(1, 2, VersionPart.STAR, "abc", "1.2.*-abc")]
    [DataRow(1, 2, 3, "*", "1.2.3-*")]
    [DataRow(1, 2, Version.ABSENT, null, "1.2")]
    [DataRow(1, 2, VersionPart.UNKNOWN, null, "1.2")]
    public void ToStringTest(int Major, int Minor, int Patch, string? Tag, string expected)
    {
        var v = new Version(Major, Minor, Patch, Tag);
        Assert.AreEqual(expected, v.ToString());
    }
}