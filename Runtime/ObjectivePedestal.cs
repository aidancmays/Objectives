using UnityEngine;

namespace Objectives
{
    /// <summary>
    /// One place on an <see cref="ObjectiveReceptacle"/> where an item is set down: an item let go inside its area is
    /// fixed at its socket and stays there. Which pedestals hold an item is worked out on every machine from where the
    /// placed items are, so nothing about pedestals needs syncing.
    /// </summary>
    public sealed class ObjectivePedestal : MonoBehaviour
    {
        [Tooltip("Where an item counts as set on the pedestal.")]
        [SerializeField] ObjectiveArea area = new(new Vector3(0f, 0.9f, 0f), new Vector3(1.4f, 1.6f, 1.4f));
        [Tooltip("Where the item is fixed. Empty = this transform.")]
        [SerializeField] Transform socket;
        [Tooltip("Shown while empty / once it holds an item (every machine).")]
        [SerializeField] GameObject empty;
        [SerializeField] GameObject filled;

        bool? shown;

        public ObjectiveArea Area => area;
        public Transform Socket => socket != null ? socket : transform;

        /// <summary>The item on this pedestal, or null (every machine).</summary>
        public ObjectiveItem Item { get; private set; }

        public bool IsFilled => Item != null;

        /// <summary>Finds a placed item sitting at the socket (every machine).</summary>
        internal void Refresh()
        {
            if (Item != null && (!Item.isActiveAndEnabled || !Item.IsPlaced))
                Item = null;
            if (Item == null)
                foreach (var item in ObjectiveItem.All)
                    if (item != null && item.IsPlaced && (item.transform.position - Socket.position).sqrMagnitude < 0.01f)
                    {
                        Item = item;
                        break;
                    }
            Show(Item != null);
        }

        internal void Take(ObjectiveItem item)
        {
            item.Place(Socket);
            Item = item;
            Show(true);
        }

        internal void Clear()
        {
            Item = null;
            Show(false);
        }

        void Show(bool value)
        {
            if (shown == value)
                return;
            shown = value;
            if (empty != null)
                empty.SetActive(!value);
            if (filled != null)
                filled.SetActive(value);
        }

        /// <summary>For tools that build pedestals.</summary>
        public void Setup(ObjectiveArea dropArea, Transform itemSocket, GameObject whileEmpty, GameObject onceFilled)
        {
            area = dropArea;
            socket = itemSocket;
            empty = whileEmpty;
            filled = onceFilled;
            shown = null;
        }

        void OnDrawGizmos() => area.DrawGizmo(transform, new Color(0.95f, 0.85f, 0.3f, 0.8f));
    }
}
