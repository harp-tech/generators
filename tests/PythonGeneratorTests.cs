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
}
