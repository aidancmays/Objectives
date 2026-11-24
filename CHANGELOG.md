# Changelog

## [0.1.0] - 2026-11-24

- `Objective` base (state, progress, required, signals to the host), `ObjectiveTracker` (host-authoritative,
  synced by index, works offline), `ObjectiveGate`, `ObjectiveIndicator`.
- `PlateObjective` / `ObjectivePlate` (co-op pressure plates) and `HoldAreaObjective` (hold an area).
- Objective items: `ObjectiveReward` spawns an `ObjectiveItem` (networked prefab) when its objective completes and
  remakes it if it is lost; `ObjectiveReceptacle` takes items brought into its area and completes once it has one
  per rewarding objective. `ObjectiveReceptacle.CanAccept` hook.
- `ObjectiveDebugPanel` (F10).
