# Flashlight Plugin Modernization Design

**Date:** 2026-07-21  
**Status:** Approved  
**Version target:** 0.1.0

## Goal

Modernize the CounterStrikeSharp flashlight plugin to the latest CSS/.NET stack, replace the per-tick spawn/teleport light path with a parented `light_barn` implementation, add `IPluginConfig` for admin tuning, improve testability, and update the README.

## Background

The current plugin (`Flashlight` v0.0.7) targets CounterStrikeSharp.API `1.0.363` on .NET 8. While the flashlight is on, `OnTick` creates a `light_omni2` and calls `DispatchSpawn` every server frame. That is the dominant performance problem. Existing tests only exercise inlined dictionary/bool logic and do not cover extractable plugin helpers. CI still installs .NET 7.

Latest CounterStrikeSharp.API is `1.0.371` and targets .NET 10. CS2Fixes demonstrates the preferred flashlight pattern: spawn `light_barn` once, set a flashlight lightcookie via entity keyvalues, parent to the player pawn attachment, and toggle enablement rather than teleporting every tick.

## Decisions

| Topic | Decision |
| --- | --- |
| Runtime | .NET 10 + CounterStrikeSharp.API 1.0.371 |
| Light entity | `light_barn` (`CBarnLight`), parented once |
| Position updates | Engine parenting; no per-tick teleport |
| Config | `IPluginConfig<FlashlightConfig>` with JSON config |
| Logging | Prefer `BasePlugin.Logger` |
| README | Fully updated for new runtime, behavior, config, build/test |
| Module version | 0.1.0 |

## Architecture

```
FlashlightPlugin (BasePlugin, IPluginConfig)
  ├── FlashlightConfig          // JSON-backed settings + clamps
  ├── PlayerFlashlightState     // per-player flags + entity handle
  ├── FlashlightService         // create / parent / enable / destroy
  └── FlashlightLogic           // pure helpers (toggle, cooldown, offsets)
```

### Runtime flow

1. `OnConfigParsed` validates/clamps config.
2. `Load` registers `OnTick` (Use-key edge + cooldown only), game event handlers, and command `css_fl_toggle`.
3. When a player turns the light on:
   - Create `CBarnLight` via `Utilities.CreateEntityByName<CBarnLight>("light_barn")`.
   - Apply config (brightness, range, color, temperature, soft/skirt/size, cast shadows, direct light).
   - Compute initial origin: pawn origin + eye Z offset + forward * `ForwardDistance`.
   - `Teleport` once using `System.Numerics.Vector3` overloads (avoid legacy `Vector` allocs).
   - `DispatchSpawn(CEntityKeyValues)` with `lightcookie` = configured path (default flashlight vtex).
   - `AcceptInput("SetParent", pawn, …)` then `AcceptInput("SetParentAttachmentMaintainOffset", …, AttachmentName)`.
   - Set `Enabled = true`.
4. When turned off: set `Enabled = false` and remove the entity (or disable and keep — prefer remove to avoid orphaned entities across pawn changes).
5. Cleanup on death, team change, disconnect, and plugin unload.

### Why not keep tick teleport?

Parenting follows view/attachment with far less managed work and no entity churn. Tick work is limited to scanning connected humans for Use-button edges and cooldown expiry.

## Config surface

File written/loaded by CSS config system (standard plugin config JSON).

| Key | Type | Default | Notes |
| --- | --- | --- | --- |
| `Enabled` | bool | `true` | Master switch |
| `AllowUseKey` | bool | `true` | Toggle via Use (`E`) |
| `ToggleCooldownSeconds` | float | `0.25` | Clamp ≥ 0 |
| `Brightness` | float | `1.0` | |
| `Range` | float | `2048` | Match CS2Fixes-style defaults |
| `ColorR` / `ColorG` / `ColorB` | byte | `255` | White |
| `ColorTemperature` | float | `6500` | |
| `CastShadows` | bool | `true` | Maps to `CastShadows` int |
| `SoftX` / `SoftY` | float | `1.0` | |
| `Skirt` | float | `0.5` | |
| `SkirtNear` | float | `1.0` | |
| `SizeX` / `SizeY` / `SizeZ` | float | `45` / `45` / `0.03` | `SizeParams` |
| `ForwardDistance` | float | `54` | Avoid AWP blocking beam |
| `StandEyeOffsetZ` | float | `64` | |
| `CrouchEyeOffsetZ` | float | `46` | Used at spawn time only |
| `AttachmentName` | string | `axis_of_intent` | Parent attachment |
| `LightCookie` | string | `materials/effects/lightcookies/flashlight.vtex` | |

Invalid values are clamped or rejected in `OnConfigParsed` with log warnings; plugin remains loadable when possible.

## Player state

Replace multiple `Dictionary<CCSPlayerController, …>` maps with one structure keyed by player slot (or controller), holding:

- `IsOn`
- `CanToggle`
- `Light` (`CBarnLight?`)

Crouch tracking for continuous Z updates is unnecessary once the light is parented; crouch offset is only applied at creation time. Optional: read duck state at spawn for initial Z only.

## Event / command behavior (unchanged UX)

- Use key toggles when `AllowUseKey` is true and cooldown allows.
- `css_fl_toggle` remains client-only command alternative.
- Flashlight turns off on death and spawn; entity cleaned on team change and disconnect.
- Bots ignored.

## Testing strategy

Full CSS entity lifecycle cannot be unit-tested without a game server. Extract and test pure logic:

1. Toggle state transitions and cooldown gating.
2. Config clamp helpers (range, cooldown, color channels).
3. Initial position offset calculation (stand/crouch Z + forward distance given basis vectors).
4. Enable/disable policy: when entity should be created vs destroyed.

Use xUnit on .NET 10. Keep Moq only if needed; prefer plain helpers over mocking CSS types.

## CI / packaging

- GitHub Actions: .NET 10 SDK, `dotnet restore`, `dotnet test`, release build on tags.
- Remove stale .NET 7 setup.
- Publish zip layout unchanged: `plugins/Flashlight/`.

## README updates

- Prerequisites: .NET 10, CSS 1.0.371+.
- Behavior: parented `light_barn`, Use + command.
- Config table with defaults.
- Build / test instructions.
- Changelog entry for 0.1.0 (API bump, performance rewrite, config, tests, CI).

## Out of scope

- Particle flashlight mode (CS2Fixes mode 2).
- Admin permissions / VIP-only flashlight.
- Client-side HUD indicators.
- Migrating to CounterStrikeSharp 2.0 alpha.

## Risks / mitigations

| Risk | Mitigation |
| --- | --- |
| Attachment name missing on some models | Configurable `AttachmentName`; fall back to parent-only if attachment input fails |
| `CEntityKeyValues` lightcookie path differs | Use CS2Fixes-proven path; document override |
| .NET 10 server prerequisite | Document clearly; MinimumApiVersion 371 |
| Parenting breaks on pawn swap | Recreate light on spawn; destroy on death/team |

## Success criteria

- Builds against CounterStrikeSharp.API 1.0.371 on net10.0.
- No entity create/spawn/teleport in the steady-state OnTick path.
- Config file generated and honored.
- Unit tests cover pure helpers and pass in CI.
- README matches shipped behavior and versions.
