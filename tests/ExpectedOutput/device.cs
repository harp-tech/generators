using Bonsai;
using Bonsai.Harp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Xml.Serialization;

namespace Harp.Generators.Tests
{
    /// <summary>
    /// Generates events and processes commands for the Tests device connected
    /// at the specified serial port.
    /// </summary>
    [Combinator(MethodName = nameof(Generate))]
    [WorkflowElementCategory(ElementCategory.Source)]
    [Description("Generates events and processes commands for the Tests device.")]
    public partial class Device : Bonsai.Harp.Device, INamedElement
    {
        /// <summary>
        /// Represents the unique identity class of the <see cref="Tests"/> device.
        /// This field is constant.
        /// </summary>
        public const int WhoAmI = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="Device"/> class.
        /// </summary>
        public Device() : base(WhoAmI) { }

        string INamedElement.Name => nameof(Tests);

        /// <summary>
        /// Gets a read-only mapping from address to register type.
        /// </summary>
        public static new IReadOnlyDictionary<int, Type> RegisterMap { get; } = new Dictionary<int, Type>
            (Bonsai.Harp.Device.RegisterMap.ToDictionary(entry => entry.Key, entry => entry.Value))
        {
            { 32, typeof(DigitalInputs) },
            { 33, typeof(AnalogData) },
            { 34, typeof(ComplexConfiguration) },
            { 35, typeof(Version) },
            { 36, typeof(CustomPayload) },
            { 37, typeof(CustomRawPayload) },
            { 38, typeof(CustomMemberConverter) },
            { 39, typeof(BitmaskSplitter) },
            { 40, typeof(Counter0) },
            { 41, typeof(PortDIOSet) },
            { 42, typeof(PulseDOPort0) },
            { 43, typeof(PulseDO0) },
            { 37, typeof(StartPulse) },
            { 38, typeof(StartPulseTrain) }
        };

        /// <summary>
        /// Gets the contents of the metadata file describing the <see cref="Tests"/>
        /// device registers.
        /// </summary>
        public static readonly string Metadata = GetDeviceMetadata();

        static string GetDeviceMetadata()
        {
            var deviceType = typeof(Device);
            using var metadataStream = deviceType.Assembly.GetManifestResourceStream($"{deviceType.Namespace}.device.yml");
            using var streamReader = new System.IO.StreamReader(metadataStream);
            return streamReader.ReadToEnd();
        }
    }

    /// <summary>
    /// Represents an operator that returns the contents of the metadata file
    /// describing the <see cref="Tests"/> device registers.
    /// </summary>
    [Description("Returns the contents of the metadata file describing the Tests device registers.")]
    public partial class GetDeviceMetadata : Source<string>
    {
        /// <summary>
        /// Returns an observable sequence with the contents of the metadata file
        /// describing the <see cref="Tests"/> device registers.
        /// </summary>
        /// <returns>
        /// A sequence with a single <see cref="string"/> object representing the
        /// contents of the metadata file.
        /// </returns>
        public override IObservable<string> Generate()
        {
            return Observable.Return(Device.Metadata);
        }
    }

    /// <summary>
    /// Represents an operator that groups the sequence of <see cref="Tests"/>" messages by register type.
    /// </summary>
    [Description("Groups the sequence of Tests messages by register type.")]
    public partial class GroupByRegister : Combinator<HarpMessage, IGroupedObservable<Type, HarpMessage>>
    {
        /// <summary>
        /// Groups an observable sequence of <see cref="Tests"/> messages
        /// by register type.
        /// </summary>
        /// <param name="source">The sequence of Harp device messages.</param>
        /// <returns>
        /// A sequence of observable groups, each of which corresponds to a unique
        /// <see cref="Tests"/> register.
        /// </returns>
        public override IObservable<IGroupedObservable<Type, HarpMessage>> Process(IObservable<HarpMessage> source)
        {
            return source.GroupBy(message => Device.RegisterMap[message.Address]);
        }
    }

    /// <summary>
    /// Represents an operator that writes the sequence of <see cref="Tests"/>" messages
    /// to the standard Harp storage format.
    /// </summary>
    [DefaultProperty(nameof(Path))]
    [Description("Writes the sequence of Tests messages to the standard Harp storage format.")]
    public partial class DeviceDataWriter : Sink<HarpMessage>, INamedElement
    {
        const string BinaryExtension = ".bin";
        const string MetadataFileName = "device.yml";
        readonly Bonsai.Harp.MessageWriter writer = new();

        string INamedElement.Name => nameof(Tests) + "DataWriter";

        /// <summary>
        /// Gets or sets the relative or absolute path on which to save the message data.
        /// </summary>
        [Description("The relative or absolute path of the directory on which to save the message data.")]
        [Editor("Bonsai.Design.SaveFileNameEditor, Bonsai.Design", DesignTypes.UITypeEditor)]
        public string Path
        {
            get => System.IO.Path.GetDirectoryName(writer.FileName);
            set => writer.FileName = System.IO.Path.Combine(value, nameof(Tests) + BinaryExtension);
        }

        /// <summary>
        /// Gets or sets a value indicating whether element writing should be buffered. If <see langword="true"/>,
        /// the write commands will be queued in memory as fast as possible and will be processed
        /// by the writer in a different thread. Otherwise, writing will be done in the same
        /// thread in which notifications arrive.
        /// </summary>
        [Description("Indicates whether writing should be buffered.")]
        public bool Buffered
        {
            get => writer.Buffered;
            set => writer.Buffered = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to overwrite the output file if it already exists.
        /// </summary>
        [Description("Indicates whether to overwrite the output file if it already exists.")]
        public bool Overwrite
        {
            get => writer.Overwrite;
            set => writer.Overwrite = value;
        }

        /// <summary>
        /// Gets or sets a value specifying how the message filter will use the matching criteria.
        /// </summary>
        [Description("Specifies how the message filter will use the matching criteria.")]
        public FilterType FilterType
        {
            get => writer.FilterType;
            set => writer.FilterType = value;
        }

        /// <summary>
        /// Gets or sets a value specifying the expected message type. If no value is
        /// specified, all messages will be accepted.
        /// </summary>
        [Description("Specifies the expected message type. If no value is specified, all messages will be accepted.")]
        public MessageType? MessageType
        {
            get => writer.MessageType;
            set => writer.MessageType = value;
        }

        private IObservable<TSource> WriteDeviceMetadata<TSource>(IObservable<TSource> source)
        {
            var basePath = Path;
            if (string.IsNullOrEmpty(basePath))
                return source;

            var metadataPath = System.IO.Path.Combine(basePath, MetadataFileName);
            return Observable.Create<TSource>(observer =>
            {
                Bonsai.IO.PathHelper.EnsureDirectory(metadataPath);
                if (System.IO.File.Exists(metadataPath) && !Overwrite)
                {
                    throw new System.IO.IOException(string.Format("The file '{0}' already exists.", metadataPath));
                }

                System.IO.File.WriteAllText(metadataPath, Device.Metadata);
                return source.SubscribeSafe(observer);
            });
        }

        /// <summary>
        /// Writes each Harp message in the sequence to the specified binary file, and the
        /// contents of the device metadata file to a separate text file.
        /// </summary>
        /// <param name="source">The sequence of messages to write to the file.</param>
        /// <returns>
        /// An observable sequence that is identical to the <paramref name="source"/>
        /// sequence but where there is an additional side effect of writing the
        /// messages to a raw binary file, and the contents of the device metadata file
        /// to a separate text file.
        /// </returns>
        public override IObservable<HarpMessage> Process(IObservable<HarpMessage> source)
        {
            return source.Publish(ps => ps.Merge(
                WriteDeviceMetadata(writer.Process(ps.GroupBy(message => message.Address)))
                .IgnoreElements()
                .Cast<HarpMessage>()));
        }

        /// <summary>
        /// Writes each Harp message in the sequence of observable groups to the
        /// corresponding binary file, where the name of each file is generated from
        /// the common group register address. The contents of the device metadata file are
        /// written to a separate text file.
        /// </summary>
        /// <param name="source">
        /// A sequence of observable groups, each of which corresponds to a unique register
        /// address.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the <paramref name="source"/>
        /// sequence but where there is an additional side effect of writing the Harp
        /// messages in each group to the corresponding file, and the contents of the device
        /// metadata file to a separate text file.
        /// </returns>
        public IObservable<IGroupedObservable<int, HarpMessage>> Process(IObservable<IGroupedObservable<int, HarpMessage>> source)
        {
            return WriteDeviceMetadata(writer.Process(source));
        }

        /// <summary>
        /// Writes each Harp message in the sequence of observable groups to the
        /// corresponding binary file, where the name of each file is generated from
        /// the common group register name. The contents of the device metadata file are
        /// written to a separate text file.
        /// </summary>
        /// <param name="source">
        /// A sequence of observable groups, each of which corresponds to a unique register
        /// type.
        /// </param>
        /// <returns>
        /// An observable sequence that is identical to the <paramref name="source"/>
        /// sequence but where there is an additional side effect of writing the Harp
        /// messages in each group to the corresponding file, and the contents of the device
        /// metadata file to a separate text file.
        /// </returns>
        public IObservable<IGroupedObservable<Type, HarpMessage>> Process(IObservable<IGroupedObservable<Type, HarpMessage>> source)
        {
            return WriteDeviceMetadata(writer.Process(source));
        }
    }

    /// <summary>
    /// Represents an operator that filters register-specific messages
    /// reported by the <see cref="Tests"/> device.
    /// </summary>
    /// <seealso cref="DigitalInputs"/>
    /// <seealso cref="AnalogData"/>
    /// <seealso cref="ComplexConfiguration"/>
    /// <seealso cref="Version"/>
    /// <seealso cref="CustomPayload"/>
    /// <seealso cref="CustomRawPayload"/>
    /// <seealso cref="CustomMemberConverter"/>
    /// <seealso cref="BitmaskSplitter"/>
    /// <seealso cref="Counter0"/>
    /// <seealso cref="PortDIOSet"/>
    /// <seealso cref="PulseDOPort0"/>
    /// <seealso cref="PulseDO0"/>
    /// <seealso cref="StartPulse"/>
    /// <seealso cref="StartPulseTrain"/>
    [XmlInclude(typeof(DigitalInputs))]
    [XmlInclude(typeof(AnalogData))]
    [XmlInclude(typeof(ComplexConfiguration))]
    [XmlInclude(typeof(Version))]
    [XmlInclude(typeof(CustomPayload))]
    [XmlInclude(typeof(CustomRawPayload))]
    [XmlInclude(typeof(CustomMemberConverter))]
    [XmlInclude(typeof(BitmaskSplitter))]
    [XmlInclude(typeof(Counter0))]
    [XmlInclude(typeof(PortDIOSet))]
    [XmlInclude(typeof(PulseDOPort0))]
    [XmlInclude(typeof(PulseDO0))]
    [XmlInclude(typeof(StartPulse))]
    [XmlInclude(typeof(StartPulseTrain))]
    [Description("Filters register-specific messages reported by the Tests device.")]
    public class FilterRegister : FilterRegisterBuilder, INamedElement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FilterRegister"/> class.
        /// </summary>
        public FilterRegister()
        {
            Register = new DigitalInputs();
        }

        string INamedElement.Name
        {
            get => $"{nameof(Tests)}.{GetElementDisplayName(Register)}";
        }
    }

    /// <summary>
    /// Represents an operator which filters and selects specific messages
    /// reported by the Tests device.
    /// </summary>
    /// <seealso cref="DigitalInputs"/>
    /// <seealso cref="AnalogData"/>
    /// <seealso cref="ComplexConfiguration"/>
    /// <seealso cref="Version"/>
    /// <seealso cref="CustomPayload"/>
    /// <seealso cref="CustomRawPayload"/>
    /// <seealso cref="CustomMemberConverter"/>
    /// <seealso cref="BitmaskSplitter"/>
    /// <seealso cref="Counter0"/>
    /// <seealso cref="PortDIOSet"/>
    /// <seealso cref="PulseDOPort0"/>
    /// <seealso cref="PulseDO0"/>
    /// <seealso cref="StartPulse"/>
    /// <seealso cref="StartPulseTrain"/>
    [XmlInclude(typeof(DigitalInputs))]
    [XmlInclude(typeof(AnalogData))]
    [XmlInclude(typeof(ComplexConfiguration))]
    [XmlInclude(typeof(Version))]
    [XmlInclude(typeof(CustomPayload))]
    [XmlInclude(typeof(CustomRawPayload))]
    [XmlInclude(typeof(CustomMemberConverter))]
    [XmlInclude(typeof(BitmaskSplitter))]
    [XmlInclude(typeof(Counter0))]
    [XmlInclude(typeof(PortDIOSet))]
    [XmlInclude(typeof(PulseDOPort0))]
    [XmlInclude(typeof(PulseDO0))]
    [XmlInclude(typeof(StartPulse))]
    [XmlInclude(typeof(StartPulseTrain))]
    [XmlInclude(typeof(TimestampedDigitalInputs))]
    [XmlInclude(typeof(TimestampedAnalogData))]
    [XmlInclude(typeof(TimestampedComplexConfiguration))]
    [XmlInclude(typeof(TimestampedVersion))]
    [XmlInclude(typeof(TimestampedCustomPayload))]
    [XmlInclude(typeof(TimestampedCustomRawPayload))]
    [XmlInclude(typeof(TimestampedCustomMemberConverter))]
    [XmlInclude(typeof(TimestampedBitmaskSplitter))]
    [XmlInclude(typeof(TimestampedCounter0))]
    [XmlInclude(typeof(TimestampedPortDIOSet))]
    [XmlInclude(typeof(TimestampedPulseDOPort0))]
    [XmlInclude(typeof(TimestampedPulseDO0))]
    [XmlInclude(typeof(TimestampedStartPulse))]
    [XmlInclude(typeof(TimestampedStartPulseTrain))]
    [Description("Filters and selects specific messages reported by the Tests device.")]
    public partial class Parse : ParseBuilder, INamedElement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Parse"/> class.
        /// </summary>
        public Parse()
        {
            Register = new DigitalInputs();
        }

        string INamedElement.Name => $"{nameof(Tests)}.{GetElementDisplayName(Register)}";
    }

    /// <summary>
    /// Represents an operator which formats a sequence of values as specific
    /// Tests register messages.
    /// </summary>
    /// <seealso cref="DigitalInputs"/>
    /// <seealso cref="AnalogData"/>
    /// <seealso cref="ComplexConfiguration"/>
    /// <seealso cref="Version"/>
    /// <seealso cref="CustomPayload"/>
    /// <seealso cref="CustomRawPayload"/>
    /// <seealso cref="CustomMemberConverter"/>
    /// <seealso cref="BitmaskSplitter"/>
    /// <seealso cref="Counter0"/>
    /// <seealso cref="PortDIOSet"/>
    /// <seealso cref="PulseDOPort0"/>
    /// <seealso cref="PulseDO0"/>
    /// <seealso cref="StartPulse"/>
    /// <seealso cref="StartPulseTrain"/>
    [XmlInclude(typeof(DigitalInputs))]
    [XmlInclude(typeof(AnalogData))]
    [XmlInclude(typeof(ComplexConfiguration))]
    [XmlInclude(typeof(Version))]
    [XmlInclude(typeof(CustomPayload))]
    [XmlInclude(typeof(CustomRawPayload))]
    [XmlInclude(typeof(CustomMemberConverter))]
    [XmlInclude(typeof(BitmaskSplitter))]
    [XmlInclude(typeof(Counter0))]
    [XmlInclude(typeof(PortDIOSet))]
    [XmlInclude(typeof(PulseDOPort0))]
    [XmlInclude(typeof(PulseDO0))]
    [XmlInclude(typeof(StartPulse))]
    [XmlInclude(typeof(StartPulseTrain))]
    [Description("Formats a sequence of values as specific Tests register messages.")]
    public partial class Format : FormatBuilder, INamedElement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Format"/> class.
        /// </summary>
        public Format()
        {
            Register = new DigitalInputs();
        }

        string INamedElement.Name => $"{nameof(Tests)}.{GetElementDisplayName(Register)}";
    }

    /// <summary>
    /// Represents a register that manipulates messages from register DigitalInputs.
    /// </summary>
    [Description("")]
    public partial class DigitalInputs
    {
        /// <summary>
        /// Represents the address of the <see cref="DigitalInputs"/> register. This field is constant.
        /// </summary>
        public const int Address = 32;

        /// <summary>
        /// Represents the payload type of the <see cref="DigitalInputs"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U8;

        /// <summary>
        /// Represents the length of the <see cref="DigitalInputs"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 1;

        /// <summary>
        /// Returns the payload data for <see cref="DigitalInputs"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static byte GetPayload(HarpMessage message)
        {
            return message.GetPayloadByte();
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="DigitalInputs"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<byte> GetTimestampedPayload(HarpMessage message)
        {
            return message.GetTimestampedPayloadByte();
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="DigitalInputs"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="DigitalInputs"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, byte value)
        {
            return HarpMessage.FromByte(Address, messageType, value);
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="DigitalInputs"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="DigitalInputs"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, byte value)
        {
            return HarpMessage.FromByte(Address, timestamp, messageType, value);
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// DigitalInputs register.
    /// </summary>
    /// <seealso cref="DigitalInputs"/>
    [Description("Filters and selects timestamped messages from the DigitalInputs register.")]
    public partial class TimestampedDigitalInputs
    {
        /// <summary>
        /// Represents the address of the <see cref="DigitalInputs"/> register. This field is constant.
        /// </summary>
        public const int Address = DigitalInputs.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="DigitalInputs"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<byte> GetPayload(HarpMessage message)
        {
            return DigitalInputs.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that manipulates messages from register AnalogData.
    /// </summary>
    [Description("")]
    public partial class AnalogData
    {
        /// <summary>
        /// Represents the address of the <see cref="AnalogData"/> register. This field is constant.
        /// </summary>
        public const int Address = 33;

        /// <summary>
        /// Represents the payload type of the <see cref="AnalogData"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.Float;

        /// <summary>
        /// Represents the length of the <see cref="AnalogData"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 6;

        static AnalogDataPayload ParsePayload(float[] payload)
        {
            AnalogDataPayload result;
            result.Analog0 = payload[0];
            result.Analog1 = payload[1];
            result.Analog2 = payload[2];
            result.Accelerometer = PayloadMarshal.GetSubArray(payload, 3, 3);
            return result;
        }

        static float[] FormatPayload(AnalogDataPayload value)
        {
            float[] result;
            result = new float[6];
            result[0] = value.Analog0;
            result[1] = value.Analog1;
            result[2] = value.Analog2;
            PayloadMarshal.Write(new ArraySegment<float>(result, 3, 3), value.Accelerometer);
            return result;
        }

        /// <summary>
        /// Returns the payload data for <see cref="AnalogData"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static AnalogDataPayload GetPayload(HarpMessage message)
        {
            return ParsePayload(message.GetPayloadArray<float>());
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="AnalogData"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<AnalogDataPayload> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadArray<float>();
            return Timestamped.Create(ParsePayload(payload.Value), payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="AnalogData"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="AnalogData"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, AnalogDataPayload value)
        {
            return HarpMessage.FromSingle(Address, messageType, FormatPayload(value));
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="AnalogData"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="AnalogData"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, AnalogDataPayload value)
        {
            return HarpMessage.FromSingle(Address, timestamp, messageType, FormatPayload(value));
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// AnalogData register.
    /// </summary>
    /// <seealso cref="AnalogData"/>
    [Description("Filters and selects timestamped messages from the AnalogData register.")]
    public partial class TimestampedAnalogData
    {
        /// <summary>
        /// Represents the address of the <see cref="AnalogData"/> register. This field is constant.
        /// </summary>
        public const int Address = AnalogData.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="AnalogData"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<AnalogDataPayload> GetPayload(HarpMessage message)
        {
            return AnalogData.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that manipulates messages from register ComplexConfiguration.
    /// </summary>
    [Description("")]
    public partial class ComplexConfiguration
    {
        /// <summary>
        /// Represents the address of the <see cref="ComplexConfiguration"/> register. This field is constant.
        /// </summary>
        public const int Address = 34;

        /// <summary>
        /// Represents the payload type of the <see cref="ComplexConfiguration"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U8;

        /// <summary>
        /// Represents the length of the <see cref="ComplexConfiguration"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 17;

        static ComplexConfigurationPayload ParsePayload(byte[] payload)
        {
            ComplexConfigurationPayload result;
            result.PwmPort = (PwmPort)payload[0];
            result.DutyCycle = PayloadMarshal.ReadSingle(new ArraySegment<byte>(payload, 4, 4));
            result.Frequency = PayloadMarshal.ReadSingle(new ArraySegment<byte>(payload, 8, 4));
            result.EventsEnabled = payload[12] != 0;
            result.Delta = PayloadMarshal.ReadUInt32(new ArraySegment<byte>(payload, 13, 4));
            return result;
        }

        static byte[] FormatPayload(ComplexConfigurationPayload value)
        {
            byte[] result;
            result = new byte[17];
            result[0] = (byte)value.PwmPort;
            PayloadMarshal.Write(new ArraySegment<byte>(result, 4, 4), value.DutyCycle);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 8, 4), value.Frequency);
            result[12] = (byte)(value.EventsEnabled ? 1 : 0);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 13, 4), value.Delta);
            return result;
        }

        /// <summary>
        /// Returns the payload data for <see cref="ComplexConfiguration"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static ComplexConfigurationPayload GetPayload(HarpMessage message)
        {
            return ParsePayload(message.GetPayloadArray<byte>());
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="ComplexConfiguration"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<ComplexConfigurationPayload> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadArray<byte>();
            return Timestamped.Create(ParsePayload(payload.Value), payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="ComplexConfiguration"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="ComplexConfiguration"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, ComplexConfigurationPayload value)
        {
            return HarpMessage.FromByte(Address, messageType, FormatPayload(value));
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="ComplexConfiguration"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="ComplexConfiguration"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, ComplexConfigurationPayload value)
        {
            return HarpMessage.FromByte(Address, timestamp, messageType, FormatPayload(value));
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// ComplexConfiguration register.
    /// </summary>
    /// <seealso cref="ComplexConfiguration"/>
    [Description("Filters and selects timestamped messages from the ComplexConfiguration register.")]
    public partial class TimestampedComplexConfiguration
    {
        /// <summary>
        /// Represents the address of the <see cref="ComplexConfiguration"/> register. This field is constant.
        /// </summary>
        public const int Address = ComplexConfiguration.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="ComplexConfiguration"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<ComplexConfigurationPayload> GetPayload(HarpMessage message)
        {
            return ComplexConfiguration.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that manipulates messages from register Version.
    /// </summary>
    [Description("")]
    public partial class Version
    {
        /// <summary>
        /// Represents the address of the <see cref="Version"/> register. This field is constant.
        /// </summary>
        public const int Address = 35;

        /// <summary>
        /// Represents the payload type of the <see cref="Version"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U8;

        /// <summary>
        /// Represents the length of the <see cref="Version"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 32;

        static VersionPayload ParsePayload(byte[] payload)
        {
            VersionPayload result;
            result.ProtocolVersion = PayloadMarshal.ReadHarpVersion(new ArraySegment<byte>(payload, 0, 3));
            result.FirmwareVersion = PayloadMarshal.ReadHarpVersion(new ArraySegment<byte>(payload, 3, 3));
            result.HardwareVersion = PayloadMarshal.ReadHarpVersion(new ArraySegment<byte>(payload, 6, 3));
            result.CoreId = PayloadMarshal.ReadUtf8String(new ArraySegment<byte>(payload, 9, 3));
            result.InterfaceHash = PayloadMarshal.GetSubArray(payload, 12, 20);
            return result;
        }

        static byte[] FormatPayload(VersionPayload value)
        {
            byte[] result;
            result = new byte[32];
            PayloadMarshal.Write(new ArraySegment<byte>(result, 0, 3), value.ProtocolVersion);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 3, 3), value.FirmwareVersion);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 6, 3), value.HardwareVersion);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 9, 3), value.CoreId);
            PayloadMarshal.Write(new ArraySegment<byte>(result, 12, 20), value.InterfaceHash);
            return result;
        }

        /// <summary>
        /// Returns the payload data for <see cref="Version"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static VersionPayload GetPayload(HarpMessage message)
        {
            return ParsePayload(message.GetPayloadArray<byte>());
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="Version"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<VersionPayload> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadArray<byte>();
            return Timestamped.Create(ParsePayload(payload.Value), payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="Version"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="Version"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, VersionPayload value)
        {
            return HarpMessage.FromByte(Address, messageType, FormatPayload(value));
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="Version"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="Version"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, VersionPayload value)
        {
            return HarpMessage.FromByte(Address, timestamp, messageType, FormatPayload(value));
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// Version register.
    /// </summary>
    /// <seealso cref="Version"/>
    [Description("Filters and selects timestamped messages from the Version register.")]
    public partial class TimestampedVersion
    {
        /// <summary>
        /// Represents the address of the <see cref="Version"/> register. This field is constant.
        /// </summary>
        public const int Address = Version.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="Version"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<VersionPayload> GetPayload(HarpMessage message)
        {
            return Version.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that manipulates messages from register CustomPayload.
    /// </summary>
    [Description("")]
    public partial class CustomPayload
    {
        /// <summary>
        /// Represents the address of the <see cref="CustomPayload"/> register. This field is constant.
        /// </summary>
        public const int Address = 36;

        /// <summary>
        /// Represents the payload type of the <see cref="CustomPayload"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U32;

        /// <summary>
        /// Represents the length of the <see cref="CustomPayload"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 3;

        private static partial HarpVersion ParsePayload(uint[] payload);

        private static partial uint[] FormatPayload(HarpVersion value);

        /// <summary>
        /// Returns the payload data for <see cref="CustomPayload"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static HarpVersion GetPayload(HarpMessage message)
        {
            return ParsePayload(message.GetPayloadArray<uint>());
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="CustomPayload"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<HarpVersion> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadArray<uint>();
            return Timestamped.Create(ParsePayload(payload.Value), payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="CustomPayload"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="CustomPayload"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, HarpVersion value)
        {
            return HarpMessage.FromUInt32(Address, messageType, FormatPayload(value));
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="CustomPayload"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="CustomPayload"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, HarpVersion value)
        {
            return HarpMessage.FromUInt32(Address, timestamp, messageType, FormatPayload(value));
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// CustomPayload register.
    /// </summary>
    /// <seealso cref="CustomPayload"/>
    [Description("Filters and selects timestamped messages from the CustomPayload register.")]
    public partial class TimestampedCustomPayload
    {
        /// <summary>
        /// Represents the address of the <see cref="CustomPayload"/> register. This field is constant.
        /// </summary>
        public const int Address = CustomPayload.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="CustomPayload"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<HarpVersion> GetPayload(HarpMessage message)
        {
            return CustomPayload.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that manipulates messages from register CustomRawPayload.
    /// </summary>
    [Description("")]
    public partial class CustomRawPayload
    {
        /// <summary>
        /// Represents the address of the <see cref="CustomRawPayload"/> register. This field is constant.
        /// </summary>
        public const int Address = 37;

        /// <summary>
        /// Represents the payload type of the <see cref="CustomRawPayload"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U32;

        /// <summary>
        /// Represents the length of the <see cref="CustomRawPayload"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 3;

        private static partial HarpVersion ParsePayload(ArraySegment<byte> payload);

        private static partial ArraySegment<byte> FormatPayload(HarpVersion value);

        /// <summary>
        /// Returns the payload data for <see cref="CustomRawPayload"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static HarpVersion GetPayload(HarpMessage message)
        {
            return ParsePayload(message.GetPayload());
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="CustomRawPayload"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<HarpVersion> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayload();
            return Timestamped.Create(ParsePayload(payload.Value), payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="CustomRawPayload"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="CustomRawPayload"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, HarpVersion value)
        {
            return HarpMessage.FromPayload(Address, messageType, PayloadType.U32, FormatPayload(value));
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="CustomRawPayload"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="CustomRawPayload"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, HarpVersion value)
        {
            return HarpMessage.FromPayload(Address, timestamp, messageType, PayloadType.U32, FormatPayload(value));
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// CustomRawPayload register.
    /// </summary>
    /// <seealso cref="CustomRawPayload"/>
    [Description("Filters and selects timestamped messages from the CustomRawPayload register.")]
    public partial class TimestampedCustomRawPayload
    {
        /// <summary>
        /// Represents the address of the <see cref="CustomRawPayload"/> register. This field is constant.
        /// </summary>
        public const int Address = CustomRawPayload.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="CustomRawPayload"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<HarpVersion> GetPayload(HarpMessage message)
        {
            return CustomRawPayload.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that manipulates messages from register CustomMemberConverter.
    /// </summary>
    [Description("")]
    public partial class CustomMemberConverter
    {
        /// <summary>
        /// Represents the address of the <see cref="CustomMemberConverter"/> register. This field is constant.
        /// </summary>
        public const int Address = 38;

        /// <summary>
        /// Represents the payload type of the <see cref="CustomMemberConverter"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U8;

        /// <summary>
        /// Represents the length of the <see cref="CustomMemberConverter"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 3;

        private static partial int ParsePayloadData(ArraySegment<byte> payloadData);

        static CustomMemberConverterPayload ParsePayload(byte[] payload)
        {
            CustomMemberConverterPayload result;
            result.Header = payload[0];
            result.Data = ParsePayloadData(new ArraySegment<byte>(payload, 1, 2));
            return result;
        }

        private static partial byte[] FormatPayloadData(int data);

        static byte[] FormatPayload(CustomMemberConverterPayload value)
        {
            byte[] result;
            result = new byte[3];
            result[0] = value.Header;
            PayloadMarshal.Write(new ArraySegment<byte>(result, 1, 2), FormatPayloadData(value.Data));
            return result;
        }

        /// <summary>
        /// Returns the payload data for <see cref="CustomMemberConverter"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static CustomMemberConverterPayload GetPayload(HarpMessage message)
        {
            return ParsePayload(message.GetPayloadArray<byte>());
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="CustomMemberConverter"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<CustomMemberConverterPayload> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadArray<byte>();
            return Timestamped.Create(ParsePayload(payload.Value), payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="CustomMemberConverter"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="CustomMemberConverter"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, CustomMemberConverterPayload value)
        {
            return HarpMessage.FromByte(Address, messageType, FormatPayload(value));
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="CustomMemberConverter"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="CustomMemberConverter"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, CustomMemberConverterPayload value)
        {
            return HarpMessage.FromByte(Address, timestamp, messageType, FormatPayload(value));
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// CustomMemberConverter register.
    /// </summary>
    /// <seealso cref="CustomMemberConverter"/>
    [Description("Filters and selects timestamped messages from the CustomMemberConverter register.")]
    public partial class TimestampedCustomMemberConverter
    {
        /// <summary>
        /// Represents the address of the <see cref="CustomMemberConverter"/> register. This field is constant.
        /// </summary>
        public const int Address = CustomMemberConverter.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="CustomMemberConverter"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<CustomMemberConverterPayload> GetPayload(HarpMessage message)
        {
            return CustomMemberConverter.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that manipulates messages from register BitmaskSplitter.
    /// </summary>
    [Description("")]
    public partial class BitmaskSplitter
    {
        /// <summary>
        /// Represents the address of the <see cref="BitmaskSplitter"/> register. This field is constant.
        /// </summary>
        public const int Address = 39;

        /// <summary>
        /// Represents the payload type of the <see cref="BitmaskSplitter"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U8;

        /// <summary>
        /// Represents the length of the <see cref="BitmaskSplitter"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 1;

        private static partial int ParsePayloadLow(byte payloadLow);

        static BitmaskSplitterPayload ParsePayload(byte payload)
        {
            BitmaskSplitterPayload result;
            result.Low = ParsePayloadLow((byte)(payload & 0xF));
            result.High = (int)(byte)((payload & 0xF0) >> 4);
            return result;
        }

        private static partial byte FormatPayloadLow(int low);

        static byte FormatPayload(BitmaskSplitterPayload value)
        {
            byte result;
            result = (byte)((byte)FormatPayloadLow(value.Low) & 0xF);
            result |= (byte)(((byte)value.High << 4) & 0xF0);
            return result;
        }

        /// <summary>
        /// Returns the payload data for <see cref="BitmaskSplitter"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static BitmaskSplitterPayload GetPayload(HarpMessage message)
        {
            return ParsePayload(message.GetPayloadByte());
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="BitmaskSplitter"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<BitmaskSplitterPayload> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadByte();
            return Timestamped.Create(ParsePayload(payload.Value), payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="BitmaskSplitter"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="BitmaskSplitter"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, BitmaskSplitterPayload value)
        {
            return HarpMessage.FromByte(Address, messageType, FormatPayload(value));
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="BitmaskSplitter"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="BitmaskSplitter"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, BitmaskSplitterPayload value)
        {
            return HarpMessage.FromByte(Address, timestamp, messageType, FormatPayload(value));
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// BitmaskSplitter register.
    /// </summary>
    /// <seealso cref="BitmaskSplitter"/>
    [Description("Filters and selects timestamped messages from the BitmaskSplitter register.")]
    public partial class TimestampedBitmaskSplitter
    {
        /// <summary>
        /// Represents the address of the <see cref="BitmaskSplitter"/> register. This field is constant.
        /// </summary>
        public const int Address = BitmaskSplitter.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="BitmaskSplitter"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<BitmaskSplitterPayload> GetPayload(HarpMessage message)
        {
            return BitmaskSplitter.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that manipulates messages from register Counter0.
    /// </summary>
    [Description("")]
    public partial class Counter0
    {
        /// <summary>
        /// Represents the address of the <see cref="Counter0"/> register. This field is constant.
        /// </summary>
        public const int Address = 40;

        /// <summary>
        /// Represents the payload type of the <see cref="Counter0"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.S32;

        /// <summary>
        /// Represents the length of the <see cref="Counter0"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 1;

        /// <summary>
        /// Returns the payload data for <see cref="Counter0"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static int GetPayload(HarpMessage message)
        {
            return message.GetPayloadInt32();
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="Counter0"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<int> GetTimestampedPayload(HarpMessage message)
        {
            return message.GetTimestampedPayloadInt32();
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="Counter0"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="Counter0"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, int value)
        {
            return HarpMessage.FromInt32(Address, messageType, value);
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="Counter0"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="Counter0"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, int value)
        {
            return HarpMessage.FromInt32(Address, timestamp, messageType, value);
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// Counter0 register.
    /// </summary>
    /// <seealso cref="Counter0"/>
    [Description("Filters and selects timestamped messages from the Counter0 register.")]
    public partial class TimestampedCounter0
    {
        /// <summary>
        /// Represents the address of the <see cref="Counter0"/> register. This field is constant.
        /// </summary>
        public const int Address = Counter0.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="Counter0"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<int> GetPayload(HarpMessage message)
        {
            return Counter0.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that manipulates messages from register PortDIOSet.
    /// </summary>
    [Description("")]
    public partial class PortDIOSet
    {
        /// <summary>
        /// Represents the address of the <see cref="PortDIOSet"/> register. This field is constant.
        /// </summary>
        public const int Address = 41;

        /// <summary>
        /// Represents the payload type of the <see cref="PortDIOSet"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U8;

        /// <summary>
        /// Represents the length of the <see cref="PortDIOSet"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 1;

        /// <summary>
        /// Returns the payload data for <see cref="PortDIOSet"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static PortDigitalIOS GetPayload(HarpMessage message)
        {
            return (PortDigitalIOS)message.GetPayloadByte();
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="PortDIOSet"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<PortDigitalIOS> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadByte();
            return Timestamped.Create((PortDigitalIOS)payload.Value, payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="PortDIOSet"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="PortDIOSet"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, PortDigitalIOS value)
        {
            return HarpMessage.FromByte(Address, messageType, (byte)value);
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="PortDIOSet"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="PortDIOSet"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, PortDigitalIOS value)
        {
            return HarpMessage.FromByte(Address, timestamp, messageType, (byte)value);
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// PortDIOSet register.
    /// </summary>
    /// <seealso cref="PortDIOSet"/>
    [Description("Filters and selects timestamped messages from the PortDIOSet register.")]
    public partial class TimestampedPortDIOSet
    {
        /// <summary>
        /// Represents the address of the <see cref="PortDIOSet"/> register. This field is constant.
        /// </summary>
        public const int Address = PortDIOSet.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="PortDIOSet"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<PortDigitalIOS> GetPayload(HarpMessage message)
        {
            return PortDIOSet.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that manipulates messages from register PulseDOPort0.
    /// </summary>
    [Description("")]
    public partial class PulseDOPort0
    {
        /// <summary>
        /// Represents the address of the <see cref="PulseDOPort0"/> register. This field is constant.
        /// </summary>
        public const int Address = 42;

        /// <summary>
        /// Represents the payload type of the <see cref="PulseDOPort0"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U16;

        /// <summary>
        /// Represents the length of the <see cref="PulseDOPort0"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 1;

        /// <summary>
        /// Returns the payload data for <see cref="PulseDOPort0"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static ushort GetPayload(HarpMessage message)
        {
            return message.GetPayloadUInt16();
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="PulseDOPort0"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<ushort> GetTimestampedPayload(HarpMessage message)
        {
            return message.GetTimestampedPayloadUInt16();
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="PulseDOPort0"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="PulseDOPort0"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, ushort value)
        {
            return HarpMessage.FromUInt16(Address, messageType, value);
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="PulseDOPort0"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="PulseDOPort0"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, ushort value)
        {
            return HarpMessage.FromUInt16(Address, timestamp, messageType, value);
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// PulseDOPort0 register.
    /// </summary>
    /// <seealso cref="PulseDOPort0"/>
    [Description("Filters and selects timestamped messages from the PulseDOPort0 register.")]
    public partial class TimestampedPulseDOPort0
    {
        /// <summary>
        /// Represents the address of the <see cref="PulseDOPort0"/> register. This field is constant.
        /// </summary>
        public const int Address = PulseDOPort0.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="PulseDOPort0"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<ushort> GetPayload(HarpMessage message)
        {
            return PulseDOPort0.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that manipulates messages from register PulseDO0.
    /// </summary>
    [Description("")]
    public partial class PulseDO0
    {
        /// <summary>
        /// Represents the address of the <see cref="PulseDO0"/> register. This field is constant.
        /// </summary>
        public const int Address = 43;

        /// <summary>
        /// Represents the payload type of the <see cref="PulseDO0"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U16;

        /// <summary>
        /// Represents the length of the <see cref="PulseDO0"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 1;

        /// <summary>
        /// Returns the payload data for <see cref="PulseDO0"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static ushort GetPayload(HarpMessage message)
        {
            return message.GetPayloadUInt16();
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="PulseDO0"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<ushort> GetTimestampedPayload(HarpMessage message)
        {
            return message.GetTimestampedPayloadUInt16();
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="PulseDO0"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="PulseDO0"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, ushort value)
        {
            return HarpMessage.FromUInt16(Address, messageType, value);
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="PulseDO0"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="PulseDO0"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, ushort value)
        {
            return HarpMessage.FromUInt16(Address, timestamp, messageType, value);
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// PulseDO0 register.
    /// </summary>
    /// <seealso cref="PulseDO0"/>
    [Description("Filters and selects timestamped messages from the PulseDO0 register.")]
    public partial class TimestampedPulseDO0
    {
        /// <summary>
        /// Represents the address of the <see cref="PulseDO0"/> register. This field is constant.
        /// </summary>
        public const int Address = PulseDO0.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="PulseDO0"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<ushort> GetPayload(HarpMessage message)
        {
            return PulseDO0.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that manipulates messages from register StartPulse.
    /// </summary>
    [Description("")]
    public partial class StartPulse
    {
        /// <summary>
        /// Represents the address of the <see cref="StartPulse"/> register. This field is constant.
        /// </summary>
        public const int Address = 37;

        /// <summary>
        /// Represents the payload type of the <see cref="StartPulse"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U16;

        /// <summary>
        /// Represents the length of the <see cref="StartPulse"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 1;

        static StartPulsePayload ParsePayload(ushort payload)
        {
            StartPulsePayload result;
            result.DigitalOutput = (PwmPort)(ushort)((payload & 0xC00) >> 10);
            result.PulseWidth = (ushort)(ushort)(payload & 0x3FF);
            return result;
        }

        static ushort FormatPayload(StartPulsePayload value)
        {
            ushort result;
            result = (ushort)(((ushort)value.DigitalOutput << 10) & 0xC00);
            result |= (ushort)((ushort)value.PulseWidth & 0x3FF);
            return result;
        }

        /// <summary>
        /// Returns the payload data for <see cref="StartPulse"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static StartPulsePayload GetPayload(HarpMessage message)
        {
            return ParsePayload(message.GetPayloadUInt16());
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="StartPulse"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<StartPulsePayload> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadUInt16();
            return Timestamped.Create(ParsePayload(payload.Value), payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="StartPulse"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="StartPulse"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, StartPulsePayload value)
        {
            return HarpMessage.FromUInt16(Address, messageType, FormatPayload(value));
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="StartPulse"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="StartPulse"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, StartPulsePayload value)
        {
            return HarpMessage.FromUInt16(Address, timestamp, messageType, FormatPayload(value));
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// StartPulse register.
    /// </summary>
    /// <seealso cref="StartPulse"/>
    [Description("Filters and selects timestamped messages from the StartPulse register.")]
    public partial class TimestampedStartPulse
    {
        /// <summary>
        /// Represents the address of the <see cref="StartPulse"/> register. This field is constant.
        /// </summary>
        public const int Address = StartPulse.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="StartPulse"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<StartPulsePayload> GetPayload(HarpMessage message)
        {
            return StartPulse.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents a register that manipulates messages from register StartPulseTrain.
    /// </summary>
    [Description("")]
    public partial class StartPulseTrain
    {
        /// <summary>
        /// Represents the address of the <see cref="StartPulseTrain"/> register. This field is constant.
        /// </summary>
        public const int Address = 38;

        /// <summary>
        /// Represents the payload type of the <see cref="StartPulseTrain"/> register. This field is constant.
        /// </summary>
        public const PayloadType RegisterType = PayloadType.U16;

        /// <summary>
        /// Represents the length of the <see cref="StartPulseTrain"/> register. This field is constant.
        /// </summary>
        public const int RegisterLength = 2;

        static StartPulseTrainPayload ParsePayload(ushort[] payload)
        {
            StartPulseTrainPayload result;
            result.DigitalOutput = (PwmPort)(ushort)((payload[0] & 0xC00) >> 10);
            result.PulseWidth = (ushort)(ushort)(payload[0] & 0x3FF);
            result.Frequency = (byte)(ushort)((payload[1] & 0xFF00) >> 8);
            result.PulseCount = (byte)(ushort)(payload[1] & 0xFF);
            return result;
        }

        static ushort[] FormatPayload(StartPulseTrainPayload value)
        {
            ushort[] result;
            result = new ushort[2];
            result[0] = (ushort)(((ushort)value.DigitalOutput << 10) & 0xC00);
            result[0] |= (ushort)((ushort)value.PulseWidth & 0x3FF);
            result[1] = (ushort)(((ushort)value.Frequency << 8) & 0xFF00);
            result[1] |= (ushort)((ushort)value.PulseCount & 0xFF);
            return result;
        }

        /// <summary>
        /// Returns the payload data for <see cref="StartPulseTrain"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the message payload.</returns>
        public static StartPulseTrainPayload GetPayload(HarpMessage message)
        {
            return ParsePayload(message.GetPayloadArray<ushort>());
        }

        /// <summary>
        /// Returns the timestamped payload data for <see cref="StartPulseTrain"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<StartPulseTrainPayload> GetTimestampedPayload(HarpMessage message)
        {
            var payload = message.GetTimestampedPayloadArray<ushort>();
            return Timestamped.Create(ParsePayload(payload.Value), payload.Seconds);
        }

        /// <summary>
        /// Returns a Harp message for the <see cref="StartPulseTrain"/> register.
        /// </summary>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="StartPulseTrain"/> register
        /// with the specified message type and payload.
        /// </returns>
        public static HarpMessage FromPayload(MessageType messageType, StartPulseTrainPayload value)
        {
            return HarpMessage.FromUInt16(Address, messageType, FormatPayload(value));
        }

        /// <summary>
        /// Returns a timestamped Harp message for the <see cref="StartPulseTrain"/>
        /// register.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">The type of the Harp message.</param>
        /// <param name="value">The value to be stored in the message payload.</param>
        /// <returns>
        /// A <see cref="HarpMessage"/> object for the <see cref="StartPulseTrain"/> register
        /// with the specified message type, timestamp, and payload.
        /// </returns>
        public static HarpMessage FromPayload(double timestamp, MessageType messageType, StartPulseTrainPayload value)
        {
            return HarpMessage.FromUInt16(Address, timestamp, messageType, FormatPayload(value));
        }
    }

    /// <summary>
    /// Provides methods for manipulating timestamped messages from the
    /// StartPulseTrain register.
    /// </summary>
    /// <seealso cref="StartPulseTrain"/>
    [Description("Filters and selects timestamped messages from the StartPulseTrain register.")]
    public partial class TimestampedStartPulseTrain
    {
        /// <summary>
        /// Represents the address of the <see cref="StartPulseTrain"/> register. This field is constant.
        /// </summary>
        public const int Address = StartPulseTrain.Address;

        /// <summary>
        /// Returns timestamped payload data for <see cref="StartPulseTrain"/> register messages.
        /// </summary>
        /// <param name="message">A <see cref="HarpMessage"/> object representing the register message.</param>
        /// <returns>A value representing the timestamped message payload.</returns>
        public static Timestamped<StartPulseTrainPayload> GetPayload(HarpMessage message)
        {
            return StartPulseTrain.GetTimestampedPayload(message);
        }
    }

    /// <summary>
    /// Represents an operator which creates standard message payloads for the
    /// Tests device.
    /// </summary>
    /// <seealso cref="CreateDigitalInputsPayload"/>
    /// <seealso cref="CreateAnalogDataPayload"/>
    /// <seealso cref="CreateComplexConfigurationPayload"/>
    /// <seealso cref="CreateVersionPayload"/>
    /// <seealso cref="CreateCustomPayloadPayload"/>
    /// <seealso cref="CreateCustomRawPayloadPayload"/>
    /// <seealso cref="CreateCustomMemberConverterPayload"/>
    /// <seealso cref="CreateBitmaskSplitterPayload"/>
    /// <seealso cref="CreateCounter0Payload"/>
    /// <seealso cref="CreatePortDIOSetPayload"/>
    /// <seealso cref="CreatePulseDOPort0Payload"/>
    /// <seealso cref="CreatePulseDO0Payload"/>
    /// <seealso cref="CreateStartPulsePayload"/>
    /// <seealso cref="CreateStartPulseTrainPayload"/>
    [XmlInclude(typeof(CreateDigitalInputsPayload))]
    [XmlInclude(typeof(CreateAnalogDataPayload))]
    [XmlInclude(typeof(CreateComplexConfigurationPayload))]
    [XmlInclude(typeof(CreateVersionPayload))]
    [XmlInclude(typeof(CreateCustomPayloadPayload))]
    [XmlInclude(typeof(CreateCustomRawPayloadPayload))]
    [XmlInclude(typeof(CreateCustomMemberConverterPayload))]
    [XmlInclude(typeof(CreateBitmaskSplitterPayload))]
    [XmlInclude(typeof(CreateCounter0Payload))]
    [XmlInclude(typeof(CreatePortDIOSetPayload))]
    [XmlInclude(typeof(CreatePulseDOPort0Payload))]
    [XmlInclude(typeof(CreatePulseDO0Payload))]
    [XmlInclude(typeof(CreateStartPulsePayload))]
    [XmlInclude(typeof(CreateStartPulseTrainPayload))]
    [XmlInclude(typeof(CreateTimestampedDigitalInputsPayload))]
    [XmlInclude(typeof(CreateTimestampedAnalogDataPayload))]
    [XmlInclude(typeof(CreateTimestampedComplexConfigurationPayload))]
    [XmlInclude(typeof(CreateTimestampedVersionPayload))]
    [XmlInclude(typeof(CreateTimestampedCustomPayloadPayload))]
    [XmlInclude(typeof(CreateTimestampedCustomRawPayloadPayload))]
    [XmlInclude(typeof(CreateTimestampedCustomMemberConverterPayload))]
    [XmlInclude(typeof(CreateTimestampedBitmaskSplitterPayload))]
    [XmlInclude(typeof(CreateTimestampedCounter0Payload))]
    [XmlInclude(typeof(CreateTimestampedPortDIOSetPayload))]
    [XmlInclude(typeof(CreateTimestampedPulseDOPort0Payload))]
    [XmlInclude(typeof(CreateTimestampedPulseDO0Payload))]
    [XmlInclude(typeof(CreateTimestampedStartPulsePayload))]
    [XmlInclude(typeof(CreateTimestampedStartPulseTrainPayload))]
    [Description("Creates standard message payloads for the Tests device.")]
    public partial class CreateMessage : CreateMessageBuilder, INamedElement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateMessage"/> class.
        /// </summary>
        public CreateMessage()
        {
            Payload = new CreateDigitalInputsPayload();
        }

        string INamedElement.Name => $"{nameof(Tests)}.{GetElementDisplayName(Payload)}";
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// for register DigitalInputs.
    /// </summary>
    [DisplayName("DigitalInputsPayload")]
    [Description("Creates a message payload for register DigitalInputs.")]
    public partial class CreateDigitalInputsPayload
    {
        /// <summary>
        /// Gets or sets the value for register DigitalInputs.
        /// </summary>
        [Description("The value for register DigitalInputs.")]
        public byte DigitalInputs { get; set; }

        /// <summary>
        /// Creates a message payload for the DigitalInputs register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public byte GetPayload()
        {
            return DigitalInputs;
        }

        /// <summary>
        /// Creates a message for register DigitalInputs.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the DigitalInputs register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return Harp.Generators.Tests.DigitalInputs.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// for register DigitalInputs.
    /// </summary>
    [DisplayName("TimestampedDigitalInputsPayload")]
    [Description("Creates a timestamped message payload for register DigitalInputs.")]
    public partial class CreateTimestampedDigitalInputsPayload : CreateDigitalInputsPayload
    {
        /// <summary>
        /// Creates a timestamped message for register DigitalInputs.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the DigitalInputs register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return Harp.Generators.Tests.DigitalInputs.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// for register AnalogData.
    /// </summary>
    [DisplayName("AnalogDataPayload")]
    [Description("Creates a message payload for register AnalogData.")]
    public partial class CreateAnalogDataPayload
    {
        /// <summary>
        /// Gets or sets a value to write on payload member Analog0.
        /// </summary>
        [Description("")]
        public float Analog0 { get; set; }

        /// <summary>
        /// Gets or sets a value to write on payload member Analog1.
        /// </summary>
        [Description("")]
        public float Analog1 { get; set; }

        /// <summary>
        /// Gets or sets a value to write on payload member Analog2.
        /// </summary>
        [Description("")]
        public float Analog2 { get; set; }

        /// <summary>
        /// Gets or sets a value to write on payload member Accelerometer.
        /// </summary>
        [Description("")]
        public float[] Accelerometer { get; set; }

        /// <summary>
        /// Creates a message payload for the AnalogData register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public AnalogDataPayload GetPayload()
        {
            AnalogDataPayload value;
            value.Analog0 = Analog0;
            value.Analog1 = Analog1;
            value.Analog2 = Analog2;
            value.Accelerometer = Accelerometer;
            return value;
        }

        /// <summary>
        /// Creates a message for register AnalogData.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the AnalogData register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return Harp.Generators.Tests.AnalogData.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// for register AnalogData.
    /// </summary>
    [DisplayName("TimestampedAnalogDataPayload")]
    [Description("Creates a timestamped message payload for register AnalogData.")]
    public partial class CreateTimestampedAnalogDataPayload : CreateAnalogDataPayload
    {
        /// <summary>
        /// Creates a timestamped message for register AnalogData.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the AnalogData register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return Harp.Generators.Tests.AnalogData.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// for register ComplexConfiguration.
    /// </summary>
    [DisplayName("ComplexConfigurationPayload")]
    [Description("Creates a message payload for register ComplexConfiguration.")]
    public partial class CreateComplexConfigurationPayload
    {
        /// <summary>
        /// Gets or sets a value to write on payload member PwmPort.
        /// </summary>
        [Description("")]
        public PwmPort PwmPort { get; set; }

        /// <summary>
        /// Gets or sets a value to write on payload member DutyCycle.
        /// </summary>
        [Description("")]
        public float DutyCycle { get; set; }

        /// <summary>
        /// Gets or sets a value to write on payload member Frequency.
        /// </summary>
        [Description("")]
        public float Frequency { get; set; }

        /// <summary>
        /// Gets or sets a value to write on payload member EventsEnabled.
        /// </summary>
        [Description("")]
        public bool EventsEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value to write on payload member Delta.
        /// </summary>
        [Description("")]
        public uint Delta { get; set; }

        /// <summary>
        /// Creates a message payload for the ComplexConfiguration register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public ComplexConfigurationPayload GetPayload()
        {
            ComplexConfigurationPayload value;
            value.PwmPort = PwmPort;
            value.DutyCycle = DutyCycle;
            value.Frequency = Frequency;
            value.EventsEnabled = EventsEnabled;
            value.Delta = Delta;
            return value;
        }

        /// <summary>
        /// Creates a message for register ComplexConfiguration.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the ComplexConfiguration register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return Harp.Generators.Tests.ComplexConfiguration.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// for register ComplexConfiguration.
    /// </summary>
    [DisplayName("TimestampedComplexConfigurationPayload")]
    [Description("Creates a timestamped message payload for register ComplexConfiguration.")]
    public partial class CreateTimestampedComplexConfigurationPayload : CreateComplexConfigurationPayload
    {
        /// <summary>
        /// Creates a timestamped message for register ComplexConfiguration.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the ComplexConfiguration register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return Harp.Generators.Tests.ComplexConfiguration.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// for register Version.
    /// </summary>
    [DisplayName("VersionPayload")]
    [Description("Creates a message payload for register Version.")]
    public partial class CreateVersionPayload
    {
        /// <summary>
        /// Gets or sets a value to write on payload member ProtocolVersion.
        /// </summary>
        [Description("")]
        public HarpVersion ProtocolVersion { get; set; }

        /// <summary>
        /// Gets or sets a value to write on payload member FirmwareVersion.
        /// </summary>
        [Description("")]
        public HarpVersion FirmwareVersion { get; set; }

        /// <summary>
        /// Gets or sets a value to write on payload member HardwareVersion.
        /// </summary>
        [Description("")]
        public HarpVersion HardwareVersion { get; set; }

        /// <summary>
        /// Gets or sets a value to write on payload member CoreId.
        /// </summary>
        [Description("")]
        public string CoreId { get; set; }

        /// <summary>
        /// Gets or sets a value to write on payload member InterfaceHash.
        /// </summary>
        [Description("")]
        public byte[] InterfaceHash { get; set; }

        /// <summary>
        /// Creates a message payload for the Version register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public VersionPayload GetPayload()
        {
            VersionPayload value;
            value.ProtocolVersion = ProtocolVersion;
            value.FirmwareVersion = FirmwareVersion;
            value.HardwareVersion = HardwareVersion;
            value.CoreId = CoreId;
            value.InterfaceHash = InterfaceHash;
            return value;
        }

        /// <summary>
        /// Creates a message for register Version.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the Version register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return Harp.Generators.Tests.Version.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// for register Version.
    /// </summary>
    [DisplayName("TimestampedVersionPayload")]
    [Description("Creates a timestamped message payload for register Version.")]
    public partial class CreateTimestampedVersionPayload : CreateVersionPayload
    {
        /// <summary>
        /// Creates a timestamped message for register Version.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the Version register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return Harp.Generators.Tests.Version.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// for register CustomPayload.
    /// </summary>
    [DisplayName("CustomPayloadPayload")]
    [Description("Creates a message payload for register CustomPayload.")]
    public partial class CreateCustomPayloadPayload
    {
        /// <summary>
        /// Gets or sets the value for register CustomPayload.
        /// </summary>
        [Description("The value for register CustomPayload.")]
        public HarpVersion CustomPayload { get; set; }

        /// <summary>
        /// Creates a message payload for the CustomPayload register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public HarpVersion GetPayload()
        {
            return CustomPayload;
        }

        /// <summary>
        /// Creates a message for register CustomPayload.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the CustomPayload register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return Harp.Generators.Tests.CustomPayload.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// for register CustomPayload.
    /// </summary>
    [DisplayName("TimestampedCustomPayloadPayload")]
    [Description("Creates a timestamped message payload for register CustomPayload.")]
    public partial class CreateTimestampedCustomPayloadPayload : CreateCustomPayloadPayload
    {
        /// <summary>
        /// Creates a timestamped message for register CustomPayload.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the CustomPayload register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return Harp.Generators.Tests.CustomPayload.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// for register CustomRawPayload.
    /// </summary>
    [DisplayName("CustomRawPayloadPayload")]
    [Description("Creates a message payload for register CustomRawPayload.")]
    public partial class CreateCustomRawPayloadPayload
    {
        /// <summary>
        /// Gets or sets the value for register CustomRawPayload.
        /// </summary>
        [Description("The value for register CustomRawPayload.")]
        public HarpVersion CustomRawPayload { get; set; }

        /// <summary>
        /// Creates a message payload for the CustomRawPayload register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public HarpVersion GetPayload()
        {
            return CustomRawPayload;
        }

        /// <summary>
        /// Creates a message for register CustomRawPayload.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the CustomRawPayload register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return Harp.Generators.Tests.CustomRawPayload.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// for register CustomRawPayload.
    /// </summary>
    [DisplayName("TimestampedCustomRawPayloadPayload")]
    [Description("Creates a timestamped message payload for register CustomRawPayload.")]
    public partial class CreateTimestampedCustomRawPayloadPayload : CreateCustomRawPayloadPayload
    {
        /// <summary>
        /// Creates a timestamped message for register CustomRawPayload.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the CustomRawPayload register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return Harp.Generators.Tests.CustomRawPayload.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// for register CustomMemberConverter.
    /// </summary>
    [DisplayName("CustomMemberConverterPayload")]
    [Description("Creates a message payload for register CustomMemberConverter.")]
    public partial class CreateCustomMemberConverterPayload
    {
        /// <summary>
        /// Gets or sets a value to write on payload member Header.
        /// </summary>
        [Description("")]
        public byte Header { get; set; }

        /// <summary>
        /// Gets or sets a value to write on payload member Data.
        /// </summary>
        [Description("")]
        public int Data { get; set; }

        /// <summary>
        /// Creates a message payload for the CustomMemberConverter register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public CustomMemberConverterPayload GetPayload()
        {
            CustomMemberConverterPayload value;
            value.Header = Header;
            value.Data = Data;
            return value;
        }

        /// <summary>
        /// Creates a message for register CustomMemberConverter.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the CustomMemberConverter register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return Harp.Generators.Tests.CustomMemberConverter.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// for register CustomMemberConverter.
    /// </summary>
    [DisplayName("TimestampedCustomMemberConverterPayload")]
    [Description("Creates a timestamped message payload for register CustomMemberConverter.")]
    public partial class CreateTimestampedCustomMemberConverterPayload : CreateCustomMemberConverterPayload
    {
        /// <summary>
        /// Creates a timestamped message for register CustomMemberConverter.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the CustomMemberConverter register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return Harp.Generators.Tests.CustomMemberConverter.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// for register BitmaskSplitter.
    /// </summary>
    [DisplayName("BitmaskSplitterPayload")]
    [Description("Creates a message payload for register BitmaskSplitter.")]
    public partial class CreateBitmaskSplitterPayload
    {
        /// <summary>
        /// Gets or sets a value to write on payload member Low.
        /// </summary>
        [Description("")]
        public int Low { get; set; }

        /// <summary>
        /// Gets or sets a value to write on payload member High.
        /// </summary>
        [Description("")]
        public int High { get; set; }

        /// <summary>
        /// Creates a message payload for the BitmaskSplitter register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public BitmaskSplitterPayload GetPayload()
        {
            BitmaskSplitterPayload value;
            value.Low = Low;
            value.High = High;
            return value;
        }

        /// <summary>
        /// Creates a message for register BitmaskSplitter.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the BitmaskSplitter register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return Harp.Generators.Tests.BitmaskSplitter.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// for register BitmaskSplitter.
    /// </summary>
    [DisplayName("TimestampedBitmaskSplitterPayload")]
    [Description("Creates a timestamped message payload for register BitmaskSplitter.")]
    public partial class CreateTimestampedBitmaskSplitterPayload : CreateBitmaskSplitterPayload
    {
        /// <summary>
        /// Creates a timestamped message for register BitmaskSplitter.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the BitmaskSplitter register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return Harp.Generators.Tests.BitmaskSplitter.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// for register Counter0.
    /// </summary>
    [DisplayName("Counter0Payload")]
    [Description("Creates a message payload for register Counter0.")]
    public partial class CreateCounter0Payload
    {
        /// <summary>
        /// Gets or sets the value for register Counter0.
        /// </summary>
        [Description("The value for register Counter0.")]
        public int Counter0 { get; set; }

        /// <summary>
        /// Creates a message payload for the Counter0 register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public int GetPayload()
        {
            return Counter0;
        }

        /// <summary>
        /// Creates a message for register Counter0.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the Counter0 register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return Harp.Generators.Tests.Counter0.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// for register Counter0.
    /// </summary>
    [DisplayName("TimestampedCounter0Payload")]
    [Description("Creates a timestamped message payload for register Counter0.")]
    public partial class CreateTimestampedCounter0Payload : CreateCounter0Payload
    {
        /// <summary>
        /// Creates a timestamped message for register Counter0.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the Counter0 register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return Harp.Generators.Tests.Counter0.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// for register PortDIOSet.
    /// </summary>
    [DisplayName("PortDIOSetPayload")]
    [Description("Creates a message payload for register PortDIOSet.")]
    public partial class CreatePortDIOSetPayload
    {
        /// <summary>
        /// Gets or sets the value for register PortDIOSet.
        /// </summary>
        [Description("The value for register PortDIOSet.")]
        public PortDigitalIOS PortDIOSet { get; set; }

        /// <summary>
        /// Creates a message payload for the PortDIOSet register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public PortDigitalIOS GetPayload()
        {
            return PortDIOSet;
        }

        /// <summary>
        /// Creates a message for register PortDIOSet.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the PortDIOSet register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return Harp.Generators.Tests.PortDIOSet.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// for register PortDIOSet.
    /// </summary>
    [DisplayName("TimestampedPortDIOSetPayload")]
    [Description("Creates a timestamped message payload for register PortDIOSet.")]
    public partial class CreateTimestampedPortDIOSetPayload : CreatePortDIOSetPayload
    {
        /// <summary>
        /// Creates a timestamped message for register PortDIOSet.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the PortDIOSet register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return Harp.Generators.Tests.PortDIOSet.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// for register PulseDOPort0.
    /// </summary>
    [DisplayName("PulseDOPort0Payload")]
    [Description("Creates a message payload for register PulseDOPort0.")]
    public partial class CreatePulseDOPort0Payload
    {
        /// <summary>
        /// Gets or sets the value for register PulseDOPort0.
        /// </summary>
        [Range(min: 1, max: long.MaxValue)]
        [Editor(DesignTypes.NumericUpDownEditor, DesignTypes.UITypeEditor)]
        [Description("The value for register PulseDOPort0.")]
        public ushort PulseDOPort0 { get; set; } = 1;

        /// <summary>
        /// Creates a message payload for the PulseDOPort0 register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public ushort GetPayload()
        {
            return PulseDOPort0;
        }

        /// <summary>
        /// Creates a message for register PulseDOPort0.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the PulseDOPort0 register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return Harp.Generators.Tests.PulseDOPort0.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// for register PulseDOPort0.
    /// </summary>
    [DisplayName("TimestampedPulseDOPort0Payload")]
    [Description("Creates a timestamped message payload for register PulseDOPort0.")]
    public partial class CreateTimestampedPulseDOPort0Payload : CreatePulseDOPort0Payload
    {
        /// <summary>
        /// Creates a timestamped message for register PulseDOPort0.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the PulseDOPort0 register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return Harp.Generators.Tests.PulseDOPort0.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// for register PulseDO0.
    /// </summary>
    [DisplayName("PulseDO0Payload")]
    [Description("Creates a message payload for register PulseDO0.")]
    public partial class CreatePulseDO0Payload
    {
        /// <summary>
        /// Gets or sets the value for register PulseDO0.
        /// </summary>
        [Range(min: 1, max: long.MaxValue)]
        [Editor(DesignTypes.NumericUpDownEditor, DesignTypes.UITypeEditor)]
        [Description("The value for register PulseDO0.")]
        public ushort PulseDO0 { get; set; } = 1;

        /// <summary>
        /// Creates a message payload for the PulseDO0 register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public ushort GetPayload()
        {
            return PulseDO0;
        }

        /// <summary>
        /// Creates a message for register PulseDO0.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the PulseDO0 register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return Harp.Generators.Tests.PulseDO0.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// for register PulseDO0.
    /// </summary>
    [DisplayName("TimestampedPulseDO0Payload")]
    [Description("Creates a timestamped message payload for register PulseDO0.")]
    public partial class CreateTimestampedPulseDO0Payload : CreatePulseDO0Payload
    {
        /// <summary>
        /// Creates a timestamped message for register PulseDO0.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the PulseDO0 register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return Harp.Generators.Tests.PulseDO0.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// for register StartPulse.
    /// </summary>
    [DisplayName("StartPulsePayload")]
    [Description("Creates a message payload for register StartPulse.")]
    public partial class CreateStartPulsePayload
    {
        /// <summary>
        /// Gets or sets a value to write on payload member DigitalOutput.
        /// </summary>
        [Description("")]
        public PwmPort DigitalOutput { get; set; }

        /// <summary>
        /// Gets or sets a value to write on payload member PulseWidth.
        /// </summary>
        [Description("")]
        public ushort PulseWidth { get; set; }

        /// <summary>
        /// Creates a message payload for the StartPulse register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public StartPulsePayload GetPayload()
        {
            StartPulsePayload value;
            value.DigitalOutput = DigitalOutput;
            value.PulseWidth = PulseWidth;
            return value;
        }

        /// <summary>
        /// Creates a message for register StartPulse.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the StartPulse register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return Harp.Generators.Tests.StartPulse.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// for register StartPulse.
    /// </summary>
    [DisplayName("TimestampedStartPulsePayload")]
    [Description("Creates a timestamped message payload for register StartPulse.")]
    public partial class CreateTimestampedStartPulsePayload : CreateStartPulsePayload
    {
        /// <summary>
        /// Creates a timestamped message for register StartPulse.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the StartPulse register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return Harp.Generators.Tests.StartPulse.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a message payload
    /// for register StartPulseTrain.
    /// </summary>
    [DisplayName("StartPulseTrainPayload")]
    [Description("Creates a message payload for register StartPulseTrain.")]
    public partial class CreateStartPulseTrainPayload
    {
        /// <summary>
        /// Gets or sets a value to write on payload member DigitalOutput.
        /// </summary>
        [Description("")]
        public PwmPort DigitalOutput { get; set; }

        /// <summary>
        /// Gets or sets a value to write on payload member PulseWidth.
        /// </summary>
        [Description("")]
        public ushort PulseWidth { get; set; }

        /// <summary>
        /// Gets or sets a value to write on payload member Frequency.
        /// </summary>
        [Range(min: 1, max: 255)]
        [Editor(DesignTypes.NumericUpDownEditor, DesignTypes.UITypeEditor)]
        [Description("")]
        public byte Frequency { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value to write on payload member PulseCount.
        /// </summary>
        [Description("")]
        public byte PulseCount { get; set; }

        /// <summary>
        /// Creates a message payload for the StartPulseTrain register.
        /// </summary>
        /// <returns>The created message payload value.</returns>
        public StartPulseTrainPayload GetPayload()
        {
            StartPulseTrainPayload value;
            value.DigitalOutput = DigitalOutput;
            value.PulseWidth = PulseWidth;
            value.Frequency = Frequency;
            value.PulseCount = PulseCount;
            return value;
        }

        /// <summary>
        /// Creates a message for register StartPulseTrain.
        /// </summary>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new message for the StartPulseTrain register.</returns>
        public HarpMessage GetMessage(MessageType messageType)
        {
            return Harp.Generators.Tests.StartPulseTrain.FromPayload(messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents an operator that creates a timestamped message payload
    /// for register StartPulseTrain.
    /// </summary>
    [DisplayName("TimestampedStartPulseTrainPayload")]
    [Description("Creates a timestamped message payload for register StartPulseTrain.")]
    public partial class CreateTimestampedStartPulseTrainPayload : CreateStartPulseTrainPayload
    {
        /// <summary>
        /// Creates a timestamped message for register StartPulseTrain.
        /// </summary>
        /// <param name="timestamp">The timestamp of the message payload, in seconds.</param>
        /// <param name="messageType">Specifies the type of the created message.</param>
        /// <returns>A new timestamped message for the StartPulseTrain register.</returns>
        public HarpMessage GetMessage(double timestamp, MessageType messageType)
        {
            return Harp.Generators.Tests.StartPulseTrain.FromPayload(timestamp, messageType, GetPayload());
        }
    }

    /// <summary>
    /// Represents the payload of the AnalogData register.
    /// </summary>
    public struct AnalogDataPayload
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AnalogDataPayload"/> structure.
        /// </summary>
        /// <param name="analog0"></param>
        /// <param name="analog1"></param>
        /// <param name="analog2"></param>
        /// <param name="accelerometer"></param>
        public AnalogDataPayload(
            float analog0,
            float analog1,
            float analog2,
            float[] accelerometer)
        {
            Analog0 = analog0;
            Analog1 = analog1;
            Analog2 = analog2;
            Accelerometer = accelerometer;
        }

        /// <summary>
        /// 
        /// </summary>
        public float Analog0;

        /// <summary>
        /// 
        /// </summary>
        public float Analog1;

        /// <summary>
        /// 
        /// </summary>
        public float Analog2;

        /// <summary>
        /// 
        /// </summary>
        public float[] Accelerometer;

        /// <summary>
        /// Returns a <see cref="string"/> that represents the payload of
        /// the AnalogData register.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the payload of the
        /// AnalogData register.
        /// </returns>
        public override string ToString()
        {
            return "AnalogDataPayload { " +
                "Analog0 = " + Analog0 + ", " +
                "Analog1 = " + Analog1 + ", " +
                "Analog2 = " + Analog2 + ", " +
                "Accelerometer = " + Accelerometer + " " +
            "}";
        }
    }

    /// <summary>
    /// Represents the payload of the ComplexConfiguration register.
    /// </summary>
    public struct ComplexConfigurationPayload
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ComplexConfigurationPayload"/> structure.
        /// </summary>
        /// <param name="pwmPort"></param>
        /// <param name="dutyCycle"></param>
        /// <param name="frequency"></param>
        /// <param name="eventsEnabled"></param>
        /// <param name="delta"></param>
        public ComplexConfigurationPayload(
            PwmPort pwmPort,
            float dutyCycle,
            float frequency,
            bool eventsEnabled,
            uint delta)
        {
            PwmPort = pwmPort;
            DutyCycle = dutyCycle;
            Frequency = frequency;
            EventsEnabled = eventsEnabled;
            Delta = delta;
        }

        /// <summary>
        /// 
        /// </summary>
        public PwmPort PwmPort;

        /// <summary>
        /// 
        /// </summary>
        public float DutyCycle;

        /// <summary>
        /// 
        /// </summary>
        public float Frequency;

        /// <summary>
        /// 
        /// </summary>
        public bool EventsEnabled;

        /// <summary>
        /// 
        /// </summary>
        public uint Delta;

        /// <summary>
        /// Returns a <see cref="string"/> that represents the payload of
        /// the ComplexConfiguration register.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the payload of the
        /// ComplexConfiguration register.
        /// </returns>
        public override string ToString()
        {
            return "ComplexConfigurationPayload { " +
                "PwmPort = " + PwmPort + ", " +
                "DutyCycle = " + DutyCycle + ", " +
                "Frequency = " + Frequency + ", " +
                "EventsEnabled = " + EventsEnabled + ", " +
                "Delta = " + Delta + " " +
            "}";
        }
    }

    /// <summary>
    /// Represents the payload of the Version register.
    /// </summary>
    public struct VersionPayload
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VersionPayload"/> structure.
        /// </summary>
        /// <param name="protocolVersion"></param>
        /// <param name="firmwareVersion"></param>
        /// <param name="hardwareVersion"></param>
        /// <param name="coreId"></param>
        /// <param name="interfaceHash"></param>
        public VersionPayload(
            HarpVersion protocolVersion,
            HarpVersion firmwareVersion,
            HarpVersion hardwareVersion,
            string coreId,
            byte[] interfaceHash)
        {
            ProtocolVersion = protocolVersion;
            FirmwareVersion = firmwareVersion;
            HardwareVersion = hardwareVersion;
            CoreId = coreId;
            InterfaceHash = interfaceHash;
        }

        /// <summary>
        /// 
        /// </summary>
        public HarpVersion ProtocolVersion;

        /// <summary>
        /// 
        /// </summary>
        public HarpVersion FirmwareVersion;

        /// <summary>
        /// 
        /// </summary>
        public HarpVersion HardwareVersion;

        /// <summary>
        /// 
        /// </summary>
        public string CoreId;

        /// <summary>
        /// 
        /// </summary>
        public byte[] InterfaceHash;

        /// <summary>
        /// Returns a <see cref="string"/> that represents the payload of
        /// the Version register.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the payload of the
        /// Version register.
        /// </returns>
        public override string ToString()
        {
            return "VersionPayload { " +
                "ProtocolVersion = " + ProtocolVersion + ", " +
                "FirmwareVersion = " + FirmwareVersion + ", " +
                "HardwareVersion = " + HardwareVersion + ", " +
                "CoreId = " + CoreId + ", " +
                "InterfaceHash = " + InterfaceHash + " " +
            "}";
        }
    }

    /// <summary>
    /// Represents the payload of the CustomMemberConverter register.
    /// </summary>
    public struct CustomMemberConverterPayload
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomMemberConverterPayload"/> structure.
        /// </summary>
        /// <param name="header"></param>
        /// <param name="data"></param>
        public CustomMemberConverterPayload(
            byte header,
            int data)
        {
            Header = header;
            Data = data;
        }

        /// <summary>
        /// 
        /// </summary>
        public byte Header;

        /// <summary>
        /// 
        /// </summary>
        public int Data;

        /// <summary>
        /// Returns a <see cref="string"/> that represents the payload of
        /// the CustomMemberConverter register.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the payload of the
        /// CustomMemberConverter register.
        /// </returns>
        public override string ToString()
        {
            return "CustomMemberConverterPayload { " +
                "Header = " + Header + ", " +
                "Data = " + Data + " " +
            "}";
        }
    }

    /// <summary>
    /// Represents the payload of the BitmaskSplitter register.
    /// </summary>
    public struct BitmaskSplitterPayload
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BitmaskSplitterPayload"/> structure.
        /// </summary>
        /// <param name="low"></param>
        /// <param name="high"></param>
        public BitmaskSplitterPayload(
            int low,
            int high)
        {
            Low = low;
            High = high;
        }

        /// <summary>
        /// 
        /// </summary>
        public int Low;

        /// <summary>
        /// 
        /// </summary>
        public int High;

        /// <summary>
        /// Returns a <see cref="string"/> that represents the payload of
        /// the BitmaskSplitter register.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the payload of the
        /// BitmaskSplitter register.
        /// </returns>
        public override string ToString()
        {
            return "BitmaskSplitterPayload { " +
                "Low = " + Low + ", " +
                "High = " + High + " " +
            "}";
        }
    }

    /// <summary>
    /// Represents the payload of the StartPulse register.
    /// </summary>
    public struct StartPulsePayload
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StartPulsePayload"/> structure.
        /// </summary>
        /// <param name="digitalOutput"></param>
        /// <param name="pulseWidth"></param>
        public StartPulsePayload(
            PwmPort digitalOutput,
            ushort pulseWidth)
        {
            DigitalOutput = digitalOutput;
            PulseWidth = pulseWidth;
        }

        /// <summary>
        /// 
        /// </summary>
        public PwmPort DigitalOutput;

        /// <summary>
        /// 
        /// </summary>
        public ushort PulseWidth;

        /// <summary>
        /// Returns a <see cref="string"/> that represents the payload of
        /// the StartPulse register.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the payload of the
        /// StartPulse register.
        /// </returns>
        public override string ToString()
        {
            return "StartPulsePayload { " +
                "DigitalOutput = " + DigitalOutput + ", " +
                "PulseWidth = " + PulseWidth + " " +
            "}";
        }
    }

    /// <summary>
    /// Represents the payload of the StartPulseTrain register.
    /// </summary>
    public struct StartPulseTrainPayload
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StartPulseTrainPayload"/> structure.
        /// </summary>
        /// <param name="digitalOutput"></param>
        /// <param name="pulseWidth"></param>
        /// <param name="frequency"></param>
        /// <param name="pulseCount"></param>
        public StartPulseTrainPayload(
            PwmPort digitalOutput,
            ushort pulseWidth,
            byte frequency,
            byte pulseCount)
        {
            DigitalOutput = digitalOutput;
            PulseWidth = pulseWidth;
            Frequency = frequency;
            PulseCount = pulseCount;
        }

        /// <summary>
        /// 
        /// </summary>
        public PwmPort DigitalOutput;

        /// <summary>
        /// 
        /// </summary>
        public ushort PulseWidth;

        /// <summary>
        /// 
        /// </summary>
        public byte Frequency;

        /// <summary>
        /// 
        /// </summary>
        public byte PulseCount;

        /// <summary>
        /// Returns a <see cref="string"/> that represents the payload of
        /// the StartPulseTrain register.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents the payload of the
        /// StartPulseTrain register.
        /// </returns>
        public override string ToString()
        {
            return "StartPulseTrainPayload { " +
                "DigitalOutput = " + DigitalOutput + ", " +
                "PulseWidth = " + PulseWidth + ", " +
                "Frequency = " + Frequency + ", " +
                "PulseCount = " + PulseCount + " " +
            "}";
        }
    }

    /// <summary>
    /// 
    /// </summary>
    [Flags]
    public enum PortDigitalIOS : ushort
    {
        None = 0x0,
        DIO0 = 0x1,
        DIO1 = 0x2,
        DIO2 = 0x4,
        DIO3 = 0x400
    }

    /// <summary>
    /// 
    /// </summary>
    public enum PwmPort : byte
    {
        Pwm0 = 1,
        Pwm1 = 2,
        Pwm2 = 4,
        Pwm3 = 10
    }

    internal static partial class PayloadMarshal
    {
        internal static T[] GetSubArray<T>(T[] array, int offset, int count)
        {
            var result = new T[count];
            Array.Copy(array, offset, result, 0, count);
            return result;
        }

        internal static byte ReadByte(ArraySegment<byte> segment) => segment.Array[segment.Offset];

        internal static sbyte ReadSByte(ArraySegment<byte> segment) => (sbyte)segment.Array[segment.Offset];

        internal static ushort ReadUInt16(ArraySegment<byte> segment) => BitConverter.ToUInt16(segment.Array, segment.Offset);

        internal static short ReadInt16(ArraySegment<byte> segment) => BitConverter.ToInt16(segment.Array, segment.Offset);

        internal static uint ReadUInt32(ArraySegment<byte> segment) => BitConverter.ToUInt32(segment.Array, segment.Offset);

        internal static int ReadInt32(ArraySegment<byte> segment) => BitConverter.ToInt32(segment.Array, segment.Offset);

        internal static ulong ReadUInt64(ArraySegment<byte> segment) => BitConverter.ToUInt64(segment.Array, segment.Offset);

        internal static long ReadInt64(ArraySegment<byte> segment) => BitConverter.ToInt64(segment.Array, segment.Offset);

        internal static float ReadSingle(ArraySegment<byte> segment) => BitConverter.ToSingle(segment.Array, segment.Offset);

        internal static string ReadUtf8String(ArraySegment<byte> segment)
        {
            var count = Array.IndexOf(segment.Array, (byte)0, segment.Offset, segment.Count) - segment.Offset;
            return System.Text.Encoding.UTF8.GetString(segment.Array, segment.Offset, count < 0 ? segment.Count : count);
        }

        internal static void Write(ArraySegment<byte> segment, byte value) => segment.Array[segment.Offset] = value;

        internal static void Write(ArraySegment<byte> segment, sbyte value) => segment.Array[segment.Offset] = (byte)value;

        internal static void Write(ArraySegment<byte> segment, ushort value)
        {
            segment.Array[segment.Offset] = (byte)value;
            segment.Array[segment.Offset + 1] = (byte)(value >> 8);
        }

        internal static void Write(ArraySegment<byte> segment, short value)
        {
            segment.Array[segment.Offset] = (byte)value;
            segment.Array[segment.Offset + 1] = (byte)(value >> 8);
        }

        internal static void Write(ArraySegment<byte> segment, uint value)
        {
            segment.Array[segment.Offset] = (byte)value;
            segment.Array[segment.Offset + 1] = (byte)(value >> 8);
            segment.Array[segment.Offset + 2] = (byte)(value >> 16);
            segment.Array[segment.Offset + 3] = (byte)(value >> 24);
        }

        internal static void Write(ArraySegment<byte> segment, int value)
        {
            segment.Array[segment.Offset] = (byte)value;
            segment.Array[segment.Offset + 1] = (byte)(value >> 8);
            segment.Array[segment.Offset + 2] = (byte)(value >> 16);
            segment.Array[segment.Offset + 3] = (byte)(value >> 24);
        }

        internal static void Write(ArraySegment<byte> segment, ulong value)
        {
            segment.Array[segment.Offset] = (byte)value;
            segment.Array[segment.Offset + 1] = (byte)(value >> 8);
            segment.Array[segment.Offset + 2] = (byte)(value >> 16);
            segment.Array[segment.Offset + 3] = (byte)(value >> 24);
            segment.Array[segment.Offset + 4] = (byte)(value >> 32);
            segment.Array[segment.Offset + 5] = (byte)(value >> 40);
            segment.Array[segment.Offset + 6] = (byte)(value >> 48);
            segment.Array[segment.Offset + 7] = (byte)(value >> 56);
        }

        internal static void Write(ArraySegment<byte> segment, long value)
        {
            segment.Array[segment.Offset] = (byte)value;
            segment.Array[segment.Offset + 1] = (byte)(value >> 8);
            segment.Array[segment.Offset + 2] = (byte)(value >> 16);
            segment.Array[segment.Offset + 3] = (byte)(value >> 24);
            segment.Array[segment.Offset + 4] = (byte)(value >> 32);
            segment.Array[segment.Offset + 5] = (byte)(value >> 40);
            segment.Array[segment.Offset + 6] = (byte)(value >> 48);
            segment.Array[segment.Offset + 7] = (byte)(value >> 56);
        }

        internal static unsafe void Write(ArraySegment<byte> segment, float value) => Write(segment, *(int*)&value);

        internal static unsafe void Write(ArraySegment<byte> segment, string value) =>
            System.Text.Encoding.UTF8.GetBytes(value, 0, Math.Min(value.Length, segment.Count), segment.Array, segment.Offset);

        internal static void Write<T>(ArraySegment<byte> segment, T[] values) where T : unmanaged
        {
            Buffer.BlockCopy(values, 0, segment.Array, segment.Offset, segment.Count);
        }

        internal static void Write<T>(ArraySegment<T> segment, T[] values)
        {
            Array.Copy(values, 0, segment.Array, segment.Offset, segment.Count);
        }
    }
}
