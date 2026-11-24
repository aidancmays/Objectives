# Changelog

## [0.3.0] - 2026-11-24

- Pedestals only detect items: an item resting on a pedestal (and accepted by `CanAccept`) counts, nothing is fixed in
  place, and taking it off empties the pedestal again. The receptacle completes once every pedestal is filled at the
  same time. `ObjectivePedestal.Item` / `Contains`; `ObjectiveItem.Place` is no longer used by pedestals.

## [0.2.0] - 2026-11-24

- Pedestals: `ObjectivePedestal`, and `ObjectiveReceptacle` lays out one per item needed from a prefab (rows along
  local X, stepping back along -Z) plus any hand-placed children. Items let go on a pedestal are fixed there
  (`ObjectiveItem.Place`, synced `IsPlaced`, colliders moved to a layer grabbing skips). Without pedestals the
  receptacle consumes items as before.

## [0.1.0] - 2026-11-24

- `Objective` base (state, progress, required, signals to the host), `ObjectiveTracker` (host-authoritative,
  synced by index, works offline), `ObjectiveGate`, `ObjectiveIndicator`.
- `PlateObjective` / `ObjectivePlate` (co-op pressure plates) and `HoldAreaObjective` (hold an area).
- Objective items: `ObjectiveReward` spawns an `ObjectiveItem` (networked prefab) when its objective completes and
  remakes it if it is lost; `ObjectiveReceptacle` takes items brought into its area and completes once it has one
  per rewarding objective. `ObjectiveReceptacle.CanAccept` hook.
- `ObjectiveDebugPanel` (F10).
