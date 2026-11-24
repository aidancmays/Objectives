# Objectives

Networked objectives for Netcode for GameObjects. Objectives are plain components placed anywhere in a level
(including inside procedurally built pieces); one `ObjectiveTracker` on a NetworkObject collects them and syncs
their state from the host to every client, so the objectives themselves never need to be NetworkObjects.
Gates stay shut until every required objective is done.

## Install

```json
"com.aidancmays.objectives": "https://github.com/aidancmays/Objectives.git#v0.2.0"
```

Depends on Netcode for GameObjects 2.14 and the Input System (for the debug panel key).

## Concepts

| Thing | What it is |
|---|---|
| `Objective` | Base component. State (`Incomplete`, `Active`, `Complete`) plus progress 0 to 1, *Required* flag. Logic runs on the authority (host, or everywhere offline); every machine gets `Changed` / `OnStateChanged` for visuals. `Signal(int)` sends client input to the host's `OnSignal`. |
| `ObjectiveTracker` | NetworkBehaviour. `Collect(root)` gathers every active objective under the roots in hierarchy order and starts them over. The host's states go out in a `NetworkList`; clients apply them by index. `AllComplete`, `CompletedCount`, `RequiredCount`, `Changed`, `Completed`. `ObjectiveTracker.ForScene(scene)` finds one. |
| `ObjectiveGate` | Shut until its tracker (or the first one in its scene) is `AllComplete`. Toggles closed / open objects and raises `Opened` / `Closed`. |
| `ObjectiveIndicator` | Toggles one object per state and scales a fill by progress. |
| `PlateObjective` + `ObjectivePlate` | Co-op pressure plates: enough plates held at once (by default as many as there are players, so it is solvable alone), for *Hold Seconds*. |
| `HoldAreaObjective` | Hold an area for *Duration* seconds; the clock drains while nobody is inside. A stand-in for "survive the arena". |
| `ObjectiveReward` | On an objective: once it completes, the host spawns an `ObjectiveItem` prefab at a spawn point (in the objective's scene). Lost items (fallen out of the world, despawned) come back after a delay. |
| `ObjectiveItem` | NetworkBehaviour on the item prefab (add your own carry/physics components). `Consume()` delivers it, `Kind` filters receptacles. |
| `ObjectiveReceptacle` | An objective of its own: completes once enough items have entered its area (default: one per objective with a reward). The host consumes each item. `ObjectiveReceptacle.CanAccept` lets game code refuse items, e.g. while still held. Put one by the exit and gate the exit on the tracker: the way opens once every objective is done and every item brought. |
| `ObjectivePedestal` | A place on a receptacle where an item is set down: an item let go in its area is fixed at its socket, kinematic and out of reach of grab raycasts (Ignore Raycast layer), for good. A receptacle with a *Pedestal Prefab* lays out one per item needed, in centred rows. |
| `ObjectiveArea` | Serializable box standing on a transform, used by the above. |
| `ObjectivePlayers` | Where the players are: spawned NGO player objects, or your own `Source`. |
| `ObjectiveDebugPanel` | F10: every tracker's objectives; on the host, complete or reset them. |

## Use

1. Add `ObjectiveTracker` to a NetworkObject that exists on every machine (an in-scene object in the level scene).
2. Put objectives in the level. Every machine must end up with the same objectives in the same order: build the
   same level everywhere (same prefab, same seed).
3. After the level is built, on every machine: `tracker.Collect(levelRoot)`. The host's state wins; a client
   that collects later gets the current state straight away.
4. Gate the way out with `ObjectiveGate`, or check `ObjectiveTracker.ForScene(scene).AllComplete` yourself.

Offline (no NetworkManager listening) the tracker runs everything locally, so a level scene can be played alone.

## Writing an objective

```csharp
public sealed class LeverObjective : Objective
{
    public void Pull() => Signal(1);                     // any machine

    protected override void OnSignal(ulong sender, int value) => Complete();   // host

    protected override void OnStateChanged(ObjectiveState from, ObjectiveState to) { /* visuals, every machine */ }
}
```

Polling objectives check `Running` in `Update` (authority, not complete, tracker live) before calling
`SetState(state, progress)` or `Complete()`. Progress is synced as a byte, so setting it every frame only sends
when it visibly changes.
