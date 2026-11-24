using UnityEngine;

namespace Objectives
{
    /// <summary>One plate of a <see cref="PlateObjective"/>: an area players stand in, with raised/pressed visuals.</summary>
    public sealed class ObjectivePlate : MonoBehaviour
    {
        [SerializeField] ObjectiveArea area = new(Vector3.zero, new Vector3(1.6f, 2f, 1.6f));
        [Tooltip("Shown while nobody stands on the plate.")]
        [SerializeField] GameObject raised;
        [Tooltip("Shown while someone does (and once the objective is complete).")]
        [SerializeField] GameObject pressed;

        bool? shown;

        public ObjectiveArea Area => area;

        /// <summary>True while a player stands on the plate (worked out on every machine).</summary>
        public bool IsPressed => shown == true;

        public void SetPressed(bool value)
        {
            if (shown == value)
                return;
            shown = value;
            if (raised != null)
                raised.SetActive(!value);
            if (pressed != null)
                pressed.SetActive(value);
        }

        /// <summary>For tools that build plates.</summary>
        public void Setup(ObjectiveArea plateArea, GameObject whileRaised, GameObject whilePressed)
        {
            area = plateArea;
            raised = whileRaised;
            pressed = whilePressed;
            shown = null;
        }

        void OnDrawGizmos() => area.DrawGizmo(transform, new Color(0.65f, 0.4f, 0.9f, 0.8f));
    }
}
