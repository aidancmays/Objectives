using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Objectives
{
    /// <summary>
    /// Hands out an <see cref="ObjectiveItem"/> when its objective (on this object or a parent) completes. The host
    /// makes the item at the spawn point, in this object's scene; if it falls out of the world, or is lost any other
    /// way before it is delivered, a new one appears at the spawn point. Receptacles that count "one item per
    /// objective" count the objectives that have one of these.
    /// </summary>
    public sealed class ObjectiveReward : MonoBehaviour
    {
        [Tooltip("Prefab with an ObjectiveItem (and a NetworkObject, registered with the NetworkManager, when networked).")]
        [SerializeField] ObjectiveItem itemPrefab;
        [Tooltip("Where the item appears. Empty = this transform.")]
        [SerializeField] Transform spawnPoint;
        [Tooltip("The item is lost (and made again) when it drops this far below the spawn point.")]
        [SerializeField, Min(1f)] float lostBelow = 30f;
        [Tooltip("Seconds before a lost item is made again.")]
        [SerializeField, Min(0f)] float respawnDelay = 2f;

        Objective objective;
        ObjectiveItem item;
        float respawnAt = -1f;

        public Objective Objective => objective != null ? objective : objective = GetComponentInParent<Objective>();
        public ObjectiveItem ItemPrefab { get => itemPrefab; set => itemPrefab = value; }

        /// <summary>The item out in the world right now (authority only), or null.</summary>
        public ObjectiveItem Item => item;

        /// <summary>True once this objective's item has been delivered (authority only).</summary>
        public bool Delivered { get; private set; }

        Transform Point => spawnPoint != null ? spawnPoint : transform;

        void OnEnable()
        {
            if (Objective != null)
                objective.Changed += OnObjectiveChanged;
        }

        void OnDisable()
        {
            if (objective != null)
                objective.Changed -= OnObjectiveChanged;
        }

        void OnObjectiveChanged(Objective o)
        {
            if (!o.IsAuthority)
                return;
            if (o.IsComplete)
            {
                if (item == null && !Delivered)
                    respawnAt = 0f;
            }
            else
            {
                // Reset (debug, or the level was collected again): start over.
                Delivered = false;
                respawnAt = -1f;
                if (item != null)
                {
                    var old = item;
                    item = null;
                    old.Discard();
                }
            }
        }

        void Update()
        {
            if (objective == null || !objective.IsAuthority || !objective.IsComplete || Delivered)
                return;
            if (item != null)
            {
                if (item.transform.position.y < Point.position.y - lostBelow)
                {
                    var old = item;
                    item = null;
                    old.Discard();
                    respawnAt = Time.time + respawnDelay;
                }
                return;
            }
            if (respawnAt >= 0f && Time.time >= respawnAt && (objective.Tracker == null || objective.Tracker.IsLive))
            {
                respawnAt = -1f;
                Make();
            }
        }

        void Make()
        {
            if (itemPrefab == null)
            {
                Debug.LogWarning($"[Objectives] {name} has no item prefab.", this);
                return;
            }
            var point = Point;
            item = Instantiate(itemPrefab, point.position, point.rotation);
            if (item.gameObject.scene != gameObject.scene && gameObject.scene.IsValid())
                SceneManager.MoveGameObjectToScene(item.gameObject, gameObject.scene);
            item.Source = this;

            var network = NetworkManager.Singleton;
            if (network != null && network.IsListening && network.IsServer && item.TryGetComponent<NetworkObject>(out var netObj))
                netObj.Spawn(destroyWithScene: true);
        }

        internal void OnDelivered(ObjectiveItem delivered)
        {
            if (delivered != item)
                return;
            item = null;
            Delivered = true;
        }

        internal void OnLost(ObjectiveItem lost)
        {
            if (lost != item)
                return;
            item = null;
            if (isActiveAndEnabled && objective != null && objective.IsComplete && !Delivered)
                respawnAt = Time.time + respawnDelay;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.95f, 0.85f, 0.3f, 0.9f);
            Gizmos.DrawWireSphere(Point.position + Vector3.up * 0.3f, 0.3f);
        }
    }
}
