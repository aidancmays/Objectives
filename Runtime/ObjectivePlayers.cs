using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Objectives
{
    /// <summary>
    /// Where the players are, for objectives that react to them. By default every spawned NGO player object; set
    /// <see cref="Source"/> to use something else (bots, an offline test rig).
    /// </summary>
    public static class ObjectivePlayers
    {
        /// <summary>Fills the list with player transforms. Null = NGO player objects.</summary>
        public static Action<List<Transform>> Source { get; set; }

        /// <summary>Clears <paramref name="into"/> and fills it with the current players.</summary>
        public static void Get(List<Transform> into)
        {
            into.Clear();
            if (Source != null)
            {
                Source(into);
                return;
            }
            var network = NetworkManager.Singleton;
            if (network == null || !network.IsListening || network.SpawnManager == null)
                return;
            foreach (var obj in network.SpawnManager.SpawnedObjectsList)
                if (obj != null && obj.IsPlayerObject)
                    into.Add(obj.transform);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Source = null;
    }

    /// <summary>A box that stands on its transform: centred on x and z, from just below the floor up to its height.</summary>
    [Serializable]
    public struct ObjectiveArea
    {
        [Tooltip("Centre of the area's floor, relative to the transform.")]
        public Vector3 offset;
        [Tooltip("Width, height and depth.")]
        public Vector3 size;

        public ObjectiveArea(Vector3 offset, Vector3 size)
        {
            this.offset = offset;
            this.size = size;
        }

        public bool Contains(Transform owner, Vector3 worldPoint)
        {
            var p = owner.InverseTransformPoint(worldPoint) - offset;
            return Mathf.Abs(p.x) <= size.x / 2f && Mathf.Abs(p.z) <= size.z / 2f && p.y >= -0.5f && p.y <= size.y;
        }

        /// <summary>How many of the players stand inside.</summary>
        public int Count(Transform owner, List<Transform> players)
        {
            int n = 0;
            foreach (var p in players)
                if (p != null && Contains(owner, p.position))
                    n++;
            return n;
        }

        public void DrawGizmo(Transform owner, Color color)
        {
            Gizmos.color = color;
            Gizmos.matrix = owner.localToWorldMatrix;
            Gizmos.DrawWireCube(offset + new Vector3(0f, size.y / 2f, 0f), size);
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
