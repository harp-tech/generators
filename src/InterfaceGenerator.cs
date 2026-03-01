using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Harp.Generators;

/// <summary>
/// Provides automatic generation of reactive device interface implementations.
/// </summary>
public sealed class InterfaceGenerator
{
    readonly Device _deviceTemplate = new();
    readonly AsyncDevice _asyncDeviceTemplate = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="InterfaceGenerator"/> class with the
    /// specified device metadata and target namespace for generated code.
    /// </summary>
    /// <param name="deviceMetadata">The device metadata object.</param>
    /// <param name="ns">The target namespace to use for all generated code.</param>
    public InterfaceGenerator(DeviceInfo deviceMetadata, string ns)
    {
        var session = new Dictionary<string, object>
        {
            { "Namespace", ns },
            { "DeviceMetadata", deviceMetadata }
        };
        _deviceTemplate.Session = session;
        _asyncDeviceTemplate.Session = session;
        _deviceTemplate.Initialize();
        _asyncDeviceTemplate.Initialize();
    }

    /// <summary>
    /// Generates a device interface implementation complying with the specified metadata file.
    /// </summary>
    /// <returns>The generated device interface implementation.</returns>
    public InterfaceImplementation GenerateImplementation() =>
        new(Device: _deviceTemplate.TransformText(),
            AsyncDevice: _asyncDeviceTemplate.TransformText());
}

/// <summary>
/// Represents the generated device interface implementation.
/// </summary>
/// <param name="Device">The generated source code implementing the device reactive interface.</param>
/// <param name="AsyncDevice">The generated source code for the device async interface implementation.</param>
public record struct InterfaceImplementation(string Device, string AsyncDevice)
    : IEnumerable<KeyValuePair<string, string>>
{
    /// <summary>
    /// Represents the default name for the file storing the device implementation source code.
    /// </summary>
    public const string DeviceFileName = "Device.Generated.cs";

    /// <summary>
    /// Represents the default name for the file storing the async device implementation source code.
    /// </summary>
    public const string AsyncDeviceFileName = "AsyncDevice.Generated.cs";

    /// <summary>
    /// Returns an enumerator that iterates through all the source code files in the
    /// generated implementation.
    /// </summary>
    /// <returns>
    /// An enumerator that can be used to iterate through the generated implementation files.
    /// </returns>
    public readonly IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        yield return new(DeviceFileName, Device);
        yield return new(AsyncDeviceFileName, AsyncDevice);
    }

    readonly IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
