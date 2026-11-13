# This file was automatically generated and should not be edited directly.
# To make changes, edit the device metadata and regenerate the interface.

from typing import Any, ClassVar

import numpy as np
from harp.protocol import (
    AnonymousPayload,
    BitMask,
    GroupMask,
    PayloadType,
    RegisterBase,
)
from harp.device.core import (
    EnableFlag,
    ResetFlags,
    REGISTER_MAP as _CORE_REGISTER_MAP,
)


__all__ = [
    "DEVICE_NAME",
    "WHO_AM_I",
    "EnableFlowPayload",
    "ResetFlowPayload",
    "EnableFlow",
    "ResetFlow",
    "REGISTER_MAP",
]

DEVICE_NAME: str = "CoreMasks"
WHO_AM_I: int = 1234


class EnableFlowPayload(AnonymousPayload[np.uint8]):
    """Represents the EnableFlow payload."""

    __value__: EnableFlag = GroupMask(enum=EnableFlag, mask=0xFF)


class ResetFlowPayload(AnonymousPayload[np.uint8]):
    """Represents the ResetFlow payload."""

    __value__: ResetFlags = BitMask(enum=ResetFlags)


class EnableFlow(RegisterBase[EnableFlag]):
    """Specifies whether the flow is enabled."""

    address: ClassVar[int] = 32
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = EnableFlowPayload


class ResetFlow(RegisterBase[ResetFlags]):
    """Specifies the reset behavior of the flow controller."""

    address: ClassVar[int] = 33
    payload_type: ClassVar[PayloadType] = PayloadType.U8
    payload_class = ResetFlowPayload


REGISTER_MAP: dict[int, type[RegisterBase[Any]]] = {
    **_CORE_REGISTER_MAP,
    32: EnableFlow,
    33: ResetFlow,
}
