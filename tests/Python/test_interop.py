"""Cross-stack interop test.

The .NET test ``PythonInteropTests.GenerateInteropFixtures`` drives the auto-generated C#
device interface to produce, for each register, a standard Harp binary file of frames plus a
manifest of the expected decoded values. This test reads those binaries back through the
auto-generated Python interface and asserts that every register the C# interface produces is
consumed correctly by the Python interface, closing the loop across the acquisition to
analysis stack.
"""

import enum
import json
import os
import sys
from dataclasses import is_dataclass
from pathlib import Path

import numpy as np
import pytest

from harp.protocol import HarpMessage
from harp.protocol._payload import PayloadBase


def _output_directory() -> Path:
    configured = os.environ.get("HARP_INTEROP_OUTPUT")
    if configured:
        return Path(configured)
    repository_root = Path(__file__).resolve().parents[2]
    return repository_root / "artifacts" / "python-interop"


OUTPUT_DIRECTORY = _output_directory()
sys.path.insert(0, str(OUTPUT_DIRECTORY))


def _load_manifest() -> list:
    manifest_path = OUTPUT_DIRECTORY / "manifest.json"
    if not manifest_path.exists():
        pytest.skip(f"Interop fixtures not found at {OUTPUT_DIRECTORY}; run the .NET fixture test first.")
    with open(manifest_path, encoding="utf-8") as manifest_file:
        return json.load(manifest_file)


def _split_frames(buffer: bytes) -> list:
    frames = []
    offset = 0
    while offset < len(buffer):
        # Harp framing: byte 1 is the length of everything after the first two bytes.
        length = buffer[offset + 1] + 2
        frames.append(buffer[offset:offset + length])
        offset += length
    return frames


def _canonical(value) -> object:
    if isinstance(value, PayloadBase):
        return {name: _canonical(getattr(value, name)) for name in type(value)._repr_fields}
    if isinstance(value, (enum.IntFlag, enum.IntEnum)):
        return int(value)
    if isinstance(value, bool):
        return 1 if value else 0
    if isinstance(value, np.ndarray):
        return [_canonical(element) for element in value.tolist()]
    if isinstance(value, (np.floating, float)):
        return round(float(value), 3)
    if isinstance(value, (np.integer, int)):
        return int(value)
    if isinstance(value, (bytes, bytearray)):
        return bytes(value).rstrip(b"\x00").decode("ascii")
    if isinstance(value, str):
        return value
    if is_dataclass(value):
        return [int(getattr(value, "major")), int(getattr(value, "minor"))]
    raise TypeError(f"Unhandled value type for canonicalization: {type(value)!r}")


@pytest.fixture(scope="module")
def device_module():
    import harp_device as device
    return device


@pytest.mark.parametrize("entry", _load_manifest(), ids=lambda entry: entry["name"])
def test_register_round_trips_from_csharp(device_module, entry):
    register = getattr(device_module, entry["name"])
    binary_path = OUTPUT_DIRECTORY / "data" / f"{entry['name']}.bin"
    buffer = binary_path.read_bytes()
    frames = _split_frames(buffer)

    assert len(frames) == entry["frames"]

    # Bulk parse path: the whole binary parses into one payload record per frame.
    _, _, _, payload = register.parse_bulk(buffer, parse_timestamp=False)
    assert len(payload) == entry["frames"]

    # Single message path: each frame decodes to the value the C# interface encoded.
    for frame, expected in zip(frames, entry["expected"]):
        message = HarpMessage.parse(frame)
        parsed = register.parse(message)
        assert _canonical(parsed) == expected
