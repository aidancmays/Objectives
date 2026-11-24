using System;
using UnityEngine;

namespace Objectives
{
    /// <summary>
    /// Base for anything players have to get done. Put objectives anywhere under the roots an
    /// <see cref="ObjectiveTracker"/> collects (for a procedural level, inside the pieces); they are plain
    /// MonoBehaviours, not NetworkObjects. Every machine must collect the same objectives in the same order, which
    /// holds when every machine builds the same level.
    /// </summary>
    /// <remarks>
    /// Game logic runs only where <see cref="IsAuthority"/> is true (the host, or everywhere offline): check
    /// <see cref="Running"/> in <c>Update</c> and call <see cref="SetState"/> / <see cref="Complete"/>. Every machine
    /// gets <see cref="OnStateChanged"/> and <see cref="Changed"/> for visuals. Clients send input to the host with
    /// <see cref="Signal"/>, which arrives in <see cref="OnSignal"/> there.
    /// </remarks>
    public abstract class Objective : MonoBehaviour
    {
        [Tooltip("Name used by debug tools. Empty = the GameObject's name.")]
        [SerializeField] string label;
        [Tooltip("The tracker waits for this objective before it counts its objectives as done.")]
        [SerializeField] bool required = true;

        ObjectiveStatus status;

        public string Label => string.IsNullOrEmpty(label) ? name : label;
        public bool Required { get => required; set => required = value; }

        /// <summary>The tracker that collected this objective, or null.</summary>
        public ObjectiveTracker Tracker { get; private set; }

        /// <summary>Position in the tracker's list (the same on every machine), or -1.</summary>
        public int Index { get; private set; } = -1;

        public ObjectiveStatus Status => status;
        public ObjectiveState State => status.state;
        public float Progress => status.Progress;
        public bool IsComplete => status.state == ObjectiveState.Complete;

        /// <summary>True where this objective's state is decided: the host, or everywhere when not networked.</summary>
        public bool IsAuthority => Tracker != null ? Tracker.IsAuthority : ObjectiveTracker.LocalIsAuthority;

        /// <summary>True where game logic should run now: on the authority, not complete, tracker live.</summary>
        protected bool Running => IsAuthority && !IsComplete && (Tracker == null || Tracker.IsLive);

        /// <summary>Raised on every machine when the state or progress changes.</summary>
        public event Action<Objective> Changed;

        /// <summary>Authority only: sets the state and progress (0 to 1). Ignored elsewhere.</summary>
        protected void SetState(ObjectiveState state, float progress)
        {
            if (!IsAuthority)
                return;
            var next = new ObjectiveStatus(state, progress);
            if (Tracker != null)
                Tracker.Report(this, next);
            else
                Apply(next);
        }

        /// <summary>Authority only: marks the objective done.</summary>
        protected void Complete() => SetState(ObjectiveState.Complete, 1f);

        /// <summary>Authority only: completes the objective from outside (debug tools, scripted events).</summary>
        public void ForceComplete() => Complete();

        /// <summary>Authority only: back to the start.</summary>
        public void ResetObjective()
        {
            if (!IsAuthority)
                return;
            OnReset();
            SetState(ObjectiveState.Incomplete, 0f);
        }

        /// <summary>Sends a value to the authority's <see cref="OnSignal"/> (directly when this machine is it).</summary>
        public void Signal(int value)
        {
            if (Tracker != null)
                Tracker.SendSignal(this, value);
            else if (IsAuthority)
                OnSignal(0, value);
        }

        /// <summary>Authority: a <see cref="Signal"/> arrived from a client (or from this machine).</summary>
        protected internal virtual void OnSignal(ulong senderClientId, int value) { }

        /// <summary>Every machine: the state changed (progress alone changing does not call this).</summary>
        protected virtual void OnStateChanged(ObjectiveState previous, ObjectiveState current) { }

        /// <summary>Authority: the objective starts over (collected, or reset); clear any timers.</summary>
        protected virtual void OnReset() { }

        internal void Attach(ObjectiveTracker tracker, int index)
        {
            Tracker = tracker;
            Index = index;
            OnReset();
            Apply(default);
        }

        internal void Detach(ObjectiveTracker tracker)
        {
            if (Tracker != tracker)
                return;
            Tracker = null;
            Index = -1;
        }

        internal void Apply(ObjectiveStatus next)
        {
            if (next.Equals(status))
                return;
            var previous = status.state;
            status = next;
            if (previous != next.state)
                OnStateChanged(previous, next.state);
            Changed?.Invoke(this);
        }
    }
}
