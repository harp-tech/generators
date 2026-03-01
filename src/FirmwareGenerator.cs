using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Harp.Generators;

/// <summary>
/// Provides automatic generation of device firmware implementations.
/// </summary>
public class FirmwareGenerator
{
    readonly App _appTemplate = new();
    readonly AppImpl _appImplTemplate = new();
    readonly AppFuncs _appFuncsTemplate = new();
    readonly AppFuncsImpl _appFuncsImplTemplate = new();
    readonly AppRegs _appRegsTemplate = new();
    readonly AppRegsImpl _appRegsImplTemplate = new();
    readonly Interrupts _interruptsTemplate = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="FirmwareGenerator"/> class with the
    /// specified device metadata and IO pin configuration.
    /// </summary>
    /// <param name="deviceMetadata">The device metadata object.</param>
    /// <param name="portPinMetadata">The IO pin configuration map.</param>
    public FirmwareGenerator(DeviceInfo deviceMetadata, Dictionary<string, PortPinInfo> portPinMetadata)
    {
        var session = new Dictionary<string, object>
        {
            { "DeviceMetadata", deviceMetadata },
            { "PortPinMetadata", portPinMetadata }
        };
        _appTemplate.Session = session;
        _appImplTemplate.Session = session;
        _appFuncsTemplate.Session = session;
        _appFuncsImplTemplate.Session = session;
        _appRegsTemplate.Session = session;
        _appRegsImplTemplate.Session = session;
        _interruptsTemplate.Session = session;
        _appTemplate.Initialize();
        _appImplTemplate.Initialize();
        _appFuncsTemplate.Initialize();
        _appFuncsImplTemplate.Initialize();
        _appRegsTemplate.Initialize();
        _appRegsImplTemplate.Initialize();
        _interruptsTemplate.Initialize();
    }

    /// <summary>
    /// Generates firmware header files complying with the specified metadata file.
    /// </summary>
    /// <returns>The generated device firmware headers.</returns>
    public FirmwareHeaders GenerateHeaders() =>
        new(App: _appTemplate.TransformText(),
            AppFuncs: _appFuncsTemplate.TransformText(),
            AppRegs: _appRegsTemplate.TransformText());

    /// <summary>
    /// Generates firmware implementation stubs complying with the specified metadata file.
    /// </summary>
    /// <returns>The generated device firmware implementation.</returns>
    public FirmwareImplementation GenerateImplementation() =>
        new(App: _appImplTemplate.TransformText(),
            AppFuncs: _appFuncsImplTemplate.TransformText(),
            AppRegs: _appRegsImplTemplate.TransformText(),
            Interrupts: _interruptsTemplate.TransformText());
}

/// <summary>
/// Represents the generated device firmware header files.
/// </summary>
/// <param name="App">The contents of the header file declaring the app initialization function.</param>
/// <param name="AppFuncs">The contents of the header file declaring app register functions.</param>
/// <param name="AppRegs">The contents of the header file declaring app IO pins and register data.</param>
public record struct FirmwareHeaders(string App, string AppFuncs, string AppRegs)
    : IEnumerable<KeyValuePair<string, string>>
{
    /// <summary>
    /// Represents the default name for the header file declaring the app initialization function.
    /// </summary>
    public const string AppFileName = "app.h";

    /// <summary>
    /// Represents the default name for the header file declaring app register functions.
    /// </summary>
    public const string AppFuncsFileName = "app_funcs.h";

    /// <summary>
    /// Represents the default name for the header file declaring app IO pins and register data.
    /// </summary>
    public const string AppRegsFileName = "app_ios_and_regs.h";

    /// <summary>
    /// Returns an enumerator that iterates through all the header files in the
    /// generated implementation.
    /// </summary>
    /// <returns>
    /// An enumerator that can be used to iterate through the generated header files.
    /// </returns>
    public readonly IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        yield return new(AppFileName, App);
        yield return new(AppFuncsFileName, AppFuncs);
        yield return new(AppRegsFileName, AppRegs);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

/// <summary>
/// Represents the generated device firmware implementation.
/// </summary>
/// <param name="App">The generated source code implementing app initialization.</param>
/// <param name="AppFuncs">The generated source code implementing app functions.</param>
/// <param name="AppRegs">The generated source code implementing initialization of IO pins.</param>
/// <param name="Interrupts">The generated source code implementing interrupt service routines.</param>
public record struct FirmwareImplementation(string App, string AppFuncs, string AppRegs, string Interrupts)
    : IEnumerable<KeyValuePair<string, string>>
{
    /// <summary>
    /// Represents the default name for the file storing the app initialization source code.
    /// </summary>
    public const string AppFileName = "app.c";

    /// <summary>
    /// Represents the default name for the file storing the source code for app functions.
    /// </summary>
    public const string AppFuncsFileName = "app_funcs.c";

    /// <summary>
    /// Represents the default name for the file storing the initialization source code for IO pins.
    /// </summary>
    public const string AppRegsFileName = "app_ios_and_regs.c";

    /// <summary>
    /// Represents the default name for the file storing the source code for interrupt service routines.
    /// </summary>
    public const string InterruptsFileName = "interrupts.c";

    /// <summary>
    /// Returns an enumerator that iterates through all the source code files in the
    /// generated implementation.
    /// </summary>
    /// <returns>
    /// An enumerator that can be used to iterate through the generated implementation files.
    /// </returns>
    public readonly IEnumerator<KeyValuePair<string, string>> GetEnumerator()
    {
        yield return new(AppFileName, App);
        yield return new(AppFuncsFileName, AppFuncs);
        yield return new(AppRegsFileName, AppRegs);
        yield return new(InterruptsFileName, Interrupts);
    }

    readonly IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
