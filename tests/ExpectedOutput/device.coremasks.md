# device.md

- Name: CoreMasks
- WhoAmI: 1234
- Version: 0.1

# registers.md

| name | address | type | length | access | description | range | interfaceType |
|-|-|-|-|-|-|-|-|
| [EnableFlow](xref:Harp.Generators.Tests.EnableFlow) | 32 | U8 |  | Write | Specifies whether the flow is enabled. |  | [EnableFlag](xref:Bonsai.Harp.EnableFlag) |
| [ResetFlow](xref:Harp.Generators.Tests.ResetFlow) | 33 | U8 |  | Write | Specifies the reset behavior of the flow controller. |  | [ResetFlags](xref:Bonsai.Harp.ResetFlags) |

# Harp_Generators_Tests_EnableFlow.md

---
uid: Harp.Generators.Tests.EnableFlow
generatedContent: *content
---

- Address: 32
- Payload type: U8
- Access: Write
- Interface type: [EnableFlag](xref:Bonsai.Harp.EnableFlag)

# Harp_Generators_Tests_ResetFlow.md

---
uid: Harp.Generators.Tests.ResetFlow
generatedContent: *content
---

- Address: 33
- Payload type: U8
- Access: Write
- Interface type: [ResetFlags](xref:Bonsai.Harp.ResetFlags)
