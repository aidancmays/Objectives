using UnityEngine;

namespace Objectives
{
    /// <summary>
    /// One place on an <see cref="ObjectiveReceptacle"/> where an item is set down. The pedestal only notices an item
    /// resting in its area; it doesn't hold on to it, so the item can be picked up again and the pedestal is empty
    /// once more. Worked out on every machine from where the items are, so nothing about pedestals needs syncing.
    /// </summary>
    public sealed class ObjectivePedestal : MonoBehaviour
    {
        [Tooltip("Where an item counts as set on the pedestal.")]
        [SerializeField] ObjectiveArea area = new(new Vector3(0f, 0.9f, 0f), new Vector3(1.4f, 1.6f, 1.4f));
        [Tooltip("Where an item sits on the pedestal (for tools and gizmos). Empty = this transform.")]
        [SerializeField] Transform socket;
        [Tooltip("Shown while empty / once it holds an item (every machine).")]
        [SerializeField] GameObject empty;
        [SerializeField] GameObject filled;

        bool? shown;

        public ObjectiveArea Area => area;
        public Transform Socket => socket != null ? socket : transform;

        /// <summary>The item resting on this pedestal right now, or null (every machine).</summary>
        public ObjectiveItem Item { get; private set; }

        public bool IsFilled => Item != null;

        public bool Contains(ObjectiveItem item) => item != null && area.Contains(transform, item.transform.position);

        internal void Set(ObjectiveItem item)
        {
            Item = item;
            Show(item != null);
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
