using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Harp.Generators.Tests;

[TestClass]
public sealed class PythonGeneratorTests
{
    DirectoryInfo? outputDirectory;

    [TestInitialize]
    public void Initialize()
    {
        outputDirectory = Directory.CreateDirectory("PythonOutput");
        try { Directory.Delete(outputDirectory.FullName, recursive: true); }
        catch { } // best effort
    }

    [DataTestMethod]
    [DataRow("core.yml")]
    [DataRow("device.yml")]
    [DataRow("device.coremasks.yml")]
    public void DeviceTemplate_GenerateMatchesExpectedOutput(string metadataFileName)
    {
        metadataFileName = TestHelper.GetMetadataPath(metadataFileName);
        var deviceMetadata = DeviceMetadata.Load(metadataFileName);
        var generator = new PythonGenerator(deviceMetadata);
        var implementation = generator.GenerateImplementation();
        TestHelper.AssertNoGeneratorErrors(generator.Errors);

        var outputFileName = $"{Path.GetFileNameWithoutExtension(metadataFileName)}.py";
        try
        {
            TestHelper.AssertExpectedOutput(implementation.Device, outputFileName);
        }
        catch (AssertFailedException)
        {
            if (outputDirectory is not null)
            {
                outputDirectory.Create();
                File.WriteAllText(Path.Combine(outputDirectory.FullName, outputFileName), implementation.Device);
            }
            throw;
        }
    }

    static List<string> GenerateDeviceLines(DeviceMetadata deviceMetadata)
    {
        var generator = new PythonGenerator(deviceMetadata);
        var implementation = generator.GenerateImplementation();
        TestHelper.AssertNoGeneratorErrors(generator.Errors);
        return implementation.Device.ReplaceLineEndings("\n").Split('\n').ToList();
    }

    static List<string> GetMetadataLiteral(List<string> lines)
    {
        var start = lines.IndexOf("DEVICE_METADATA: str = \"\"\"\\");
        Assert.AreNotEqual(-1, start, "The generated interface does not declare DEVICE_METADATA.");
        // The literal is closed on the line right before the attribute docstring that follows it.
        var docstring = lines.FindIndex(start + 1, line => line.StartsWith("\"\"\"The contents of the metadata file"));
        Assert.AreNotEqual(-1, docstring, "The DEVICE_METADATA literal is not followed by its docstring.");
        return lines.GetRange(start + 1, docstring - start - 1);
    }

    [TestMethod]
    public void DeviceTemplate_EmbedsSourceMetadata()
    {
        var metadataFileName = TestHelper.GetMetadataPath("device.yml");
        var lines = GenerateDeviceLines(DeviceMetadata.Load(metadataFileName));
        CollectionAssert.Contains(lines, "    \"DEVICE_METADATA\",");

        var expected = File.ReadAllText(metadataFileName).ReplaceLineEndings("\n").Split('\n').ToList();
        expected[^1] += "\"\"\"";
        CollectionAssert.AreEqual(expected, GetMetadataLiteral(lines));
    }

    [TestMethod]
    public void DeviceTemplate_EscapesMetadataLiteral()
    {
        // Comments reach the literal verbatim, so they exercise escaping without affecting
        // the parsed metadata. The document deliberately ends in a quote with no final newline.
        var metadata = DeviceMetadata.Parse(string.Join("\r\n",
            "# quotes: \"\"\" and \"\"\"\" and a \\ backslash",
            "# control: \u0001 and\ttab",
            "device: Escapes",
            "whoAmI: 1",
            "# ends with \"\"\""));
        var literal = GetMetadataLiteral(GenerateDeviceLines(metadata));
        CollectionAssert.AreEqual(new[]
        {
            "# quotes: \"\"\\\" and \"\"\\\"\" and a \\\\ backslash",
            "# control: \\x01 and\ttab",
            "device: Escapes",
            "whoAmI: 1",
            "# ends with \"\"\\\"\"\"\""
        }, literal);
    }

    [TestMethod]
    public void DeviceTemplate_MissingSourceTextReportsError()
    {
        var metadata = new DeviceMetadata { Device = "Constructed", WhoAmI = 1 };
        var generator = new PythonGenerator(metadata);
        generator.GenerateImplementation();
        TestHelper.AssertExpectedGeneratorErrors(generator.Errors, "has no source text");
    }

    [TestMethod]
    public void DeviceTemplate_CoreOmitsMetadata()
    {
        var metadataFileName = TestHelper.GetMetadataPath("core.yml");
        var device = new PythonGenerator(DeviceMetadata.Load(metadataFileName)).GenerateImplementation().Device;
        StringAssert.DoesNotMatch(device, new System.Text.RegularExpressions.Regex("DEVICE_METADATA"));
    }
}
