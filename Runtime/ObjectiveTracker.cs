using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Objectives
{
    /// <summary>
    /// Keeps every <see cref="Objective"/> under some roots in one list and syncs their state from the host to every
    /// client. Put it on a NetworkObject that exists on every machine (an in-scene one in a level scene) and call
    /// <see cref="Collect(Transform)"/> once the level is built, on every machine. Objectives are matched by their
    /// position in the list, so every machine must build the same level.
    /// </summary>
    /// <remarks>
    /// Works offline too (no NetworkManager listening, or the object never spawned): then this machine decides
    /// everything. <see cref="AllComplete"/> is what gates such as <see cref="ObjectiveGate"/> wait for.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ObjectiveTracker : NetworkBehaviour
    {
        static readonly List<ObjectiveTracker> active = new();

        readonly NetworkList<ObjectiveStatus> statuses = new();
        readonly List<Objective> objectives = new();
        readonly List<ObjectiveStatus> local = new();
        readonly List<Objective> found = new();
        bool wasComplete;

        /// <summary>Every enabled tracker.</summary>
        public static IReadOnlyList<ObjectiveTracker> Active => active;

        /// <summary>The first enabled tracker in a scene, or null.</summary>
        public static ObjectiveTracker ForScene(Scene scene)
        {
            foreach (var tracker in active)
                if (tracker.gameObject.scene == scene)
                    return tracker;
            return null;
        }

        /// <summary>True when this machine decides objectives without a tracker: offline, or the server.</summary>
        public static bool LocalIsAuthority
        {
            get
            {
                var network = NetworkManager.Singleton;
                return network == null || !network.IsListening || network.IsServer;
            }
        }

        /// <summary>The collected objectives, in the same order on every machine.</summary>
        public IReadOnlyList<Objective> Objectives => objectives;

        /// <summary>Objectives that count towards <see cref="AllComplete"/>.</summary>
        public int RequiredCount { get; private set; }

        /// <summary>Required objectives that are complete.</summary>
        public int CompletedCount { get; private set; }

        /// <summary>True when every required objective is complete (also when there are none).</summary>
        public bool AllComplete => CompletedCount >= RequiredCount;

        /// <summary>True on the machine that decides objective state.</summary>
        public bool IsAuthority => LocalIsAuthority;

        /// <summary>True when objective logic may run: offline, or spawned on the server.</summary>
        public bool IsLive
        {
            get
            {
                var network = NetworkManager.Singleton;
                return network == null || !network.IsListening || (IsSpawned && network.IsServer);
            }
        }

        /// <summary>Raised on every machine when any objective changes, or after <see cref="Collect(Transform)"/>.</summary>
        public event Action<ObjectiveTracker> Changed;

        /// <summary>Raised on every machine once, when the last required objective completes.</summary>
        public event Action<ObjectiveTracker> Completed;

        /// <summary>Raised on every machine when one objective changes.</summary>
        public event Action<Objective> ObjectiveChanged;

        void OnEnable() => active.Add(this);
        void OnDisable() => active.Remove(this);

        /// <summary>Collects every active objective under <paramref name="root"/> and starts them over.</summary>
        public void Collect(Transform root) => Collect(new[] { root });

        /// <summary>
        /// Collects every active objective under the roots, in order (roots first, then hierarchy order), and starts
        /// them over. Call it on every machine with the same roots, after the level is built.
        /// </summary>
        public void Collect(IEnumerable<Transform> roots)
        {
            foreach (var o in objectives)
                if (o != null)
                {
                    o.Changed -= OnObjectiveChanged;
                    o.Detach(this);
                }
            objectives.Clear();
            local.Clear();

            foreach (var root in roots)
            {
                if (root == null)
                    continue;
                root.GetComponentsInChildren(false, found);
                foreach (var o in found)
                    if (!objectives.Contains(o))
                        objectives.Add(o);
            }
            found.Clear();

            for (int i = 0; i < objectives.Count; i++)
            {
                objectives[i].Attach(this, i);
                objectives[i].Changed += OnObjectiveChanged;
                local.Add(default);
            }

            wasComplete = false;
            if (IsSpawned)
            {
                if (IsServer)
                    WriteAll();
                else
                    ReadAll();
            }
            Recount();
        }

        public override void OnNetworkSpawn()
        {
            statuses.OnListChanged += OnListChanged;
            if (IsServer)
                WriteAll();
            else
                ReadAll();
        }

        public override void OnNetworkDespawn() => statuses.OnListChanged -= OnListChanged;

        /// <summary>Authority only: completes every objective (debug).</summary>
        public void CompleteAll()
        {
            foreach (var o in objectives)
                if (o != null)
                    o.ForceComplete();
        }

        internal void Report(Objective objective, ObjectiveStatus status)
        {
            if (!IsAuthority || objective.Tracker != this)
                return;
            int i = objective.Index;
            if (i < 0 || i >= local.Count)
                return;
            local[i] = status;
            if (IsSpawned && IsServer && i < statuses.Count && !statuses[i].Equals(status))
                statuses[i] = status;
            objective.Apply(status);
        }

        internal void SendSignal(Objective objective, int value)
        {
            if (objective.Tracker != this)
                return;
            if (IsAuthority)
            {
                var network = NetworkManager.Singleton;
                objective.OnSignal(network != null ? network.LocalClientId : 0, value);
            }
            else if (IsSpawned)
                SignalRpc(objective.Index, value);
        }

        [Rpc(SendTo.Server)]
        void SignalRpc(int index, int value, RpcParams rpcParams = default)
        {
            if (index >= 0 && index < objectives.Count && objectives[index] != null)
                objectives[index].OnSignal(rpcParams.Receive.SenderClientId, value);
        }

        void WriteAll()
        {
            statuses.Clear();
            foreach (var s in local)
                statuses.Add(s);
        }

        void ReadAll()
        {
            if (objectives.Count == 0)
                return;
            if (statuses.Count != objectives.Count)
                Debug.LogWarning($"[Objectives] {name}: the host has {statuses.Count} objectives, this machine collected {objectives.Count}. Do both build the same level?", this);
            int n = Mathf.Min(statuses.Count, objectives.Count);
            for (int i = 0; i < n; i++)
            {
                local[i] = statuses[i];
                objectives[i].Apply(statuses[i]);
            }
        }

        void OnListChanged(NetworkListEvent<ObjectiveStatus> change)
        {
            if (!IsServer)
                ReadAll();
        }

        void OnObjectiveChanged(Objective objective)
        {
            ObjectiveChanged?.Invoke(objective);
            Recount();
        }

        void Recount()
        {
            int required = 0, completed = 0;
            foreach (var o in objectives)
            {
                if (o == null || !o.Required)
                    continue;
                required++;
                if (o.IsComplete)
                    completed++;
            }
            RequiredCount = required;
            CompletedCount = completed;
            Changed?.Invoke(this);
            if (objectives.Count > 0 && AllComplete && !wasComplete)
            {
                wasComplete = true;
                Completed?.Invoke(this);
            }
            else if (!AllComplete)
                wasComplete = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => active.Clear();
    }
}
