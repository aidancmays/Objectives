using System.Collections.Generic;
using UnityEngine;

namespace Objectives
{
    /// <summary>
    /// Hold an area: the clock runs while at least the needed number of players stand inside and the objective
    /// completes when it reaches the duration. With nobody inside, the clock drains (or pauses at drain 0). A
    /// stand-in for "survive the arena" until there are real encounters; subclass or replace it then.
    /// </summary>
    public sealed class HoldAreaObjective : Objective
    {
        [SerializeField] ObjectiveArea area = new(Vector3.zero, new Vector3(12f, 4f, 12f));
        [Tooltip("Seconds the area has to be held.")]
        [SerializeField, Min(0.1f)] float duration = 30f;
        [Tooltip("Players needed inside for the clock to run.")]
        [SerializeField, Min(1)] int playersNeeded = 1;
        [Tooltip("Seconds lost per second while the area is not held. 0 = the clock pauses.")]
        [SerializeField, Min(0f)] float drainRate = 0.5f;

        readonly List<Transform> players = new();
        float clock;

        public ObjectiveArea Area => area;
        public float Duration => duration;

        /// <summary>Players inside right now (worked out on every machine).</summary>
        public int Inside { get; private set; }

        void Update()
        {
            ObjectivePlayers.Get(players);
            Inside = area.Count(transform, players);
            if (!Running)
                return;

            bool holding = Inside >= Mathf.Min(playersNeeded, Mathf.Max(1, players.Count));
            clock = holding ? clock + Time.deltaTime : Mathf.Max(0f, clock - drainRate * Time.deltaTime);
            if (clock >= duration)
            {
                Complete();
                return;
            }
            SetState(holding || clock > 0f ? ObjectiveState.Active : ObjectiveState.Incomplete, clock / duration);
        }

        protected override void OnReset() => clock = 0f;

        /// <summary>For tools that build these.</summary>
        public void Setup(ObjectiveArea holdArea, float seconds)
        {
            area = holdArea;
            duration = Mathf.Max(0.1f, seconds);
        }

        void OnDrawGizmos() => area.DrawGizmo(transform, new Color(0.9f, 0.25f, 0.25f, 0.8f));
    }
}
