using System;
using System.Collections.Generic;
using UnityEngine;

namespace Objectives
{
    /// <summary>
    /// Where objective items are brought: an objective of its own that completes once enough
    /// <see cref="ObjectiveItem"/>s have arrived. By default it needs one item for every objective in its tracker that
    /// has an <see cref="ObjectiveReward"/>, so a gate that waits for every objective waits for every item too.
    /// </summary>
    /// <remarks>
    /// With pedestals (<see cref="ObjectivePedestal"/> children, plus copies of <c>Pedestal Prefab</c> laid out in rows
    /// until there is one per item needed), an item let go on an empty pedestal is fixed there and stays. Pedestals
    /// beyond the number needed are hidden. Without pedestals, the host consumes each item that enters the area.
    /// Pedestals are made locally on every machine from the same count, so they are never networked.
    /// </remarks>
    public sealed class ObjectiveReceptacle : Objective
    {
        [Tooltip("Without pedestals: items entering this area are consumed.")]
        [SerializeField] ObjectiveArea area = new(Vector3.zero, new Vector3(3f, 3f, 3f));
        [Tooltip("Items needed. 0 = one for each objective with a reward in this tracker.")]
        [SerializeField, Min(0)] int itemsNeeded;
        [Tooltip("Only take items of this kind. Empty = any.")]
        [SerializeField] string kind;

        [Header("Pedestals")]
        [Tooltip("Copied as needed when there are fewer pedestal children than items needed. Empty = children only.")]
        [SerializeField] ObjectivePedestal pedestalPrefab;
        [Tooltip("Metres between pedestals in a row (along local X, centred on this transform).")]
        [SerializeField, Min(0.5f)] float spacing = 2f;
        [Tooltip("Pedestals per row; further rows step back along local -Z.")]
        [SerializeField, Min(1)] int perRow = 5;
        [SerializeField, Min(0.5f)] float rowGap = 2f;

        readonly List<ObjectivePedestal> pedestals = new();
        readonly List<ObjectivePedestal> made = new();
        int delivered;
        int laidOutFor = -1;

        /// <summary>
        /// Host: whether an item inside the area may be taken now (for example, not while someone still holds it).
        /// Null = always. Applies to every receptacle; set it once from game code (it is not cleared between play
        /// sessions, so a RuntimeInitializeOnLoadMethod works).
        /// </summary>
        public static Func<GameObject, bool> CanAccept { get; set; }

        public ObjectiveArea Area => area;

        /// <summary>The pedestals in use (every machine), empty without pedestals.</summary>
        public IReadOnlyList<ObjectivePedestal> Pedestals => pedestals;

        bool UsesPedestals => pedestalPrefab != null || GetComponentInChildren<ObjectivePedestal>(true) != null;

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

        /// <summary>Items delivered so far (every machine).</summary>
        public int DeliveredCount
        {
            get
            {
                if (pedestals.Count > 0)
                {
                    int n = 0;
                    foreach (var p in pedestals)
                        if (p.IsFilled)
                            n++;
                    return n;
                }
                return IsAuthority ? delivered : Mathf.RoundToInt(Progress * Needed);
            }
        }

        void Update()
        {
            int needed = Needed;
            if (needed != laidOutFor && UsesPedestals)
                LayOut(needed);
            foreach (var p in pedestals)
                p.Refresh();

            if (!Running)
                return;
            if (needed > 0)
                Take();
            int count = pedestals.Count > 0 ? DeliveredCount : delivered;
            if (count >= needed)
                Complete();
            else
                SetState(count > 0 ? ObjectiveState.Active : ObjectiveState.Incomplete, count / (float)needed);
        }

        void Take()
        {
            var items = ObjectiveItem.All;
            for (int i = items.Count - 1; i >= 0; i--)
            {
                var item = items[i];
                if (item == null || item.Delivered || item.IsPlaced || (!string.IsNullOrEmpty(kind) && item.Kind != kind))
                    continue;
                var at = item.transform.position;
                if (pedestals.Count > 0)
                {
                    foreach (var p in pedestals)
                        if (!p.IsFilled && p.Area.Contains(p.transform, at) && Accepts(item))
                        {
                            p.Take(item);
                            break;
                        }
                }
                else if (area.Contains(transform, at) && Accepts(item))
                {
                    delivered++;
                    item.Consume();
                }
            }
        }

        static bool Accepts(ObjectiveItem item) => CanAccept == null || CanAccept(item.gameObject);

        // Children first, then copies of the prefab, so there is one pedestal per item needed; the rest are hidden.
        void LayOut(int needed)
        {
            laidOutFor = needed;
            pedestals.Clear();
            foreach (var p in GetComponentsInChildren<ObjectivePedestal>(true))
                if (!made.Contains(p))
                    pedestals.Add(p);
            foreach (var p in made)
                if (p != null)
                    pedestals.Add(p);

            for (int i = pedestals.Count; i < needed && pedestalPrefab != null; i++)
            {
                var p = Instantiate(pedestalPrefab, transform);
                p.name = $"{pedestalPrefab.name} {i + 1}";
                made.Add(p);
                pedestals.Add(p);
            }

            // Lay the prefab copies out in centred rows (hand-placed children stay where they are).
            int rows = Mathf.CeilToInt(needed / (float)perRow);
            for (int i = 0; i < pedestals.Count; i++)
            {
                var p = pedestals[i];
                if (made.Contains(p) && i < needed)
                {
                    int row = i / perRow;
                    int inRow = row < rows - 1 ? perRow : needed - row * perRow;
                    int col = i % perRow;
                    p.transform.localPosition = new Vector3((col - (inRow - 1) / 2f) * spacing, 0f, -row * rowGap);
                    p.transform.localRotation = Quaternion.identity;
                }
                p.gameObject.SetActive(i < needed);
            }
            if (pedestals.Count > needed)
                pedestals.RemoveRange(needed, pedestals.Count - needed);
        }

        protected override void OnReset() => delivered = 0;

        /// <summary>For tools that build receptacles.</summary>
        public void Setup(ObjectiveArea dropArea, ObjectivePedestal pedestal, float pedestalSpacing = 2f, int pedestalsPerRow = 5, float pedestalRowGap = 2f)
        {
            area = dropArea;
            pedestalPrefab = pedestal;
            spacing = pedestalSpacing;
            perRow = Mathf.Max(1, pedestalsPerRow);
            rowGap = pedestalRowGap;
            laidOutFor = -1;
        }

        void OnDrawGizmos()
        {
            if (!UsesPedestals)
                area.DrawGizmo(transform, new Color(0.95f, 0.85f, 0.3f, 0.8f));
        }
    }
}
