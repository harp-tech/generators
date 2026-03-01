using Microsoft.VisualStudio.TestTools.UnitTesting;
using YamlDotNet.Core;

namespace Harp.Generators.Tests;

static class TestHelper
{
    public static Stream GetManifestResourceStream(string name)
    {
        var qualifierType = typeof(TestHelper);
        var embeddedWorkflowStream = qualifierType.Namespace + "." + name;
        return qualifierType.Assembly.GetManifestResourceStream(embeddedWorkflowStream)!;
    }

    public static string GetManifestResourceText(string name)
    {
        using var resourceStream = GetManifestResourceStream(name);
        if (resourceStream is null)
            return string.Empty;

        using var resourceReader = new StreamReader(resourceStream);
        return resourceReader.ReadToEnd();
    }

    public static string GetMetadataPath(string fileName)
    {
        return Path.Combine("Metadata", fileName);
    }

    public static DeviceInfo ReadDeviceMetadata(string path)
    {
        using var reader = new StreamReader(path);
        var parser = new MergingParser(new Parser(reader));
        return MetadataDeserializer.Instance.Deserialize<DeviceInfo>(parser);
    }

    public static Dictionary<string, PortPinInfo> ReadPortPinMetadata(string path)
    {
        using var reader = new StreamReader(path);
        return MetadataDeserializer.Instance.Deserialize<Dictionary<string, PortPinInfo>>(reader);
    }

    public static void AssertExpectedOutput(string actual, string outputFileName)
    {
        var expectedFileName = Path.Combine("ExpectedOutput", outputFileName);
        if (File.Exists(expectedFileName))
        {
            var expected = File.ReadAllText(expectedFileName);
            if (!string.Equals(actual, expected, StringComparison.InvariantCulture))
            {
                Assert.Fail($"The generated output has diverged from the reference: {outputFileName}");
            }
        }
    }
}
