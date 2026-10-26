using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Harp.Generators.Tests;

[TestClass]
public sealed class DocumentationGeneratorTests
{
    DirectoryInfo? outputDirectory;

    [TestInitialize]
    public void Initialize()
    {
        outputDirectory = Directory.CreateDirectory("DocumentationOutput");
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
        var generator = new DocumentationGenerator(deviceMetadata, typeof(DocumentationGeneratorTests).Namespace ?? "");
        var implementation = generator.GenerateImplementation();
        TestHelper.AssertNoGeneratorErrors(generator.Errors);

        var documentation = new StringBuilder();
        foreach (var page in implementation)
        {
            if (documentation.Length > 0) documentation.AppendLine();
            documentation.AppendLine($"# {page.Key}");
            documentation.AppendLine();
            documentation.Append(page.Value);
        }

        var outputFileName = $"{Path.GetFileNameWithoutExtension(metadataFileName)}.md";
        try
        {
            TestHelper.AssertExpectedOutput(documentation.ToString(), outputFileName);
        }
        catch (AssertFailedException)
        {
            if (outputDirectory is not null)
            {
                outputDirectory.Create();
                File.WriteAllText(Path.Combine(outputDirectory.FullName, outputFileName), documentation.ToString());
            }
            throw;
        }
    }
}
