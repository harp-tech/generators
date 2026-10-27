# device.md

- Name: Tests
- WhoAmI: 0
- Version: 0.1

# registers.md

| name | address | type | length | access | description | range | interfaceType |
|-|-|-|-|-|-|-|-|
| [DigitalInputs](xref:Harp.Generators.Tests.DigitalInputs) | 32 | U8 |  | Event |  |  |  |
| [AnalogData](xref:Harp.Generators.Tests.AnalogData) | 33 | Float | 6 | Event |  |  | [AnalogDataPayload](xref:Harp.Generators.Tests.AnalogDataPayload) |
| [ComplexConfiguration](xref:Harp.Generators.Tests.ComplexConfiguration) | 34 | U8 | 17 | Write |  |  | [ComplexConfigurationPayload](xref:Harp.Generators.Tests.ComplexConfigurationPayload) |
| [Version](xref:Harp.Generators.Tests.Version) | 35 | U8 | 32 | Event |  |  | [VersionPayload](xref:Harp.Generators.Tests.VersionPayload) |
| [CustomPayload](xref:Harp.Generators.Tests.CustomPayload) | 36 | U32 | 3 | Read |  |  | [HarpVersion](xref:Bonsai.Harp.HarpVersion) |
| [CustomRawPayload](xref:Harp.Generators.Tests.CustomRawPayload) | 37 | U32 | 3 | Read |  |  | [HarpVersion](xref:Bonsai.Harp.HarpVersion) |
| [CustomMemberConverter](xref:Harp.Generators.Tests.CustomMemberConverter) | 38 | U8 | 3 | Read |  |  | [CustomMemberConverterPayload](xref:Harp.Generators.Tests.CustomMemberConverterPayload) |
| [BitmaskSplitter](xref:Harp.Generators.Tests.BitmaskSplitter) | 39 | U8 |  | Write |  |  | [BitmaskSplitterPayload](xref:Harp.Generators.Tests.BitmaskSplitterPayload) |
| [Counter0](xref:Harp.Generators.Tests.Counter0) | 40 | S32 |  | Event |  |  |  |
| [PortDIOSet](xref:Harp.Generators.Tests.PortDIOSet) | 41 | U8 |  | Write |  |  | [PortDigitalIOS](xref:Harp.Generators.Tests.PortDigitalIOS) |
| [PulseDOPort0](xref:Harp.Generators.Tests.PulseDOPort0) | 42 | U16 |  | Write |  | [1:] |  |
| [PulseDO0](xref:Harp.Generators.Tests.PulseDO0) | 43 | U16 |  | Write |  | [1:] |  |
| [MultiElementPayload](xref:Harp.Generators.Tests.MultiElementPayload) | 44 | U16 | 3 | Write |  |  |  |
| [SingleElementPayload](xref:Harp.Generators.Tests.SingleElementPayload) | 45 | U16 | 1 | Write |  |  |  |
| [MixedMemberLength](xref:Harp.Generators.Tests.MixedMemberLength) | 46 | U8 | 4 | Write |  |  | [MixedMemberLengthPayload](xref:Harp.Generators.Tests.MixedMemberLengthPayload) |
| [Rgb0](xref:Harp.Generators.Tests.Rgb0) | 47 | U8 | 3 | Write |  |  | [RgbPayload](xref:Harp.Generators.Tests.RgbPayload) |
| [Rgb1](xref:Harp.Generators.Tests.Rgb1) | 48 | U8 | 3 | Write |  |  | [RgbPayload](xref:Harp.Generators.Tests.RgbPayload) |
| [StartPulse](xref:Harp.Generators.Tests.StartPulse) | 100 | U16 |  | Write | Starts a PWM pulse. |  | [StartPulsePayload](xref:Harp.Generators.Tests.StartPulsePayload) |
| [StartPulseTrain](xref:Harp.Generators.Tests.StartPulseTrain) | 101 | U16 | 2 | Write | Starts a PWM pulse train. |  | [StartPulseTrainPayload](xref:Harp.Generators.Tests.StartPulseTrainPayload) |
| [EncoderMode](xref:Harp.Generators.Tests.EncoderMode) | 103 | U8 |  | Write | Configures the operation mode of the encoder. |  | [EncoderModeMask](xref:Harp.Generators.Tests.EncoderModeMask) |

# Harp_Generators_Tests_DigitalInputs.md

---
uid: Harp.Generators.Tests.DigitalInputs
generatedContent: *content
---

- Address: 32
- Payload type: U8
- Access: Event

# Harp_Generators_Tests_AnalogData.md

---
uid: Harp.Generators.Tests.AnalogData
generatedContent: *content
---

- Address: 33
- Payload type: Float
- Length: 6
- Access: Event
- Interface type: [AnalogDataPayload](xref:Harp.Generators.Tests.AnalogDataPayload)

### Payload members

| name | offset | length | mask | range | interfaceType | description |
|-|-|-|-|-|-|-|
| Analog0 | 0 |  |  |  |  |  |
| Analog1 | 1 |  |  |  |  |  |
| Analog2 | 2 |  |  |  |  |  |
| Accelerometer | 3 | 3 |  |  |  |  |

# Harp_Generators_Tests_ComplexConfiguration.md

---
uid: Harp.Generators.Tests.ComplexConfiguration
generatedContent: *content
---

- Address: 34
- Payload type: U8
- Length: 17
- Access: Write
- Interface type: [ComplexConfigurationPayload](xref:Harp.Generators.Tests.ComplexConfigurationPayload)

### Payload members

| name | offset | length | mask | range | interfaceType | description |
|-|-|-|-|-|-|-|
| PwmPort | 0 |  |  |  | [PwmPort](xref:Harp.Generators.Tests.PwmPort) |  |
| Invert | 1 |  |  | 1 | bool |  |
| DutyCycle | 4 | 4 |  |  | float |  |
| Frequency | 8 | 4 |  |  | float |  |
| EventsEnabled | 12 |  |  |  | bool |  |
| Delta | 13 | 4 |  |  | uint |  |

# Harp_Generators_Tests_Version.md

---
uid: Harp.Generators.Tests.Version
generatedContent: *content
---

- Address: 35
- Payload type: U8
- Length: 32
- Access: Event
- Interface type: [VersionPayload](xref:Harp.Generators.Tests.VersionPayload)

### Payload members

| name | offset | length | mask | range | interfaceType | description |
|-|-|-|-|-|-|-|
| ProtocolVersion | 0 | 3 |  |  | [HarpVersion](xref:Bonsai.Harp.HarpVersion) |  |
| FirmwareVersion | 3 | 3 |  |  | [HarpVersion](xref:Bonsai.Harp.HarpVersion) |  |
| HardwareVersion | 6 | 3 |  |  | [HarpVersion](xref:Bonsai.Harp.HarpVersion) |  |
| CoreId | 9 | 3 |  |  | string |  |
| InterfaceHash | 12 | 20 |  |  |  |  |

# Harp_Generators_Tests_CustomPayload.md

---
uid: Harp.Generators.Tests.CustomPayload
generatedContent: *content
---

- Address: 36
- Payload type: U32
- Length: 3
- Access: Read
- Interface type: [HarpVersion](xref:Bonsai.Harp.HarpVersion)

# Harp_Generators_Tests_CustomRawPayload.md

---
uid: Harp.Generators.Tests.CustomRawPayload
generatedContent: *content
---

- Address: 37
- Payload type: U32
- Length: 3
- Access: Read
- Interface type: [HarpVersion](xref:Bonsai.Harp.HarpVersion)

# Harp_Generators_Tests_CustomMemberConverter.md

---
uid: Harp.Generators.Tests.CustomMemberConverter
generatedContent: *content
---

- Address: 38
- Payload type: U8
- Length: 3
- Access: Read
- Interface type: [CustomMemberConverterPayload](xref:Harp.Generators.Tests.CustomMemberConverterPayload)

### Payload members

| name | offset | length | mask | range | interfaceType | description |
|-|-|-|-|-|-|-|
| Header | 0 |  |  |  |  |  |
| Data | 1 | 2 |  |  | int |  |

# Harp_Generators_Tests_BitmaskSplitter.md

---
uid: Harp.Generators.Tests.BitmaskSplitter
generatedContent: *content
---

- Address: 39
- Payload type: U8
- Access: Write
- Interface type: [BitmaskSplitterPayload](xref:Harp.Generators.Tests.BitmaskSplitterPayload)

### Payload members

| name | offset | length | mask | range | interfaceType | description |
|-|-|-|-|-|-|-|
| Low |  |  | 0xF |  | int |  |
| High |  |  | 0xF0 |  | int |  |

# Harp_Generators_Tests_Counter0.md

---
uid: Harp.Generators.Tests.Counter0
generatedContent: *content
---

- Address: 40
- Payload type: S32
- Access: Event

# Harp_Generators_Tests_PortDIOSet.md

---
uid: Harp.Generators.Tests.PortDIOSet
generatedContent: *content
---

- Address: 41
- Payload type: U8
- Access: Write
- Interface type: [PortDigitalIOS](xref:Harp.Generators.Tests.PortDigitalIOS)

# Harp_Generators_Tests_PulseDOPort0.md

---
uid: Harp.Generators.Tests.PulseDOPort0
generatedContent: *content
---

- Address: 42
- Payload type: U16
- Access: Write
- Range: [1:]

# Harp_Generators_Tests_PulseDO0.md

---
uid: Harp.Generators.Tests.PulseDO0
generatedContent: *content
---

- Address: 43
- Payload type: U16
- Access: Write
- Range: [1:]

# Harp_Generators_Tests_MultiElementPayload.md

---
uid: Harp.Generators.Tests.MultiElementPayload
generatedContent: *content
---

- Address: 44
- Payload type: U16
- Length: 3
- Access: Write

# Harp_Generators_Tests_SingleElementPayload.md

---
uid: Harp.Generators.Tests.SingleElementPayload
generatedContent: *content
---

- Address: 45
- Payload type: U16
- Length: 1
- Access: Write

# Harp_Generators_Tests_MixedMemberLength.md

---
uid: Harp.Generators.Tests.MixedMemberLength
generatedContent: *content
---

- Address: 46
- Payload type: U8
- Length: 4
- Access: Write
- Interface type: [MixedMemberLengthPayload](xref:Harp.Generators.Tests.MixedMemberLengthPayload)

### Payload members

| name | offset | length | mask | range | interfaceType | description |
|-|-|-|-|-|-|-|
| Absent | 0 |  |  |  |  |  |
| Single | 1 | 1 |  |  |  |  |
| Multiple | 2 | 2 |  |  |  |  |

# Harp_Generators_Tests_Rgb0.md

---
uid: Harp.Generators.Tests.Rgb0
generatedContent: *content
---

- Address: 47
- Payload type: U8
- Length: 3
- Access: Write
- Interface type: [RgbPayload](xref:Harp.Generators.Tests.RgbPayload)

### Payload members

| name | offset | length | mask | range | interfaceType | description |
|-|-|-|-|-|-|-|
| Red | 0 |  |  |  |  |  |
| Green | 1 |  |  |  |  |  |
| Blue | 2 |  |  |  |  |  |

# Harp_Generators_Tests_Rgb1.md

---
uid: Harp.Generators.Tests.Rgb1
generatedContent: *content
---

- Address: 48
- Payload type: U8
- Length: 3
- Access: Write
- Interface type: [RgbPayload](xref:Harp.Generators.Tests.RgbPayload)

### Payload members

| name | offset | length | mask | range | interfaceType | description |
|-|-|-|-|-|-|-|
| Red | 0 |  |  |  |  |  |
| Green | 1 |  |  |  |  |  |
| Blue | 2 |  |  |  |  |  |

# Harp_Generators_Tests_StartPulse.md

---
uid: Harp.Generators.Tests.StartPulse
generatedContent: *content
---

- Address: 100
- Payload type: U16
- Access: Write
- Interface type: [StartPulsePayload](xref:Harp.Generators.Tests.StartPulsePayload)

### Payload members

| name | offset | length | mask | range | interfaceType | description |
|-|-|-|-|-|-|-|
| DigitalOutput | 0 |  | 0xC00 |  | [PwmPort](xref:Harp.Generators.Tests.PwmPort) |  |
| PulseWidth | 0 |  | 0x3FF |  | ushort |  |

# Harp_Generators_Tests_StartPulseTrain.md

---
uid: Harp.Generators.Tests.StartPulseTrain
generatedContent: *content
---

- Address: 101
- Payload type: U16
- Length: 2
- Access: Write
- Interface type: [StartPulseTrainPayload](xref:Harp.Generators.Tests.StartPulseTrainPayload)

### Payload members

| name | offset | length | mask | range | interfaceType | description |
|-|-|-|-|-|-|-|
| DigitalOutput | 0 |  | 0xC00 |  | [PwmPort](xref:Harp.Generators.Tests.PwmPort) |  |
| PulseWidth | 0 |  | 0x3FF |  | ushort |  |
| Frequency | 1 |  | 0xFF00 | 1 [1:255] | byte |  |
| PulseCount | 1 |  | 0xFF |  | byte |  |

# Harp_Generators_Tests_EncoderMode.md

---
uid: Harp.Generators.Tests.EncoderMode
generatedContent: *content
---

- Address: 103
- Payload type: U8
- Access: Write
- Interface type: [EncoderModeMask](xref:Harp.Generators.Tests.EncoderModeMask)
