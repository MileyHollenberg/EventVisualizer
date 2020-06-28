using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace MegaTools.EventVisualizer
{
    /// <summary>
    /// Utility class for requesting the associated UnityEvents of the currently selected Object in the scene
    /// </summary>
    public static class EventUtils
    {
        private static List<EventReferenceInfo> FindAllUnityEvents()
        {
            // Get all mono behaviours 
            MonoBehaviour[] behaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>();

            Dictionary<MonoBehaviour, UnityEventBase> events = new Dictionary<MonoBehaviour, UnityEventBase>();

            // Loop through all behaviours
            foreach (MonoBehaviour behaviour in behaviours)
            {
                // Find any field that is of type UnityEventBase and add them to the list
                TypeInfo info = behaviour.GetType().GetTypeInfo();
                List<FieldInfo> linkedEvents = info.DeclaredFields
                    .Where(f => f.FieldType.IsSubclassOf(typeof(UnityEventBase)))
                    .ToList();

                foreach (FieldInfo ev in linkedEvents)
                {
                    events.Add(behaviour, ev.GetValue(behaviour) as UnityEventBase);
                }
            }

            // Populate the list with info about the Unity Events
            List<EventReferenceInfo> eventReferenceInfos = new List<EventReferenceInfo>();
            foreach (KeyValuePair<MonoBehaviour, UnityEventBase> e in events)
            {
                // Create the EventReferenceInfo object
                EventReferenceInfo info = new EventReferenceInfo {Owner = e.Key.gameObject};

                // Add all targets it has to the listeners list
                int count = e.Value.GetPersistentEventCount();
                for (int i = 0; i < count; i++)
                {
                    info.Listeners.Add(new Tuple<Object, string>(
                        e.Value.GetPersistentTarget(i),
                        e.Value.GetPersistentMethodName(i)
                    ));
                }

                // Add the info to the list
                eventReferenceInfos.Add(info);
            }

            // Return the result
            return eventReferenceInfos;
        }

        /// <summary>
        /// Get the events for the current selection and store in the inputs and outputs
        /// </summary>
        /// <param name="inputs">HashSet of Objects that have a UnityEvent aimed at the selected object</param>
        /// <param name="outputs">HashSet of Objects that the selected object's UnityEvents point towards</param>
        public static void GetCurrentSelections(ref Dictionary<Object, HashSet<string>> inputs,
            ref Dictionary<Object, HashSet<string>> outputs)
        {
            // If no object is selected there's nothing to do
            if (Selection.objects.Length != 1)
            {
                return;
            }

            Object currentSelection = Selection.activeObject;

            // Clear the data before starting
            inputs.Clear();
            outputs.Clear();

            List<EventReferenceInfo> events = FindAllUnityEvents();

            // Get all events that the current selection has (output)
            List<EventReferenceInfo> myEvents = events.Where(x => x.Owner == currentSelection).ToList();
            foreach (EventReferenceInfo eventReferenceInfo in myEvents)
            {
                foreach ((Object listener, string method) in eventReferenceInfo.Listeners)
                {
                    if (outputs.ContainsKey(listener))
                        outputs[listener].Add(method);
                    else
                        outputs.Add(listener, new HashSet<string> {method});
                }
            }

            foreach ((Object listener, string _) in events.SelectMany(eventReferenceInfo =>
                eventReferenceInfo.Listeners))
            {
                List<EventReferenceInfo> toAdd = new List<EventReferenceInfo>();

                // AudioSource is not a GameObject derivative, it needs special treatment through a Component
                if (listener is GameObject)
                {
                    toAdd = events.Select(x =>
                        x.Owner != currentSelection && x.Listeners.Any(y => y.Item1 == currentSelection)
                            ? new EventReferenceInfo
                            {
                                Listeners = x.Listeners.Where(y => y.Item1 == currentSelection).ToList(),
                                Owner = x.Owner
                            }
                            : null).Where(x => x != null).ToList();
                }
                else if (currentSelection is GameObject currentGameObject)
                {
                    // Get all events that aim at the selected object (input)
                    // Get the main component, the object itself doesn't work in the equality check when trying it with a audio source
                    if (currentGameObject.TryGetComponent(listener.GetType(), out Component component))
                    {
                        toAdd = events.Select(x =>
                            x.Owner != component && x.Listeners.Any(y => y.Item1 == component)
                                ? new EventReferenceInfo
                                {
                                    Listeners = x.Listeners.Where(y => y.Item1 == component).ToList(),
                                    Owner = x.Owner
                                }
                                : null).Where(x => x != null).ToList();
                    }
                }

                foreach (EventReferenceInfo eventReferenceInfo in toAdd)
                {
                    foreach ((Object key, string item) in eventReferenceInfo.Listeners)
                    {
                        if (inputs.ContainsKey(eventReferenceInfo.Owner))
                            inputs[eventReferenceInfo.Owner].Add(item);
                        else
                            inputs.Add(eventReferenceInfo.Owner, new HashSet<string> {item});
                    }
                }
            }
        }
    }
}
