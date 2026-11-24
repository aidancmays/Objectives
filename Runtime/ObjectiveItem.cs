using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Objectives
{
    /// <summary>
    /// Something an objective hands out for the players to bring somewhere: put it on a NetworkObject prefab (with
    /// whatever makes it carryable in your game) and give the prefab to an <see cref="ObjectiveReward"/>. The host
    /// spawns it when the objective completes; an <see cref="ObjectiveReceptacle"/> consumes it when it arrives.
    /// </summary>
    /// <remarks>
    /// Works offline too: the reward instantiates it without spawning. An item that is despawned or destroyed without
    /// being delivered is handed back to its reward, which makes a new one at the objective.
    /// </remarks>
    public sealed class ObjectiveItem : NetworkBehaviour
    {
        static readonly List<ObjectiveItem> all = new();

        [Tooltip("Kind of item, for receptacles that only take some kinds. Empty = any receptacle takes it.")]
        [SerializeField] string kind;

        /// <summary>Every enabled item on this machine.</summary>
        public static IReadOnlyList<ObjectiveItem> All => all;

        public string Kind => kind;

        /// <summary>Authority only: the reward that made this item, or null.</summary>
        public ObjectiveReward Source { get; internal set; }

        /// <summary>True once a receptacle has taken it.</summary>
        public bool Delivered { get; private set; }

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        /// <summary>Authority only: the item has arrived; tells its reward and removes it.</summary>
        public void Consume()
        {
            if (Delivered || !ObjectiveTracker.LocalIsAuthority)
                return;
            Delivered = true;
            if (Source != null)
                Source.OnDelivered(this);
            if (IsSpawned)
                NetworkObject.Despawn(true);
            else
                Destroy(gameObject);
        }

        /// <summary>Authority only: removes the item without delivering it, and without a new one being made.</summary>
        public void Discard()
        {
            Source = null;
            if (IsSpawned)
                NetworkObject.Despawn(true);
            else
                Destroy(gameObject);
        }

        public override void OnNetworkDespawn() => Lost();

        public override void OnDestroy()
        {
            Lost();
            base.OnDestroy();
        }

        void Lost()
        {
            if (Delivered || Source == null)
                return;
            var source = Source;
            Source = null;
            source.OnLost(this);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();
    }
}
