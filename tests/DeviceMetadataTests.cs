using Microsoft.VisualStudio.TestTools.UnitTesting;
using YamlDotNet.Core;

namespace Harp.Generators.Tests;

[TestClass]
public sealed class DeviceMetadataTests
{
    [DataTestMethod]
    [DataRow("errors.empty.yml", "empty")]
    [DataRow("errors.validation.yml",
        "(PulseWidth)",
        "(Configuration.Level)",
        "(TriggerMode)",
        "(DeviceLabel)",
        "(OutputState.Enabled)",
        "(Color1)")]
    public void Load_InvalidMetadataThrows(string metadataFileName, params string[] expectedErrors)
    {
        metadataFileName = TestHelper.GetMetadataPath(metadataFileName);
        var exception = Assert.ThrowsException<YamlException>(() => DeviceMetadata.Load(metadataFileName));
        foreach (var expectedError in expectedErrors)
            StringAssert.Contains(exception.Message, expectedError);
    }
}
