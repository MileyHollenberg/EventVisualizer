using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace MegaTools.EventVisualizer
{
    public class EventNavigationWindow : EditorWindow
    {
        // Store the inputs and outputs in HashSets to avoid duplicates
        private Dictionary<Object, HashSet<string>> _inputs = new Dictionary<Object, HashSet<string>>();
        private Dictionary<Object, HashSet<string>> _outputs = new Dictionary<Object, HashSet<string>>();

        // The currently selected tab in the GUI, 0 = input, 1 = output
        private int currentTab;

        [MenuItem("Window/Event Navigation")]
        private static void ShowWindow()
        {
            EventNavigationWindow window = GetWindow<EventNavigationWindow>();
            window.titleContent = new GUIContent("Event Navigation");
            window.Show();
        }

        private void OnFocus()
        {
            // Remove callback listener if it had already been assigned
            SceneView.duringSceneGui -= OnSceneGUI;

            // Add (or re-add) the callbacks.
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDestroy()
        {
            // Remove the callbacks to nicely clean up the event calls
            SceneView.duringSceneGui -= OnSceneGUI;
            // Selection.selectionChanged -= SelectionChanged;
        }

        private void OnSelectionChange()
        {
            // Grab the new data
            EventUtils.GetCurrentSelections(ref _inputs, ref _outputs);

            // And repaint the window
            Repaint();
        }


        /// <summary>
        /// Draw the event lines within the scene
        /// </summary>
        /// <param name="sceneView"></param>
        private void OnSceneGUI(SceneView sceneView)
        {
            if (Selection.activeGameObject == null)
            {
                return;
            }

            // Get the position of the currently selected object
            Vector3 currentPosition = Selection.activeGameObject.transform.position;

            // Draw all outputs
            foreach (KeyValuePair<Object, HashSet<string>> output in _outputs)
                DrawEventLine(currentPosition, output.Key, Color.yellow);

            // Draw all inputs
            foreach (KeyValuePair<Object, HashSet<string>> input in _inputs)
                DrawEventLine(currentPosition, input.Key, Color.magenta, false);
        }

        /// <summary>
        /// Helper method to draw a line between the target object and the current position, also adds an arrow in the right direction
        /// </summary>
        /// <param name="currentPosition">The starting position for this line</param>
        /// <param name="obj">The object to target</param>
        /// <param name="color">The color of the line</param>
        /// <param name="ReverseArrowDirection">Point arrow from the currentLocation or towards it</param>
        private static void DrawEventLine(Vector3 currentPosition, Object obj, Color color,
            bool ReverseArrowDirection = true)
        {
            const float arrowSize = 1f;
            Handles.color = color;
            Vector3 targetPos = Vector3.zero;

            // Grab the target pos
            switch (obj)
            {
                case Component comp:
                    targetPos = comp.transform.position;
                    break;
                case GameObject gameObject:
                    targetPos = gameObject.transform.position;
                    break;
            }

            //Draw the regular line
            Handles.DrawLine(currentPosition, targetPos);

            //Draw the arrow
            if (ReverseArrowDirection)
                Handles.ArrowHandleCap(0, currentPosition,
                    Quaternion.LookRotation((targetPos - currentPosition).normalized, Vector3.up), arrowSize,
                    EventType.Repaint);
            else
                Handles.ArrowHandleCap(0, targetPos,
                    Quaternion.LookRotation((currentPosition - targetPos).normalized, Vector3.up), arrowSize,
                    EventType.Repaint);
        }

        private void OnGUI()
        {
            if (Selection.activeGameObject == null)
                return;

            // Draw the toolbar and all the items from the list
            currentTab = GUILayout.Toolbar(currentTab, new[] {"Input", "Output"});
            switch (currentTab)
            {
                case 0:
                    foreach (KeyValuePair<Object, HashSet<string>> caller in _inputs.Where(DrawEventData))
                    {
                        Selection.objects = caller.Key is GameObject
                            ? new[] {caller.Key}
                            : new Object[] {(caller.Key as Component)?.gameObject};
                    }

                    break;
                case 1:
                    foreach (KeyValuePair<Object, HashSet<string>> caller in _outputs.Where(DrawEventData))
                    {
                        Selection.objects = caller.Key is GameObject
                            ? new[] {caller.Key}
                            : new Object[] {(caller.Key as Component)?.gameObject};
                    }

                    break;
            }
        }

        private static bool DrawEventData(KeyValuePair<Object, HashSet<string>> caller)
        {
            GUILayout.BeginHorizontal();

            bool select = GUILayout.Button("Select", new GUIStyle(GUI.skin.button) {fixedWidth = 50f});
            GUILayout.Label(caller.Key.name, new GUIStyle(GUI.skin.label) {fixedWidth = 100f});
            GUILayout.BeginVertical();
            foreach (string method in caller.Value)
            {
                GUILayout.Label(method);
            }
            GUILayout.EndVertical();
            
            GUILayout.EndHorizontal();

            return select;
        }
    }
}
