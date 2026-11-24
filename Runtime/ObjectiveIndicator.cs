using UnityEngine;

namespace Objectives
{
    /// <summary>
    /// Shows an objective's state with plain objects: one set per state, and an optional fill scaled by progress.
    /// Uses the objective on this object or its parents unless one is assigned.
    /// </summary>
    public sealed class ObjectiveIndicator : MonoBehaviour
    {
        [SerializeField] Objective objective;
        [SerializeField] GameObject incomplete;
        [SerializeField] GameObject active;
        [SerializeField] GameObject complete;
        [Tooltip("Scaled along its local Y by progress (0 to its authored height).")]
        [SerializeField] Transform fill;

        Vector3 fillScale = Vector3.one;

        void Awake()
        {
            if (objective == null)
                objective = GetComponentInParent<Objective>();
            if (fill != null)
                fillScale = fill.localScale;
        }

        void OnEnable()
        {
            if (objective == null)
                return;
            objective.Changed += Show;
            Show(objective);
        }

        void OnDisable()
        {
            if (objective != null)
                objective.Changed -= Show;
        }

        void Show(Objective o)
        {
            var state = o.State;
            if (incomplete != null)
                incomplete.SetActive(state == ObjectiveState.Incomplete);
            if (active != null)
                active.SetActive(state == ObjectiveState.Active);
            if (complete != null)
                complete.SetActive(state == ObjectiveState.Complete);
            if (fill != null)
            {
                fill.localScale = new Vector3(fillScale.x, fillScale.y * Mathf.Max(0.001f, o.Progress), fillScale.z);
                fill.gameObject.SetActive(o.Progress > 0f);
            }
        }

        /// <summary>For tools that build indicators.</summary>
        public void Setup(Objective target, GameObject whileIncomplete, GameObject whileActive, GameObject onceComplete, Transform progressFill)
        {
            objective = target;
            incomplete = whileIncomplete;
            active = whileActive;
            complete = onceComplete;
            fill = progressFill;
        }
    }
}
