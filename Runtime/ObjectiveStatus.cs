using System;
using Unity.Netcode;
using UnityEngine;

namespace Objectives
{
    /// <summary>Where an objective stands.</summary>
    public enum ObjectiveState : byte
    {
        /// <summary>Not started.</summary>
        Incomplete,
        /// <summary>Under way (someone is on it), not done yet.</summary>
        Active,
        /// <summary>Done.</summary>
        Complete,
    }

    /// <summary>An objective's synced state: its <see cref="ObjectiveState"/> and progress quantised to a byte.</summary>
    public struct ObjectiveStatus : INetworkSerializable, IEquatable<ObjectiveStatus>
    {
        public ObjectiveState state;
        public byte progress;

        public ObjectiveStatus(ObjectiveState state, float progress)
        {
            this.state = state;
            this.progress = (byte)Mathf.RoundToInt(Mathf.Clamp01(progress) * 255f);
        }

        public float Progress => progress / 255f;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref state);
            serializer.SerializeValue(ref progress);
        }

        public bool Equals(ObjectiveStatus other) => state == other.state && progress == other.progress;
        public override bool Equals(object obj) => obj is ObjectiveStatus other && Equals(other);
        public override int GetHashCode() => ((int)state << 8) | progress;
        public override string ToString() => $"{state} {Mathf.RoundToInt(Progress * 100f)}%";
    }
}
