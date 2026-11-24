using UnityEngine;
using UnityEngine.InputSystem;

namespace Objectives
{
    /// <summary>
    /// Lists every tracker's objectives with their state, and on the host lets you complete or reset them.
    /// Toggle with F10. Meant for development; replace with real UI in your game.
    /// </summary>
    public sealed class ObjectiveDebugPanel : MonoBehaviour
    {
        [SerializeField] Key toggleKey = Key.F10;
        [SerializeField] bool visible;

        Rect window = new(10f, 330f, 340f, 10f);

        void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard[toggleKey].wasPressedThisFrame)
                visible = !visible;
        }

        void OnGUI()
        {
            if (visible)
                window = GUILayout.Window(GetInstanceID(), window, Draw, $"Objectives ({toggleKey})");
        }

        void Draw(int id)
        {
            var trackers = ObjectiveTracker.Active;
            if (trackers.Count == 0)
                GUILayout.Label("No tracker");
            foreach (var tracker in trackers)
            {
                GUILayout.Label($"{tracker.gameObject.scene.name}: {tracker.CompletedCount}/{tracker.RequiredCount}{(tracker.AllComplete ? " done" : "")}");
                foreach (var o in tracker.Objectives)
                {
                    if (o == null)
                        continue;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{o.Index}. {o.Label}{(o.Required ? "" : " (optional)")}: {o.Status}", GUILayout.Width(250f));
                    GUI.enabled = tracker.IsAuthority;
                    if (o.IsComplete ? GUILayout.Button("Reset") : GUILayout.Button("Done"))
                    {
                        if (o.IsComplete)
                            o.ResetObjective();
                        else
                            o.ForceComplete();
                    }
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                }
                GUI.enabled = tracker.IsAuthority && !tracker.AllComplete;
                if (GUILayout.Button("Complete all"))
                    tracker.CompleteAll();
                GUI.enabled = true;
            }
            GUI.DragWindow();
        }
    }
}
