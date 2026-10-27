# registers.md

| name | address | type | access | description | interfaceType |
|-|-|-|-|-|-|
| [WhoAmI](xref:Harp.Generators.Tests.WhoAmI) | 0 | U16 | Read | Specifies the identity class of the device. |  |
| [HardwareVersionHigh](xref:Harp.Generators.Tests.HardwareVersionHigh) | 1 | U8 | Read | Specifies the major hardware version of the device. |  |
| [HardwareVersionLow](xref:Harp.Generators.Tests.HardwareVersionLow) | 2 | U8 | Read | Specifies the minor hardware version of the device. |  |
| [AssemblyVersion](xref:Harp.Generators.Tests.AssemblyVersion) | 3 | U8 | Read | Specifies the version of the assembled components in the device. |  |
| [CoreVersionHigh](xref:Harp.Generators.Tests.CoreVersionHigh) | 4 | U8 | Read | Specifies the major version of the Harp core implemented by the device. |  |
| [CoreVersionLow](xref:Harp.Generators.Tests.CoreVersionLow) | 5 | U8 | Read | Specifies the minor version of the Harp core implemented by the device. |  |
| [FirmwareVersionHigh](xref:Harp.Generators.Tests.FirmwareVersionHigh) | 6 | U8 | Read | Specifies the major version of the Harp core implemented by the device. |  |
| [FirmwareVersionLow](xref:Harp.Generators.Tests.FirmwareVersionLow) | 7 | U8 | Read | Specifies the minor version of the Harp core implemented by the device. |  |
| [TimestampSeconds](xref:Harp.Generators.Tests.TimestampSeconds) | 8 | U32 | Read, Write, Event | Stores the integral part of the system timestamp, in seconds. |  |
| [TimestampMicroseconds](xref:Harp.Generators.Tests.TimestampMicroseconds) | 9 | U16 | Read | Stores the fractional part of the system timestamp, in microseconds. |  |
| [OperationControl](xref:Harp.Generators.Tests.OperationControl) | 10 | U8 | Write | Stores the configuration mode of the device. | [OperationControlPayload](xref:Harp.Generators.Tests.OperationControlPayload) |
| [ResetDevice](xref:Harp.Generators.Tests.ResetDevice) | 11 | U8 | Write | Resets the device and saves non-volatile registers. | [ResetFlags](xref:Harp.Generators.Tests.ResetFlags) |
| [DeviceName](xref:Harp.Generators.Tests.DeviceName) | 12 | U8[25] | Write | Stores the user-specified device name. | string |
| [SerialNumber](xref:Harp.Generators.Tests.SerialNumber) | 13 | U16 | Write | Specifies the unique serial number of the device. |  |
| [ClockConfiguration](xref:Harp.Generators.Tests.ClockConfiguration) | 14 | U8 | Write | Specifies the configuration for the device synchronization clock. | [ClockConfigurationFlags](xref:Harp.Generators.Tests.ClockConfigurationFlags) |

# Harp_Generators_Tests_WhoAmI.md

---
uid: Harp.Generators.Tests.WhoAmI
generatedContent: *content
---

- Address: 0
- Payload type: U16
- Access: Read

# Harp_Generators_Tests_HardwareVersionHigh.md

---
uid: Harp.Generators.Tests.HardwareVersionHigh
generatedContent: *content
---

- Address: 1
- Payload type: U8
- Access: Read

# Harp_Generators_Tests_HardwareVersionLow.md

---
uid: Harp.Generators.Tests.HardwareVersionLow
generatedContent: *content
---

- Address: 2
- Payload type: U8
- Access: Read

# Harp_Generators_Tests_AssemblyVersion.md

---
uid: Harp.Generators.Tests.AssemblyVersion
generatedContent: *content
---

- Address: 3
- Payload type: U8
- Access: Read

# Harp_Generators_Tests_CoreVersionHigh.md

---
uid: Harp.Generators.Tests.CoreVersionHigh
generatedContent: *content
---

- Address: 4
- Payload type: U8
- Access: Read

# Harp_Generators_Tests_CoreVersionLow.md

---
uid: Harp.Generators.Tests.CoreVersionLow
generatedContent: *content
---

- Address: 5
- Payload type: U8
- Access: Read

# Harp_Generators_Tests_FirmwareVersionHigh.md

---
uid: Harp.Generators.Tests.FirmwareVersionHigh
generatedContent: *content
---

- Address: 6
- Payload type: U8
- Access: Read

# Harp_Generators_Tests_FirmwareVersionLow.md

---
uid: Harp.Generators.Tests.FirmwareVersionLow
generatedContent: *content
---

- Address: 7
- Payload type: U8
- Access: Read

# Harp_Generators_Tests_TimestampSeconds.md

---
uid: Harp.Generators.Tests.TimestampSeconds
generatedContent: *content
---

- Address: 8
- Payload type: U32
- Access: Read, Write, Event

# Harp_Generators_Tests_TimestampMicroseconds.md

---
uid: Harp.Generators.Tests.TimestampMicroseconds
generatedContent: *content
---

- Address: 9
- Payload type: U16
- Access: Read

# Harp_Generators_Tests_OperationControl.md

---
uid: Harp.Generators.Tests.OperationControl
generatedContent: *content
---

- Address: 10
- Payload type: U8
- Access: Write
- Interface type: [OperationControlPayload](xref:Harp.Generators.Tests.OperationControlPayload)

### Payload members

| name | mask | interfaceType | description |
|-|-|-|-|
| OperationMode | 0x3 | [OperationMode](xref:Harp.Generators.Tests.OperationMode) | Specifies the operation mode of the device. |
| DumpRegisters | 0x8 | bool | Specifies whether the device should report the content of all registers on initialization. |
| MuteReplies | 0x10 | bool | Specifies whether the replies to all commands will be muted, i.e. not sent by the device. |
| VisualIndicators | 0x20 | [EnableFlag](xref:Harp.Generators.Tests.EnableFlag) | Specifies the state of all visual indicators on the device. |
| OperationLed | 0x40 | [EnableFlag](xref:Harp.Generators.Tests.EnableFlag) | Specifies whether the device state LED should report the operation mode of the device. |
| Heartbeat | 0x80 | [EnableFlag](xref:Harp.Generators.Tests.EnableFlag) | Specifies whether the device should report the content of the seconds register each second. |

# Harp_Generators_Tests_ResetDevice.md

---
uid: Harp.Generators.Tests.ResetDevice
generatedContent: *content
---

- Address: 11
- Payload type: U8
- Access: Write
- Interface type: [ResetFlags](xref:Harp.Generators.Tests.ResetFlags)

# Harp_Generators_Tests_DeviceName.md

---
uid: Harp.Generators.Tests.DeviceName
generatedContent: *content
---

- Address: 12
- Payload type: U8
- Length: 25
- Access: Write
- Interface type: string

# Harp_Generators_Tests_SerialNumber.md

---
uid: Harp.Generators.Tests.SerialNumber
generatedContent: *content
---

- Address: 13
- Payload type: U16
- Access: Write

# Harp_Generators_Tests_ClockConfiguration.md

---
uid: Harp.Generators.Tests.ClockConfiguration
generatedContent: *content
---

- Address: 14
- Payload type: U8
- Access: Write
- Interface type: [ClockConfigurationFlags](xref:Harp.Generators.Tests.ClockConfigurationFlags)
