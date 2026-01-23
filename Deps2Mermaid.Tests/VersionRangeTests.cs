using Deps;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;

namespace Deps2Mermaid.Tests;

[TestClass]
public sealed class VersionRangeTests
{
    [TestMethod]
    [DataRow("1.0.0")]
    [DataRow("[1.0.0,)")]
    [DataRow("[1.2.3,2)")]
    [DataRow("[1.2.3,2.3.4]")]
    [DataRow(" [1.2.3, 2.0.0) ", true, "[1.2.3,2.0.0)")]
    [DataRow(">= 2.0.0", true, "[2.0.0,)")]
    [DataRow(">= 1.2.3 < 2.0.0", true, "[1.2.3,2.0.0)")]
    public void ParseVersionRangeTest(string value, bool shouldSucceed = true, string? expected = null)
    {
        var v = VersionRange.Parse(value);
        Assert.AreEqual(shouldSucceed, v != null);
        if (shouldSucceed)
            Assert.AreEqual(expected ?? value, v.ToString());
    }
}