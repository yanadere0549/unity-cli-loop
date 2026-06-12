---
name: uloop-simulate-touch-input
description: "Simulate touch input (tap, long-press, drag) in PlayMode via Input System Touchscreen. PlayMode required. Window control tools work in EditMode."
context: fork
---

# Task

Simulate touch input via Input System in Unity PlayMode: $ARGUMENTS

## Workflow

1. Ensure Unity is in PlayMode (use `uloop control-play-mode --action Play` if not)
2. Determine target screen coordinates (use `uloop screenshot` to find positions)
3. Execute the appropriate `uloop simulate-touch-input` command
4. Take a screenshot to verify the result: `uloop screenshot --capture-mode rendering`
5. Report what happened

## Tool Reference

```bash
uloop simulate-touch-input --action <action> [options]
```

### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `--action` | enum | `Tap` | `Tap`, `LongPress`, `Drag` |
| `--x` | number | `0` | Target X (top-left origin). For Drag, this is the endpoint X. |
| `--y` | number | `0` | Target Y (top-left origin). For Drag, this is the endpoint Y. |
| `--from-x` | number | `0` | Drag start X (top-left origin). Used by Drag only. |
| `--from-y` | number | `0` | Drag start Y (top-left origin). Used by Drag only. |
| `--touch-id` | number | `0` | Touch identifier (0-9). Use distinct values for concurrent touches. |
| `--duration` | number | `0` | Hold duration in seconds. Required (> 0) for LongPress. For Tap, 0 = one-shot. |
| `--drag-speed` | number | `500` | Drag interpolation speed in pixels per second. 0 = instant single move. |
| `--target-view` | enum | `GameView` | `GameView` or `Simulator`. Controls which coordinate space is used. |

### Actions

| Action | What it injects | Description |
|--------|----------------|-------------|
| `Tap` | Began → (optional hold) → Ended | Quick touch tap. Duration=0 fires Began and Ended in consecutive frames. |
| `LongPress` | Began → Stationary (each frame) → Ended | Press and hold for `--duration` seconds. Duration must be > 0. |
| `Drag` | Began at (FromX,FromY) → Moved (per-frame) → Ended at (X,Y) | Swipe or drag gesture at `--drag-speed` px/s. |

### Global Options (optional)

| Option | Description |
|--------|-------------|
| `--project-path <path>` | Optional. Use only when the target Unity project is not the current directory. |

## Examples

```bash
# Tap at screen center
uloop simulate-touch-input --action Tap --x 540 --y 960

# Tap and hold for 0.5 seconds
uloop simulate-touch-input --action Tap --x 540 --y 960 --duration 0.5

# Long-press for 2 seconds
uloop simulate-touch-input --action LongPress --x 540 --y 960 --duration 2.0

# Drag (swipe left) at default speed
uloop simulate-touch-input --action Drag --from-x 800 --from-y 960 --x 200 --y 960

# Drag at custom speed
uloop simulate-touch-input --action Drag --from-x 200 --from-y 540 --x 800 --y 540 --drag-speed 300

# Instant drag (single Moved event at endpoint)
uloop simulate-touch-input --action Drag --from-x 200 --from-y 540 --x 800 --y 540 --drag-speed 0

# Touch in Device Simulator coordinate space
uloop simulate-touch-input --action Tap --x 390 --y 844 --target-view Simulator

# Multi-touch: two simultaneous taps (use distinct touch IDs)
uloop simulate-touch-input --action Tap --x 300 --y 500 --touch-id 0
uloop simulate-touch-input --action Tap --x 700 --y 500 --touch-id 1
```

## Prerequisites

- Unity must be in **PlayMode**
- **Input System package** must be installed (`com.unity.inputsystem`)
- Game code must read input via Input System touch API (e.g. `Touchscreen.current.touches`, `EnhancedTouch.Touch`)
- Use `--target-view Simulator` when the Device Simulator is open and you want to match its screen space

## Output

Returns JSON with:
- `Success`: Whether the operation succeeded
- `Message`: Status message describing what was performed
- `Action`: Echoes which action was executed (`Tap`, `LongPress`, or `Drag`)
- `TouchId`: The user-facing touch ID that was used (0-9)
- `PositionX`: Target X coordinate (start position for Drag)
- `PositionY`: Target Y coordinate (start position for Drag)
- `EndPositionX`: Drag endpoint X (nullable; populated for Drag only)
- `EndPositionY`: Drag endpoint Y (nullable; populated for Drag only)
