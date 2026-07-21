# Flashlight

Flashlight is a Counter-Strike 2 server plugin written in C# with [CounterStrikeSharp](https://docs.cssharp.dev). It gives human players a toggleable flashlight using a parented `light_barn` entity.

## Features

- Toggle with the Use key (`E` by default) or `css_fl_toggle`
- One `light_barn` per player, parented to the pawn attachment (no per-tick spawn/teleport)
- Configurable brightness, range, color, shadows, offsets, and attachment
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
| `ToggleCooldownSeconds` | `0.25` | Toggle cooldown |
| `Brightness` | `1.0` | Light brightness |
| `Range` | `2048` | Light range |
| `ColorR` / `ColorG` / `ColorB` | `255` | Light color |
| `ColorTemperature` | `6500` | Kelvin temperature |
| `CastShadows` | `true` | Cast dynamic shadows |
| `SoftX` / `SoftY` | `1.0` | Softness |
| `Skirt` / `SkirtNear` | `0.5` / `1.0` | Skirt falloff |
| `SizeX` / `SizeY` / `SizeZ` | `45` / `45` / `0.03` | Beam size params |
| `ForwardDistance` | `54` | Spawn offset along view forward |
| `StandEyeOffsetZ` | `64` | Standing eye height offset |
| `CrouchEyeOffsetZ` | `46` | Crouching eye height offset |
| `AttachmentName` | `axis_of_intent` | Parent attachment |
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

Unit tests cover toggle/cooldown logic, Use-key edge detection, origin math, create/destroy policy, and config clamping. Entity parenting requires a live CS2 server.

## Changelog

### v0.1.0

- Updated to .NET 10 and CounterStrikeSharp.API 1.0.371
- Replaced per-tick `light_omni2` spawn/teleport with parented `light_barn`
- Added `IPluginConfig` settings for light and toggle behavior
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
