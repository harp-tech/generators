# This file was automatically generated and should not be edited directly.
# To make changes, edit the device metadata and regenerate the interface.

import enum
from typing import Any, ClassVar

import numpy as np
from numpy.typing import NDArray
from harp.protocol import (
    AnonymousPayload,
    ArrayConverter,
    BitMask,
    BoolConverter,
    Field,
    GroupMask,
    HarpVersion,
    HarpVersionConverter,
    IdentityConverter,
    PayloadType,
    RegisterBase,
    RegisterS32,
    RegisterU16Array,
    RegisterU8,
    StringConverter,
    StructPayload,
)
from harp.device.core import REGISTER_MAP as _CORE_REGISTER_MAP

from .converters import (
    DataConverter,
    EnabledConverter,
    LowConverter,
    ModeConverter,
)


__all__ = [
    "DEVICE_NAME",
    "WHO_AM_I",
    "PortDigitalIOS",
    "DigitalChannels",
    "WideStatusFlags",
    "PwmPort",
    "EncoderReading",
    "AnalogDataPayload",
    "ComplexConfigurationPayload",
    "VersionPayload",
    "CustomPayloadPayload",
    "CustomRawPayloadPayload",
    "CustomMemberConverterPayload",
    "BitmaskSplitterPayload",
    "PortDIOSetPayload",
    "PulseDOPort0Payload",
    "PulseDO0Payload",
    "MixedMemberLengthPayload",
    "RgbPayload",
    "StartPulsePayload",
    "StartPulseTrainPayload",
    "EncoderModePayload",
    "LedEnabledPayload",
    "PortDIOClearPayload",
    "WideStatusPayload",
    "ThresholdPayload",
    "DigitalInputs",
    "AnalogData",
    "ComplexConfiguration",
    "Version",
    "CustomPayload",
    "CustomRawPayload",
    "CustomMemberConverter",
    "BitmaskSplitter",
    "Counter0",
    "PortDIOSet",
    "PulseDOPort0",
    "PulseDO0",
    "MultiElementPayload",
    "SingleElementPayload",
    "MixedMemberLength",
    "Rgb0",
    "Rgb1",
    "StartPulse",
    "StartPulseTrain",
    "EncoderMode",
    "LedEnabled",
    "PortDIOClear",
    "WideStatus",
    "Threshold",
    "REGISTER_MAP",
]

DEVICE_NAME: str = "Tests"
WHO_AM_I: int = 0


class PortDigitalIOS(enum.IntFlag):
    DIO0 = 0x1
    DIO1 = 0x2
    DIO2 = 0x4
    DIO3 = 0x8
    DI_PORT0 = 0x100
    TEST_DI_PORT1 = 0x200
    SUPPLY_PORT0 = 0x400
    PORT_DIO1 = 0x800


class DigitalChannels(enum.IntFlag):
    CHANNEL0 = 0x1
    CHANNEL1 = 0x2
    CHANNEL2 = 0x4
    CHANNEL3 = 0x8


class WideStatusFlags(enum.IntFlag):
    READY = 0x1
    OVERFLOW = 0x80000000


class PwmPort(enum.IntEnum):
    PWM0 = 1
    PWM1 = 2
    PWM2 = 4
    PWM3 = 10


class EncoderReading(enum.IntEnum):
    """Specifies the type of encoder mode."""

    POSITION = 0
    DISPLACEMENT = 1


class AnalogDataPayload(StructPayload[np.float32], length=6):
    """Represents the payload of the AnalogData register."""

    analog0: np.float32 = Field(IdentityConverter(np.float32))
    analog1: np.float32 = Field(IdentityConverter(np.float32), offset=1)
    analog2: np.float32 = Field(IdentityConverter(np.float32), offset=2)
    accelerometer: NDArray[np.float32] = Field(ArrayConverter(np.float32, 3), offset=3)


class ComplexConfigurationPayload(StructPayload[np.uint8], length=17):
    """Represents the payload of the ComplexConfiguration register."""

    pwm_port: PwmPort = GroupMask(enum=PwmPort, mask=0xFF, default=PwmPort.PWM1)
    invert: bool = Field(BoolConverter(), offset=1, default=True)
    input_flags: DigitalChannels = BitMask(enum=DigitalChannels, mask=0xF, offset=2, default=DigitalChannels.CHANNEL0 | DigitalChannels.CHANNEL1)
    output_flags: DigitalChannels = BitMask(enum=DigitalChannels, offset=3, default=DigitalChannels(19))
    duty_cycle: np.float32 = Field(IdentityConverter(np.float32), offset=4)
    frequency: np.float32 = Field(IdentityConverter(np.float32), offset=8)
    events_enabled: bool = Field(BoolConverter(), offset=12)
    delta: np.uint32 = Field(IdentityConverter(np.uint32), offset=13, default=np.uint32(1000))


class VersionPayload(StructPayload[np.uint8], length=32):
    """Represents the payload of the Version register."""

    protocol_version: HarpVersion = Field(HarpVersionConverter(np.uint8))
    firmware_version: HarpVersion = Field(HarpVersionConverter(np.uint8), offset=3)
    hardware_version: HarpVersion = Field(HarpVersionConverter(np.uint8), offset=6)
    core_id: str = Field(StringConverter(3), offset=9)
    interface_hash: NDArray[np.uint8] = Field(ArrayConverter(np.uint8, 20), offset=12)


class CustomPayloadPayload(AnonymousPayload[np.uint32]):
    """Represents the payload of the CustomPayload register."""

    __value__: HarpVersion = Field(HarpVersionConverter(np.uint32))


class CustomRawPayloadPayload(AnonymousPayload[np.uint32]):
    """Represents the payload of the CustomRawPayload register."""

    __value__: HarpVersion = Field(HarpVersionConverter(np.uint32))


class CustomMemberConverterPayload(StructPayload[np.uint8], length=5):
    """Represents the payload of the CustomMemberConverter register."""

    header: np.uint8 = Field(IdentityConverter(np.uint8))
    data: np.int32 = Field(DataConverter(), offset=1)
    enabled: bool = Field(EnabledConverter(), offset=3)
    mode: np.int32 = Field(ModeConverter(), offset=4)


class BitmaskSplitterPayload(StructPayload[np.uint8]):
    """Represents the payload of the BitmaskSplitter register."""

    low: np.int32 = Field(LowConverter(), mask=0xF)
    high: np.int32 = Field(IdentityConverter(np.int32), mask=0xF0)


class PortDIOSetPayload(AnonymousPayload[np.uint16]):
    """Represents the payload of the PortDIOSet register."""

    __value__: PortDigitalIOS = BitMask(enum=PortDigitalIOS, default=PortDigitalIOS.DIO0 | PortDigitalIOS.DIO1)


class PulseDOPort0Payload(AnonymousPayload[np.uint16]):
    """Represents the payload of the PulseDOPort0 register."""

    __value__: np.uint16 = Field(IdentityConverter(np.uint16), default=np.uint16(500))


class PulseDO0Payload(AnonymousPayload[np.uint16]):
    """Represents the payload of the PulseDO0 register."""

    __value__: np.uint16 = Field(IdentityConverter(np.uint16), default=np.uint16(500))


class MixedMemberLengthPayload(StructPayload[np.uint8], length=4):
    """Represents the payload of the MixedMemberLength register."""

    absent: np.uint8 = Field(IdentityConverter(np.uint8))
    single: NDArray[np.uint8] = Field(ArrayConverter(np.uint8, 1), offset=1)
    multiple: NDArray[np.uint8] = Field(ArrayConverter(np.uint8, 2), offset=2)


class RgbPayload(StructPayload[np.uint8], length=3):
    """Represents the payload of the Rgb register."""

    red: np.uint8 = Field(IdentityConverter(np.uint8))
    green: np.uint8 = Field(IdentityConverter(np.uint8), offset=1)
    blue: np.uint8 = Field(IdentityConverter(np.uint8), offset=2)


class StartPulsePayload(StructPayload[np.uint16]):
    """Represents the payload of the StartPulse register."""

    digital_output: PwmPort = GroupMask(enum=PwmPort, mask=0xC00)
    pulse_width: np.uint16 = Field(IdentityConverter(np.uint16), mask=0x3FF)


class StartPulseTrainPayload(StructPayload[np.uint16], length=2):
    """Represents the payload of the StartPulseTrain register."""

    digital_output: PwmPort = GroupMask(enum=PwmPort, mask=0xC00)
    pulse_width: np.uint16 = Field(IdentityConverter(np.uint16), mask=0x3FF)
    frequency: np.uint8 = Field(IdentityConverter(np.uint8), mask=0xFF00, offset=1, default=np.uint8(1))
    pulse_count: np.uint8 = Field(IdentityConverter(np.uint8), mask=0xFF, offset=1)


class EncoderModePayload(AnonymousPayload[np.uint8]):
    """Represents the payload of the EncoderMode register."""

    __value__: EncoderReading = GroupMask(enum=EncoderReading, mask=0xFF, default=EncoderReading.DISPLACEMENT)


class LedEnabledPayload(AnonymousPayload[np.uint8]):
    """Represents the payload of the LedEnabled register."""

    __value__: bool = Field(BoolConverter(), default=True)


class PortDIOClearPayload(AnonymousPayload[np.uint16]):
    """Represents the payload of the PortDIOClear register."""

    __value__: PortDigitalIOS = BitMask(enum=PortDigitalIOS, default=PortDigitalIOS(0))


class WideStatusPayload(AnonymousPayload[np.uint32]):
    """Represents the payload of the WideStatus register."""

    __value__: WideStatusFlags = BitMask(enum=WideStatusFlags)


class ThresholdPayload(AnonymousPayload[np.uint32]):
    """Represents the payload of the Threshold register."""

    __value__: np.uint32 = Field(IdentityConverter(np.uint32), default=np.uint32(16777217))


class DigitalInputs(RegisterU8):
    address: ClassVar[int] = 32


class AnalogData(RegisterBase[AnalogDataPayload]):
    address: ClassVar[int] = 33
    payload_type: ClassVar[PayloadType] = PayloadType.Float
    payload_class = AnalogDataPayload


class ComplexConfiguration(RegisterBase[ComplexConfigurationPayload]):
    address: ClassVar[int] = 34
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = ComplexConfigurationPayload


class Version(RegisterBase[VersionPayload]):
    address: ClassVar[int] = 35
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = VersionPayload


class CustomPayload(RegisterBase[HarpVersion]):
    address: ClassVar[int] = 36
    payload_type: ClassVar[PayloadType] = PayloadType.U32
    payload_class = CustomPayloadPayload


class CustomRawPayload(RegisterBase[HarpVersion]):
    address: ClassVar[int] = 37
    payload_type: ClassVar[PayloadType] = PayloadType.U32
    payload_class = CustomRawPayloadPayload


class CustomMemberConverter(RegisterBase[CustomMemberConverterPayload]):
    address: ClassVar[int] = 38
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = CustomMemberConverterPayload


class BitmaskSplitter(RegisterBase[BitmaskSplitterPayload]):
    address: ClassVar[int] = 39
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = BitmaskSplitterPayload


class Counter0(RegisterS32):
    address: ClassVar[int] = 40


class PortDIOSet(RegisterBase[PortDigitalIOS]):
    address: ClassVar[int] = 41
    payload_type: ClassVar[PayloadType] = PayloadType.U16
    payload_class = PortDIOSetPayload


class PulseDOPort0(RegisterBase[np.uint16]):
    address: ClassVar[int] = 42
    payload_type: ClassVar[PayloadType] = PayloadType.U16
    payload_class = PulseDOPort0Payload


class PulseDO0(RegisterBase[np.uint16]):
    address: ClassVar[int] = 43
    payload_type: ClassVar[PayloadType] = PayloadType.U16
    payload_class = PulseDO0Payload


class MultiElementPayload(RegisterU16Array):
    address: ClassVar[int] = 44
    length: int = 3


class SingleElementPayload(RegisterU16Array):
    address: ClassVar[int] = 45
    length: int = 1


class MixedMemberLength(RegisterBase[MixedMemberLengthPayload]):
    address: ClassVar[int] = 46
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = MixedMemberLengthPayload


class Rgb0(RegisterBase[RgbPayload]):
    address: ClassVar[int] = 47
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = RgbPayload


class Rgb1(RegisterBase[RgbPayload]):
    address: ClassVar[int] = 48
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = RgbPayload


class StartPulse(RegisterBase[StartPulsePayload]):
    """Starts a PWM pulse."""

    address: ClassVar[int] = 100
    payload_type: ClassVar[PayloadType] = PayloadType.U16
    payload_class = StartPulsePayload


class StartPulseTrain(RegisterBase[StartPulseTrainPayload]):
    """Starts a PWM pulse train."""

    address: ClassVar[int] = 101
    payload_type: ClassVar[PayloadType] = PayloadType.U16
    payload_class = StartPulseTrainPayload


class EncoderMode(RegisterBase[EncoderReading]):
    """Configures the operation mode of the encoder."""

    address: ClassVar[int] = 103
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = EncoderModePayload


class LedEnabled(RegisterBase[bool]):
    """Specifies whether the status LED is enabled."""

    address: ClassVar[int] = 104
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = LedEnabledPayload


class PortDIOClear(RegisterBase[PortDigitalIOS]):
    address: ClassVar[int] = 105
    payload_type: ClassVar[PayloadType] = PayloadType.U16
    payload_class = PortDIOClearPayload


class WideStatus(RegisterBase[WideStatusFlags]):
    address: ClassVar[int] = 106
    payload_type: ClassVar[PayloadType] = PayloadType.U32
    payload_class = WideStatusPayload


class Threshold(RegisterBase[np.uint32]):
    """Specifies a threshold whose default needs more than single precision."""

    address: ClassVar[int] = 107
    payload_type: ClassVar[PayloadType] = PayloadType.U32
    payload_class = ThresholdPayload


REGISTER_MAP: dict[int, type[RegisterBase[Any]]] = {
    **_CORE_REGISTER_MAP,
    32: DigitalInputs,
    33: AnalogData,
    34: ComplexConfiguration,
    35: Version,
    36: CustomPayload,
    37: CustomRawPayload,
    38: CustomMemberConverter,
    39: BitmaskSplitter,
    40: Counter0,
    41: PortDIOSet,
    42: PulseDOPort0,
    43: PulseDO0,
    44: MultiElementPayload,
    45: SingleElementPayload,
    46: MixedMemberLength,
    47: Rgb0,
    48: Rgb1,
    100: StartPulse,
    101: StartPulseTrain,
    103: EncoderMode,
    104: LedEnabled,
    105: PortDIOClear,
    106: WideStatus,
    107: Threshold,
}
