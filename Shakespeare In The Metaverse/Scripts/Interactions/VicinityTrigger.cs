using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class VicinityTrigger : MonoBehaviour
{
    // Unity events triggered on specific actions
    [SerializeField] UnityEvent onEnter = null; // Event triggered when an object enters the vicinity
    [SerializeField] UnityEvent onHover = null; // Event triggered when an object is in the vicinity
    [SerializeField] UnityEvent onExit = null; // Event triggered when an object exits the vicinity

    // Timeout duration for triggering exit event, prevents spamming
    [SerializeField] float timeout = 1;

    // Flag to control whether to show the raycast line for debugging
    [SerializeField] public bool showRaycastLine = true;

    // Variables to track time and vicinity state
    float enterTime;
    bool inVicinity = false;


    void Update()
    {
        // Check if in vicinity and timeout has passed
        if (inVicinity && Time.time > enterTime + timeout)
        {
            // Object has been in vicinity for the specified time, trigger exit event
            inVicinity = false;
            onExit?.Invoke();
        }
    }

    // Method called when the object is touched or enters the vicinity
    public void Touch()
    {
        // Check if not already in the vicinity
        if (!inVicinity)
            onEnter?.Invoke(); // Trigger enter event

        // Set inVicinity flag to true and update enter time
        inVicinity = true;
        enterTime = Time.time;

        // Trigger hover event
        onHover?.Invoke();
    }
}
