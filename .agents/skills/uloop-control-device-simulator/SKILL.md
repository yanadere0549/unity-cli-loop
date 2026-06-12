---
name: uloop-control-device-simulator
description: "Control the Unity Device Simulator: open the window, list devices, select a device, set rotation, or set zoom scale. Works in EditMode AND PlayMode. Use to configure and inspect the Device Simulator before taking screenshots or running touch-input tests."
---

# uloop control-device-simulator

Open, configure, and inspect the Unity Device Simulator (device, orientation, scale).

## Usage

```bash
uloop control-device-simulator [--action <action>] [--device-index <n>] [--device-name <name>] [--rotation <degrees>] [--scale <percent>] [--auto-open <bool>]
```

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `--action` | enum | `GetState` | Action to perform. See Actions table below. |
| `--device-index` | number | `-1` | Zero-based device index for `SelectDevice`. Takes priority over `--device-name` when >= 0. |
| `--device-name` | string | `""` | Device friendly-name substring for `SelectDevice` (case-insensitive). Used when `--device-index` is -1. |
| `--rotation` | number | `0` | Rotation in degrees for `SetRotation`. Must be `0`, `90`, `180`, or `270`. |
| `--scale` | number | `100` | Scale percentage for `SetScale` (10–100, where 100 = fit to window). |
| `--auto-open` | boolean | `true` | When `true`, automatically open the Simulator window if it is not already open. When `false`, return an error instead of auto-opening. |

## Actions

| Action | Value | Description |
|--------|-------|-------------|
| `GetState` | 0 | Report full simulator state (device, orientation, scale, screen size). |
| `Open` | 1 | Open or bring the Device Simulator window to the front. |
| `ListDevices` | 2 | Report the list of available devices. |
| `SelectDevice` | 3 | Switch to a device by `--device-index` or `--device-name` substring. |
| `SetRotation` | 4 | Set device orientation using `--rotation` (0 / 90 / 180 / 270). |
| `SetScale` | 5 | Set the zoom level using `--scale` (10–100). |

## AutoOpen Behavior

- `Action=Open` always calls `ShowWindow()` regardless of `--auto-open`.
- Other actions with `--auto-open true` (default): window is opened automatically if not already open, then the action proceeds.
- Other actions with `--auto-open false`: if the window is not open, the tool returns `Success=false` with a message to open the window first.

## Global Options

| Option | Description |
|--------|-------------|
| `--project-path <path>` | Optional. Use only when the target Unity project is not the current directory. |

## Examples

```bash
# Open the Device Simulator window
uloop control-device-simulator --action Open

# Get full current state (auto-opens if needed)
uloop control-device-simulator --action GetState

# List all available devices
uloop control-device-simulator --action ListDevices

# Select a device by index
uloop control-device-simulator --action SelectDevice --device-index 0

# Select a device by name substring (case-insensitive)
uloop control-device-simulator --action SelectDevice --device-name "iPhone 14"

# Rotate to landscape
uloop control-device-simulator --action SetRotation --rotation 90

# Reset rotation to portrait
uloop control-device-simulator --action SetRotation --rotation 0

# Set zoom to 50%
uloop control-device-simulator --action SetScale --scale 50

# Return error instead of auto-opening if window is not open
uloop control-device-simulator --action GetState --auto-open false
```

## Output

Returns JSON with:
- `Success` (boolean): Whether the action succeeded.
- `Message` (string): Human-readable result description or error message.
- `Action` (string): The action that was performed.
- `IsWindowOpen` (boolean): Whether the Device Simulator window is currently open.
- `CurrentDeviceIndex` (number | null): Zero-based index of the selected device.
- `CurrentDeviceName` (string | null): Friendly name of the selected device.
- `AllDeviceNames` (string[] | null): Full device list (present for `GetState`, `Open`, `ListDevices`, `SelectDevice`).
- `CurrentRotation` (number | null): Current rotation in degrees (0 / 90 / 180 / 270).
- `CurrentScale` (number | null): Current scale percentage (10–100).
- `ScreenWidth` (number | null): Simulated screen width in pixels.
- `ScreenHeight` (number | null): Simulated screen height in pixels.
- `OrientationName` (string | null): Current orientation name (e.g. `"Portrait"`, `"LandscapeLeft"`).

## Notes

- Works in **EditMode AND PlayMode**. Window control and device configuration are pure Editor API calls.
- Requires Unity 2021.2 or later with the Device Simulator built-in module available.
- After opening the window (`Action=Open`), the tool waits 2 frames for initialization before reading state.

## Typical Workflow

```bash
# 1. Open the Device Simulator window
uloop control-device-simulator --action Open

# 2. List available devices to find the index or name
uloop control-device-simulator --action ListDevices

# 3. Select the target device
uloop control-device-simulator --action SelectDevice --device-name "iPhone 14"

# 4. Set landscape orientation
uloop control-device-simulator --action SetRotation --rotation 90

# 5. Capture the Simulator window
uloop screenshot --window-name Simulator
```
