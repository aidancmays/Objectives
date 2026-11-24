using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Objectives
{
    /// <summary>
    /// Something an objective hands out for the players to bring somewhere: put it on a NetworkObject prefab (with
    /// whatever makes it carryable in your game) and give the prefab to an <see cref="ObjectiveReward"/>. The host
    /// spawns it when the objective completes; an <see cref="ObjectiveReceptacle"/> takes it when it arrives, either
    /// setting it on a pedestal (it stays in the world, fixed and out of reach of grabbing) or consuming it.
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
        [Tooltip("Layer the item's colliders move to once it is set on a pedestal, so raycast-based grabbing skips it. Default: Ignore Raycast.")]
        [SerializeField] int placedLayer = 2;

        readonly NetworkVariable<bool> placed = new();
        bool placedLocal;

        /// <summary>Every enabled item on this machine.</summary>
        public static IReadOnlyList<ObjectiveItem> All => all;

        public string Kind => kind;

        /// <summary>Authority only: the reward that made this item, or null.</summary>
        public ObjectiveReward Source { get; internal set; }

        /// <summary>True once a receptacle has taken it (authority).</summary>
        public bool Delivered { get; private set; }

        /// <summary>True while it sits on a pedestal (every machine).</summary>
        public bool IsPlaced => IsSpawned ? placed.Value : placedLocal;

        void OnEnable() => all.Add(this);
        void OnDisable() => all.Remove(this);

        public override void OnNetworkSpawn()
        {
            placed.OnValueChanged += OnPlacedChanged;
            if (placed.Value)
                ApplyPlaced();
        }

        /// <summary>Authority only: the item has arrived; tells its reward and removes it.</summary>
        public void Consume()
        {
            if (!Deliver())
                return;
            if (IsSpawned)
                NetworkObject.Despawn(true);
            else
                Destroy(gameObject);
        }

        /// <summary>Authority only: the item has arrived; tells its reward and fixes it at <paramref name="socket"/>.</summary>
        public void Place(Transform socket)
        {
            if (!Deliver())
                return;
            if (TryGetComponent<Rigidbody>(out var body))
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
                body.position = socket.position;
                body.rotation = socket.rotation;
            }
            transform.SetPositionAndRotation(socket.position, socket.rotation);
            if (IsSpawned)
                placed.Value = true;
            placedLocal = true;
            ApplyPlaced();
        }

        bool Deliver()
        {
            if (Delivered || !ObjectiveTracker.LocalIsAuthority)
                return false;
            Delivered = true;
            if (Source != null)
                Source.OnDelivered(this);
            return true;
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

        void OnPlacedChanged(bool previous, bool current)
        {
            if (current)
                ApplyPlaced();
        }

        // Every machine: fixed in place, and its colliders out of reach of grab raycasts.
        void ApplyPlaced()
        {
            placedLocal = true;
            if (TryGetComponent<Rigidbody>(out var body))
                body.isKinematic = true;
            foreach (var c in GetComponentsInChildren<Collider>(true))
                c.gameObject.layer = placedLayer;
        }

        public override void OnNetworkDespawn()
        {
            placed.OnValueChanged -= OnPlacedChanged;
            Lost();
        }

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
