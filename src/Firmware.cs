using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Bonsai.Harp;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Harp.Generators;

/// <summary>
/// Specifies whether an IO pin will be used as input or output.
/// </summary>
public enum PinDirection
{
    /// <summary>
    /// Specifies the pin will be used as an input.
    /// </summary>
    Input,

    /// <summary>
    /// Specifies the pin will be used as an output.
    /// </summary>
    Output
}

/// <summary>
/// Specifies how an input pin is configured to handle floating inputs.
/// </summary>
public enum InputPinMode
{
    /// <summary>
    /// Configures a pull-up on the input pin.
    /// </summary>
    PullUp,

    /// <summary>
    /// Configures a pull-down on the input pin.
    /// </summary>
    PullDown,

    /// <summary>
    /// Configures the input pin to a state of high impedance.
    /// </summary>
    TriState,

    /// <summary>
    /// Configures a bus-holder latch circuit on the input pin.
    /// </summary>
    BusHolder
}

/// <summary>
/// Specifies when the interrupt event for an input pin should be triggered.
/// </summary>
public enum TriggerMode
{
    /// <summary>
    /// No triggers will be configured for the input pin.
    /// </summary>
    None,

    /// <summary>
    /// The interrupt event will be triggered on a rising edge.
    /// </summary>
    Rising,

    /// <summary>
    /// The interrupt event will be triggered on a falling edge.
    /// </summary>
    Falling,

    /// <summary>
    /// The interrupt event will be triggered when the input pin
    /// changes logical state in either direction.
    /// </summary>
    Toggle,

    /// <summary>
    /// The interrupt event will be triggered when the input pin is in
    /// a logical low state.
    /// </summary>
    Low
}

/// <summary>
/// Specifies the priority of the interrupt event for an input pin.
/// </summary>
public enum InterruptPriority
{
    /// <summary>
    /// Specifies no interrupt events will be raised.
    /// </summary>
    Off,

    /// <summary>
    /// Specifies interrupt events will be raised with low priority.
    /// </summary>
    Low,

    /// <summary>
    /// Specifies interrupt events will be raised with normal priority.
    /// </summary>
    Medium,

    /// <summary>
    /// Specifies interrupt events will be raised with the highest priority.
    /// </summary>
    High
}

/// <summary>
/// Specifies the wiring configuration for an output pin.
/// </summary>
public enum OutputPinMode
{
    /// <summary>
    /// Configures the output pin to be driven hard to either VCC or GND
    /// following the digital logic state.
    /// </summary>
    Digital,

    /// <summary>
    /// Configures the output pin for Wired-OR connection.
    /// </summary>
    WiredOr,

    /// <summary>
    /// Configures the output pin for Wired-AND connection.
    /// </summary>
    WiredAnd,

    /// <summary>
    /// Configures the output pin for Wired-OR with pull-down operation.
    /// </summary>
    WiredOrPull,

    /// <summary>
    /// Configures the output pin for Wired-AND with pull-up operation.
    /// </summary>
    WiredAndPull
}

/// <summary>
/// Specifies the logical state of an IO pin.
/// </summary>
public enum LogicState
{
    /// <summary>
    /// Specifies the pin is in a logical low state.
    /// </summary>
    Low,

    /// <summary>
    /// Specifies the pin is in a logical high state.
    /// </summary>
    High
}

/// <summary>
/// Represents information about an IO pin configuration used to automatically generate firmware.
/// </summary>
public class PortPinInfo
{
    /// <summary>
    /// Specifies the microcontroller port where the pin is located.
    /// </summary>
    public string Port;

    /// <summary>
    /// Specifies the unique pin number in the defined port.
    /// </summary>
    public int PinNumber;

    /// <summary>
    /// Specifies whether the pin will be used as input or output.
    /// </summary>
    public PinDirection Direction;

    /// <summary>
    /// Specifies a summary description of the IO pin function.
    /// </summary>
    public string Description = "";
}

/// <summary>
/// Represents information about an input pin configuration used to automatically generate firmware.
/// </summary>
public class InputPinInfo : PortPinInfo
{
    /// <summary>
    /// Specifies how the input pin is configured to handle floating inputs.
    /// </summary>
    public InputPinMode PinMode;

    /// <summary>
    /// Specifies when the interrupt event for this pin should be triggered.
    /// </summary>
    public TriggerMode TriggerMode;

    /// <summary>
    /// Specifies the priority of the interrupt event for this pin.
    /// </summary>
    public InterruptPriority InterruptPriority;

    /// <summary>
    /// Specifies the interrupt number associated with this pin.
    /// </summary>
    public int InterruptNumber;
}

/// <summary>
/// Represents information about an output pin configuration used to automatically generate firmware.
/// </summary>
public class OutputPinInfo : PortPinInfo
{
    /// <summary>
    /// Specifies whether reading the state of the output pin is allowed.
    /// </summary>
    public bool AllowRead;

    /// <summary>
    /// Specifies the output pin wiring configuration.
    /// </summary>
    public OutputPinMode PinMode;

    /// <summary>
    /// Specifies the initial state of the output pin at boot time.
    /// </summary>
    public LogicState InitialState;

    /// <summary>
    /// Specifies whether the output logic of the pin should be inverted.
    /// </summary>
    public bool Invert;
}

internal static partial class TemplateHelper
{
    public static Dictionary<string, PortPinInfo> ReadPortPinMetadata(string path)
    {
        using var reader = new StreamReader(path);
        return MetadataDeserializer.Instance.Deserialize<Dictionary<string, PortPinInfo>>(reader);
    }

    public static IEnumerable<KeyValuePair<string, T>> GetPortPinsOfType<T>(IDictionary<string, PortPinInfo> portPins) where T : PortPinInfo
    {
        return from item in portPins
               where item.Value is T
               select new KeyValuePair<string, T>(item.Key, (T)item.Value);
    }

    public static string GetFirmwareType(PayloadType payloadType)
    {
        return payloadType switch
        {
            PayloadType.U8 => "uint8_t",
            PayloadType.S8 => "int8_t",
            PayloadType.U16 => "uint16_t",
            PayloadType.S16 => "int16_t",
            PayloadType.U32 => "uint32_t",
            PayloadType.S32 => "int32_t",
            PayloadType.U64 => "uint64_t",
            PayloadType.S64 => "int64_t",
            PayloadType.Float => "float_t",
            _ => throw new ArgumentOutOfRangeException(nameof(payloadType)),
        };
    }

    public static string GetFirmwareRegisterType(PayloadType payloadType)
    {
        return payloadType switch
        {
            PayloadType.U8 => "U8",
            PayloadType.S8 => "I8",
            PayloadType.U16 => "U16",
            PayloadType.S16 => "I16",
            PayloadType.U32 => "U32",
            PayloadType.S32 => "I32",
            PayloadType.U64 => "U64",
            PayloadType.S64 => "I64",
            PayloadType.Float => "FLOAT",
            _ => throw new ArgumentOutOfRangeException(nameof(payloadType)),
        };
    }

    public static string GetFirmwareSenseMode(TriggerMode trigger)
    {
        return trigger switch
        {
            TriggerMode.Rising => "SENSE_IO_EDGE_RISING",
            TriggerMode.Falling => "SENSE_IO_EDGE_FALLING",
            TriggerMode.Toggle => "SENSE_IO_EDGES_BOTH",
            TriggerMode.Low => "SENSE_IO_LOW_LEVEL",
            _ => throw new ArgumentOutOfRangeException(nameof(trigger))
        };
    }

    public static string GetFirmwarePullMode(InputPinMode pinMode)
    {
        return pinMode switch
        {
            InputPinMode.PullUp => "PULL_IO_UP",
            InputPinMode.PullDown => "PULL_IO_DOWN",
            InputPinMode.TriState => "PULL_IO_TRISTATE",
            InputPinMode.BusHolder => "PULL_IO_BUSHOLDER",
            _ => throw new ArgumentOutOfRangeException(nameof(pinMode))
        };
    }

    public static string GetFirmwareOutputIO(OutputPinMode pinMode)
    {
        return pinMode switch
        {
            OutputPinMode.Digital => "OUT_IO_DIGITAL",
            OutputPinMode.WiredOr => "OUT_IO_WIREDOR",
            OutputPinMode.WiredAnd => "OUT_IO_WIREDAND",
            OutputPinMode.WiredOrPull => "OUT_IO_WIREDORPULL",
            OutputPinMode.WiredAndPull => "OUT_IO_WIREDANDPULL",
            _ => throw new ArgumentOutOfRangeException(nameof(pinMode))
        };
    }

    public static string GetFirmwareInterruptPriority(InterruptPriority priority)
    {
        return priority switch
        {
            InterruptPriority.Off => "INT_LEVEL_OFF",
            InterruptPriority.Low => "INT_LEVEL_LOW",
            InterruptPriority.Medium => "INT_LEVEL_MED",
            InterruptPriority.High => "INT_LEVEL_HIGH",
            _ => throw new ArgumentOutOfRangeException(nameof(priority))
        };
    }
}

class PortPinInfoTypeConverter(IDeserializer deserializer) : IYamlTypeConverter
{
    static readonly string DirectionProperty = CamelCaseNamingConvention.Instance.Apply(nameof(PortPinInfo.Direction));
    static readonly ISerializer DefaultSerializer = new Serializer();

    public IDeserializer Deserializer { get; } = deserializer ?? throw new ArgumentNullException(nameof(deserializer));

    public bool Accepts(Type type)
    {
        return type == typeof(PortPinInfo);
    }

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer rootDeserializer)
    {
        var portPin = Deserializer.Deserialize<Dictionary<object, object>>(parser);
        if (portPin.TryGetValue(DirectionProperty, out object value))
        {
            var pinDirection = PascalCaseNamingConvention.Instance.Apply((string)value);
            Type portPinType = pinDirection switch
            {
                nameof(PinDirection.Input) => typeof(InputPinInfo),
                nameof(PinDirection.Output) => typeof(OutputPinInfo),
                _ => throw new YamlException($"Invalid value for '{DirectionProperty}' when deserializing type '{typeof(PortPinInfo)}'"),
            };
            var yaml = DefaultSerializer.Serialize(portPin);
            return Deserializer.Deserialize(yaml, portPinType);
        }

        throw new YamlException($"Required property '{DirectionProperty}' not found when deserializing type '{typeof(PortPinInfo)}'.");
    }

    public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer serializer)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Translates property names into screaming snake case following the current
/// ATxmega register naming convention.
/// </summary>
public sealed class FirmwareNamingConvention : INamingConvention
{
    private const char Separator = '_';

    /// <summary>
    /// Gets a singleton instance of the <see cref="FirmwareNamingConvention"/> class.
    /// </summary>
    public static readonly FirmwareNamingConvention Instance = new();

    /// <summary>
    /// Forward converts a property name from camel case into screaming snake case. 
    /// </summary>
    /// <param name="value">A property name in camel case.</param>
    /// <returns>The corresponding property name in screaming snake case.</returns>
    public string Apply(string value)
    {
        var startIndex = 0;
        while (startIndex < value.Length && (char.IsUpper(value[startIndex]) || !char.IsLetter(value[startIndex])))
        {
            if (startIndex > 1 && (startIndex + 1) < value.Length &&
                char.IsLetter(value[startIndex + 1]) && char.IsLower(value[startIndex + 1]))
                break;
            startIndex++;
        }

        value = value.Substring(0, startIndex).ToLowerInvariant() + value.Substring(startIndex);
        var previousMatch = 0;
        var previousLength = 0;
        value = Regex.Replace(value, "(?<sep>[_\\-])?(?<char>[A-Z])", match =>
        {
            var length = match.Index - previousMatch;
            previousMatch = match.Index;
            var character = match.Groups["char"].Value.ToLowerInvariant();
            var separate = length != 1 || (match.Index + 1) < value.Length && char.IsLower(value[match.Index + 1]);
            previousLength = length;
            return separate ? Separator + character : character;
        });
        return value.ToUpperInvariant();
    }

    /// <summary>
    /// Backward converts a property name from screaming snake case into camel case. 
    /// </summary>
    /// <param name="value">A property name in screaming snake case.</param>
    /// <returns>The corresponding property name in camel case.</returns>
    public string Reverse(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var words = Array.ConvertAll(
            value.Split(Separator),
            word => PascalCaseNamingConvention.Instance.Apply(word.ToLowerInvariant()));
        return string.Concat(words);
    }
}

class FirmwareGroupMaskNamingConvention : INamingConvention
{
    public static readonly FirmwareGroupMaskNamingConvention Instance = new();
    static readonly string[] TrimSuffixes = ["StateConfiguration", "Configuration"];
    static readonly Dictionary<string, string> TokenSubstitutions = new()
    {
        { "DIGITAL_INPUT", "DI" },
        { "DIGITAL_OUTPUT", "DO" }
    };

    public string Apply(string value)
    {
        for (int i = 0; i < TrimSuffixes.Length; i++)
        {
            var trimIndex = value.LastIndexOf(TrimSuffixes[i]);
            if (trimIndex >= 0)
            {
                value = value.Substring(0, trimIndex);
                break;
            }
        }

        var result = FirmwareNamingConvention.Instance.Apply(value);
        foreach (var substitution in TokenSubstitutions)
        {
            result = result.Replace(substitution.Key, substitution.Value);
        }
        return result;
    }

    public string Reverse(string value)
    {
        throw new NotImplementedException();
    }
}
