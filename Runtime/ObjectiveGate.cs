using UnityEngine;
using UnityEngine.Events;

namespace Objectives
{
    /// <summary>
    /// Something that stays shut until a tracker's required objectives are all complete: a door, an exit, a lift.
    /// Shows the closed objects while shut and the open objects once open, on every machine. Without a tracker
    /// assigned it uses the first one in its own scene; with no tracker, or one with no objectives, it is open.
    /// </summary>
    public sealed class ObjectiveGate : MonoBehaviour
    {
        [Tooltip("Empty = the first tracker in this scene.")]
        [SerializeField] ObjectiveTracker tracker;
        [Tooltip("Shown while shut (bars, a blocker with a collider).")]
        [SerializeField] GameObject[] closedObjects = new GameObject[0];
        [Tooltip("Shown once open.")]
        [SerializeField] GameObject[] openObjects = new GameObject[0];
        [SerializeField] UnityEvent opened = new();
        [SerializeField] UnityEvent closed = new();

        bool? shown;

        public ObjectiveTracker Tracker => tracker != null ? tracker : ObjectiveTracker.ForScene(gameObject.scene);

        /// <summary>True once every required objective is complete (or there is nothing to wait for).</summary>
        public bool IsOpen
        {
            get
            {
                var t = Tracker;
                return t == null || t.AllComplete;
            }
        }

        public UnityEvent Opened => opened;
        public UnityEvent Closed => closed;

        void OnEnable()
        {
            shown = null;
            Refresh();
        }

        // Polled: trackers come and go with levels, and this is one bool a frame.
        void Update() => Refresh();

        void Refresh()
        {
            bool open = IsOpen;
            if (shown == open)
                return;
            bool first = shown == null;
            shown = open;
            foreach (var o in closedObjects)
                if (o != null)
                    o.SetActive(!open);
            foreach (var o in openObjects)
                if (o != null)
                    o.SetActive(open);
            if (first)
                return;
            if (open)
                opened.Invoke();
            else
                closed.Invoke();
        }

        /// <summary>Sets the objects shown while shut and once open (for tools that build gates).</summary>
        public void SetObjects(GameObject[] whileClosed, GameObject[] onceOpen)
        {
            closedObjects = whileClosed ?? new GameObject[0];
            openObjects = onceOpen ?? new GameObject[0];
            shown = null;
        }
    }
}
