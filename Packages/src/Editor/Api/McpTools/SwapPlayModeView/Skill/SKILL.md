---
name: uloop-swap-play-mode-view
description: "Switch the primary play mode view between Game View and Device Simulator. Use when you need to: (1) Activate Game View as the main play view, (2) Activate Device Simulator as the main play view. Works in both EditMode and PlayMode."
toolName: swap-play-mode-view
---

# uloop swap-play-mode-view

Switch the primary play mode view between Game View and Device Simulator.

This tool works in **both EditMode and PlayMode**.

## Usage

```bash
uloop swap-play-mode-view [--target-view <view>]
```

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `--target-view` | enum | `GameView` | Target view to make primary: `GameView` - activate Game View, `Simulator` - activate Device Simulator |

## Global Options

| Option | Description |
|--------|-------------|
| `--project-path <path>` | Optional. Use only when the target Unity project is not the current directory. |

## Examples

```bash
# Switch to Game View
uloop swap-play-mode-view --target-view GameView

# Switch to Device Simulator
uloop swap-play-mode-view --target-view Simulator
```

## Output

Returns JSON with:
- `Success` (boolean): Whether the swap succeeded
- `Message` (string): Description of the result or error details
- `ActiveViewType` (string | null): The view type that is now active: `"GameView"` or `"Simulator"`

## Notes

- If the requested view is already active, returns `Success=true` with `Message="Already active: {type}"` (no-op).
- Requires the Device Simulator module (Unity 2021.2 or later) when targeting `Simulator`.
- A Game View or Device Simulator window must already be open in the Editor.
- The swap persists across Editor sessions (stored in Library/PlayModeViewStates).
