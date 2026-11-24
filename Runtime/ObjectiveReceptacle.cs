using System;
using UnityEngine;

namespace Objectives
{
    /// <summary>
    /// Where objective items are brought: an objective of its own that completes once enough
    /// <see cref="ObjectiveItem"/>s have arrived inside its area. The host consumes each item that enters. By default
    /// it needs one item for every objective in its tracker that has an <see cref="ObjectiveReward"/>, so a gate that
    /// waits for every objective waits for every item to be brought here too.
    /// </summary>
    public sealed class ObjectiveReceptacle : Objective
    {
        [SerializeField] ObjectiveArea area = new(Vector3.zero, new Vector3(3f, 3f, 3f));
        [Tooltip("Items needed. 0 = one for each objective with a reward in this tracker.")]
        [SerializeField, Min(0)] int itemsNeeded;
        [Tooltip("Only take items of this kind. Empty = any.")]
        [SerializeField] string kind;
        [Tooltip("Shown one more per delivered item (sockets lighting up), on every machine.")]
        [SerializeField] GameObject[] filledSlots = new GameObject[0];

        int delivered;

        /// <summary>
        /// Host: whether an item inside the area may be taken now (for example, not while someone still holds it).
        /// Null = always. Applies to every receptacle; set it once from game code (it is not cleared between play sessions, so a RuntimeInitializeOnLoadMethod works).
        /// </summary>
        public static Func<GameObject, bool> CanAccept { get; set; }

        public ObjectiveArea Area => area;

        /// <summary>Items needed, worked out the same way on every machine.</summary>
        public int Needed
        {
            get
            {
                if (itemsNeeded > 0)
                    return itemsNeeded;
                int n = 0;
                if (Tracker != null)
                    foreach (var o in Tracker.Objectives)
                        if (o != null && o != this && o.Required && o.GetComponentInChildren<ObjectiveReward>() != null)
                            n++;
                return n;
            }
        }

        /// <summary>Items delivered so far (every machine, from the synced progress).</summary>
        public int DeliveredCount => IsAuthority ? delivered : Mathf.RoundToInt(Progress * Needed);

        void OnEnable()
        {
            Changed += ShowSlots;
            ShowSlots(this);
        }

        void OnDisable() => Changed -= ShowSlots;

        void Update()
        {
            if (!Running)
                return;
            int needed = Needed;
            if (needed > 0)
            {
                var items = ObjectiveItem.All;
                for (int i = items.Count - 1; i >= 0; i--)
                {
                    var item = items[i];
                    if (item == null || item.Delivered || (!string.IsNullOrEmpty(kind) && item.Kind != kind))
                        continue;
                    if (!area.Contains(transform, item.transform.position))
                        continue;
                    if (CanAccept != null && !CanAccept(item.gameObject))
                        continue;
                    delivered++;
                    item.Consume();
                }
            }
            if (delivered >= needed)
                Complete();
            else
                SetState(delivered > 0 ? ObjectiveState.Active : ObjectiveState.Incomplete, delivered / (float)needed);
        }

        protected override void OnReset() => delivered = 0;

        void ShowSlots(Objective o)
        {
            int filled = IsComplete ? filledSlots.Length : DeliveredCount;
            for (int i = 0; i < filledSlots.Length; i++)
                if (filledSlots[i] != null)
                    filledSlots[i].SetActive(i < filled);
        }

        /// <summary>For tools that build receptacles.</summary>
        public void Setup(ObjectiveArea dropArea, GameObject[] slots)
        {
            area = dropArea;
            filledSlots = slots ?? new GameObject[0];
        }

        void OnDrawGizmos() => area.DrawGizmo(transform, new Color(0.95f, 0.85f, 0.3f, 0.8f));
    }
}
