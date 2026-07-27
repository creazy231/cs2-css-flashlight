# Flashlight

Flashlight is a Counter-Strike 2 server plugin written in C# with [CounterStrikeSharp](https://docs.cssharp.dev). It gives human players a toggleable flashlight using a `light_barn` entity.

## Features

- Toggle with the Use key (`E` by default) or `css_fl_toggle`
- One `light_barn` per player, re-aimed each tick so the beam tracks both pitch and yaw
- Configurable brightness, range, color, shadows, and offsets
- Optional team restriction (`Any`, `CT`, or `T`)
- Automatically turns off on death, spawn, and team change
- Bots ignored

## Requirements

- CounterStrikeSharp **1.0.371+** (API minimum version enforced)
- .NET **10** runtime as provided by your CounterStrikeSharp install

## Installation

1. Download the latest release ZIP.
2. Extract it.
3. Place the plugin folder in `game/csgo/addons/counterstrikesharp/plugins/Flashlight`.

## Usage

- Press Use (`E`) to toggle, if enabled in config.
- Or bind a key:

```
bind f "css_fl_toggle"
```

## Configuration

On first load, CounterStrikeSharp writes a JSON config for the plugin. Defaults:

| Key | Default | Description |
| --- | --- | --- |
| `Enabled` | `true` | Master switch |
| `AllowUseKey` | `true` | Allow Use-key toggle |
| `AllowedTeam` | `Any` | Who may use it: `Any`, `CT`, or `T` |
| `ToggleCooldownSeconds` | `0.25` | Toggle cooldown |
| `Brightness` | `1.0` | Light brightness |
| `Range` | `2048` | Light range |
| `ColorR` / `ColorG` / `ColorB` | `255` | Light color |
| `ColorTemperature` | `6500` | Kelvin temperature |
| `CastShadows` | `true` | Cast dynamic shadows |
| `SoftX` / `SoftY` | `1.0` | Softness |
| `Skirt` / `SkirtNear` | `0.5` / `1.0` | Skirt falloff |
| `SizeX` / `SizeY` / `SizeZ` | `45` / `45` / `0.03` | Beam size params |
| `ForwardDistance` | `54` | Horizontal offset in front of the eye, so the beam clears the player model |
| `StandEyeOffsetZ` | `64` | Standing eye height offset |
| `CrouchEyeOffsetZ` | `46` | Crouching eye height offset |
| `LightCookie` | `materials/effects/lightcookies/flashlight.vtex` | Flashlight cookie texture |

## Development

### Prerequisites

- .NET 10 SDK
- CounterStrikeSharp.API 1.0.371+

### Build

```bash
dotnet restore
dotnet build
```

### Test

```bash
dotnet test
```

Unit tests cover toggle/cooldown logic, Use-key edge detection, transform math (origin and pitch/yaw angles), create/destroy policy, and config clamping. Entity behaviour itself requires a live CS2 server.

## Changelog

### v0.1.2

- Fixed the beam only following horizontal aim: the light was parented to the pawn's `axis_of_intent` attachment, which carries body yaw but not view pitch, so looking up or down never moved it. The light is now un-parented and re-aimed every tick from the pawn's live `V_angle`.
- Fixed the flashlight never updating when `AllowUseKey` was `false`, which previously short-circuited the whole tick loop.
- `ForwardDistance` now offsets the light horizontally only, so looking straight down no longer pushes it through the floor.
- Removed the obsolete `AttachmentName` config key (leaving it in an existing config file is harmless and ignored).

### v0.1.1

- Updated to .NET 10 and CounterStrikeSharp.API 1.0.371
- Replaced per-tick `light_omni2` spawn/teleport with parented `light_barn`
- Added `IPluginConfig` settings for light and toggle behavior
- Added `AllowedTeam` config (`Any` / `CT` / `T`) to restrict flashlight by side
- Added focused xUnit tests for pure helpers
- Updated GitHub Actions for .NET 10, PR tests, and tag releases
- Switched logging to `BasePlugin.Logger`

### v0.0.6

- Updated to .NET 8.0 and CounterStrikeSharp.API v1.0.363
- Added initial xUnit test project
- Improved CSS API compatibility (`V_angle`, entity validity checks)

### v0.0.5

- Initial release

## License

GNU General Public License. See `LICENSE`.

## Author

[creazy.eth](https://github.com/creazy231)
