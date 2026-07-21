# Flashlight Modernization Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Modernize the flashlight plugin to .NET 10 / CSS 1.0.371 with a parented `light_barn` implementation, config, real unit tests, fixed CI, and updated README.

**Architecture:** Pure helpers in `FlashlightLogic` + config clamps in `FlashlightConfig`; plugin owns lifecycle and wires Use/command/events; one `CBarnLight` per active player, parented once, toggled via create/destroy.

**Tech Stack:** .NET 10, CounterStrikeSharp.API 1.0.371, xUnit, GitHub Actions

## Global Constraints

- Target framework: `net10.0`
- Package: `CounterStrikeSharp.API` `1.0.371`
- `[MinimumApiVersion(371)]`
- Module version: `0.1.0`
- No per-tick entity create/spawn/teleport in steady state
- Spec: `docs/superpowers/specs/2026-07-21-flashlight-modernization-design.md`

---

### Task 1: Project targets + pure logic + tests

**Files:**
- Modify: `Flashlight/Flashlight.csproj`
- Modify: `Flashlight.Tests/Flashlight.Tests.csproj`
- Create: `Flashlight/FlashlightConfig.cs`
- Create: `Flashlight/FlashlightLogic.cs`
- Create: `Flashlight/PlayerFlashlightState.cs`
- Modify: `Flashlight.Tests/FlashlightLogicTests.cs`

- [x] Retarget both projects to `net10.0`; bump CSS to `1.0.371`; bump test packages as needed
- [x] Implement `FlashlightConfig` with defaults from the spec and a `Clamp()` method
- [x] Implement pure `FlashlightLogic` helpers: try-toggle with cooldown, eye Z offset, origin from base+forward
- [x] Replace smoke tests with tests against those helpers
- [x] Run `dotnet test` and confirm pass

### Task 2: Plugin rewrite (parented light_barn)

**Files:**
- Modify: `Flashlight/Flashlight.cs`
- Delete or gut: `Flashlight/LogHelper.cs` (prefer `Logger`)

- [x] Implement `BasePlugin, IPluginConfig<FlashlightConfig>`
- [x] OnTick: Use-key edge + cooldown only when `AllowUseKey`
- [x] Create/parent/enable `light_barn` once on toggle on; remove on toggle off
- [x] Cleanup on death/spawn/team/disconnect/unload
- [x] Keep `css_fl_toggle`
- [x] Run `dotnet build` and confirm success

### Task 3: CI + README

**Files:**
- Modify: `.github/workflows/build.yml`
- Modify: `README.md`

- [x] CI: .NET 10, restore, test, release build
- [x] README: versions, parented light, config table, build/test, changelog 0.1.0
- [x] Run `dotnet test` once more

---
