using Microsoft.VisualStudio.TestTools.UnitTesting;
using YamlDotNet.Core;

namespace Harp.Generators.Tests;

[TestClass]
public sealed class DeviceMetadataTests
{
    [DataTestMethod]
    [DataRow("errors.empty.yml", "empty")]
    [DataRow("errors.mask.yml", "does not fit in 32 bits")]
    [DataRow("errors.validation.yml",
        "(PulseWidth)",
        "(Configuration.Level)",
        "(TriggerMode)",
        "(DeviceLabel)",
        "(OutputState.Enabled)",
        "(Timing.Period)",
        "(Window)",
        "(Gain)",
        "(Calibration.Scale)",
        "(Channels.Off)",
        "(Channels.Both)",
        "(Channels.Again)",
        "(LineState)",
        "(LineConfig.Low)",
        "(Color1)")]
    public void Load_InvalidMetadataThrows(string metadataFileName, params string[] expectedErrors)
    {
        metadataFileName = TestHelper.GetMetadataPath(metadataFileName);
        var exception = Assert.ThrowsException<YamlException>(() => DeviceMetadata.Load(metadataFileName));
        foreach (var expectedError in expectedErrors)
            StringAssert.Contains(exception.Message, expectedError);
    }
}
