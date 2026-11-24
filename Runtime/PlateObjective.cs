using System.Collections.Generic;
using UnityEngine;

namespace Objectives
{
    /// <summary>
    /// Co-op pressure plates: complete when enough of its <see cref="ObjectivePlate"/>s (children) have a player on
    /// them at the same time. By default as many plates as there are players are needed, up to every plate, so a
    /// lone player can still solve it.
    /// </summary>
    public sealed class PlateObjective : Objective
    {
        [Tooltip("Need no more plates than there are players.")]
        [SerializeField] bool scaleToPlayers = true;
        [Tooltip("Plates needed at once. 0 = every plate.")]
        [SerializeField, Min(0)] int platesNeeded;
        [Tooltip("Seconds the plates have to stay held.")]
        [SerializeField, Min(0f)] float holdSeconds = 1f;

        readonly List<Transform> players = new();
        ObjectivePlate[] plates = new ObjectivePlate[0];
        float held;

        public IReadOnlyList<ObjectivePlate> Plates => plates;

        /// <summary>Plates needed right now.</summary>
        public int Needed
        {
            get
            {
                int n = platesNeeded > 0 ? Mathf.Min(platesNeeded, plates.Length) : plates.Length;
                if (scaleToPlayers)
                    n = Mathf.Min(n, Mathf.Max(1, players.Count));
                return n;
            }
        }

        void Awake() => plates = GetComponentsInChildren<ObjectivePlate>();

        void Update()
        {
            // Every machine works out which plates are pressed, for visuals; only the authority decides.
            ObjectivePlayers.Get(players);
            int pressed = 0;
            foreach (var plate in plates)
            {
                bool on = IsComplete || plate.Area.Count(plate.transform, players) > 0;
                plate.SetPressed(on);
                if (on)
                    pressed++;
            }

            if (!Running || plates.Length == 0)
                return;
            int needed = Needed;
            if (pressed >= needed)
                held += Time.deltaTime;
            else
                held = 0f;
            if (pressed >= needed && held >= holdSeconds)
            {
                Complete();
                return;
            }
            float progress = Mathf.Min(pressed, needed) / (float)needed;
            SetState(pressed > 0 ? ObjectiveState.Active : ObjectiveState.Incomplete, progress);
        }

        protected override void OnReset() => held = 0f;
    }
}
