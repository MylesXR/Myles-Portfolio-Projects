using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ToggleChildObjectsOnGrabSimple : MonoBehaviour
{
    public List<GameObject> objectsToToggle;

    private XRGrabInteractable grabInteractable;

    void Start()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable?.selectEntered.AddListener(OnGrab);
        grabInteractable?.selectExited.AddListener(OnRelease);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        foreach (GameObject obj in objectsToToggle)
        {
            obj.SetActive(false);
        }
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        foreach (GameObject obj in objectsToToggle)
        {
            obj.SetActive(true);
        }
    }
}
