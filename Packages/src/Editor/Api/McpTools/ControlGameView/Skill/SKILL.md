---
name: uloop-control-game-view
description: "List, select, or add Game View resolutions and report current Game View state. Use when you need to: (1) Check the current Game View size, (2) Switch to a specific resolution by index or label, (3) Add a custom fixed-resolution entry. Works in both EditMode and PlayMode."
toolName: control-game-view
---

# uloop control-game-view

List, select, or add Game View resolutions, and report current Game View state.

This tool works in **both EditMode and PlayMode**.

## Usage

```bash
uloop control-game-view [--action <action>] [--size-index <index>] [--size-label <label>] [--custom-width <width>] [--custom-height <height>] [--custom-label <label>]
```

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `--action` | enum | `GetState` | `GetState` or `ListSizes` - report state and all sizes; `SelectSize` - activate a size; `AddCustomSize` - add fixed-resolution entry |
| `--size-index` | number | `-1` | Zero-based index for SelectSize. Takes priority over `--size-label` when >= 0. |
| `--size-label` | string | `""` | Label substring for SelectSize (case-insensitive). Used when `--size-index` is -1. |
| `--custom-width` | number | `1920` | Width in pixels for AddCustomSize (1-7680). |
| `--custom-height` | number | `1080` | Height in pixels for AddCustomSize (1-4320). |
| `--custom-label` | string | `""` | Label for AddCustomSize. Must not be empty. |

## Global Options

| Option | Description |
|--------|-------------|
| `--project-path <path>` | Optional. Use only when the target Unity project is not the current directory. |

## Examples

```bash
# Report current state and list all sizes
uloop control-game-view --action GetState

# Select by index
uloop control-game-view --action SelectSize --size-index 0

# Select by label substring (case-insensitive)
uloop control-game-view --action SelectSize --size-label "1920x1080"

# Add a custom resolution
uloop control-game-view --action AddCustomSize --custom-width 1280 --custom-height 720 --custom-label "Test720p"
```

## Output

Returns JSON with:
- `Success` (boolean): Whether the operation succeeded
- `Message` (string): Description of the result or error details
- `Action` (string): The action that was performed
- `CurrentSizeIndex` (number | null): Currently selected size index
- `CurrentSizeLabel` (string | null): Currently selected size label
- `AllSizeLabels` (string[] | null): All available size display texts (populated by GetState, ListSizes, SelectSize)
- `NewSizeIndex` (number | null): Index of the newly added entry (populated by AddCustomSize)

## Notes

- `GetState` and `ListSizes` return identical data.
- `SelectSize` requires the Game View window to be open.
- When `--size-label` matches multiple entries, the first match is used and noted in `Message`.
- `AddCustomSize` rejects duplicate labels (case-insensitive exact match).
