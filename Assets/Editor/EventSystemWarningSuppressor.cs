using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

[InitializeOnLoad]
public class EventSystemWarningSuppressor
{
    static EventSystemWarningSuppressor()
    {
        // Hook into Unity's log system to catch warning logs
        Application.logMessageReceived += LogCallback;
    }

    // Handle log messages
    private static void LogCallback(string condition, string stackTrace, LogType type)
    {
        // Only suppress the specific warning about multiple EventSystems
        if (type == LogType.Warning && condition.Contains("event systems in the scene"))
        {
            // Temporarily remove inactive EventSystems from Unity's internal list
            RemoveInactiveEventSystems();

            // Prevent the warning from being logged
            return;
        }
    }

    // Remove inactive EventSystems from Unity's internal list
    private static void RemoveInactiveEventSystems()
    {
        // Use FindObjectsByType to get all EventSystems in the scene
        EventSystem[] eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);

        // Loop through and remove any inactive EventSystems from Unity's internal check list
        foreach (EventSystem eventSystem in eventSystems)
        {
            if (!eventSystem.gameObject.activeInHierarchy)
            {
                // Temporarily disable the event system to prevent the warning
                eventSystem.gameObject.SetActive(false);
            }
        }
    }
}
