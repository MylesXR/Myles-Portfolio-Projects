using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;
using MUXR.Interactables;

public class ActivateInteractor : MonoBehaviour
{
    #region ----Fields----

    public Transform rayInteractor;
    public Transform directInteractor;
    public Transform teleportInteractor;

    private Transform currentInteractor;
    #endregion ----Fields----

    #region ----Methods----
    private void Start()
    {
        if (currentInteractor == null)
            currentInteractor = rayInteractor;
    }
    public void SetTeleportState(bool enableTeleport = true)
    {
        teleportInteractor.gameObject.SetActive(enableTeleport);
    }

    public void ChangeInputMethod(bool enableRay = true, bool enableDirect = false)
    {
        GetComponent<RayActivator>().enabled = enableRay;
        rayInteractor.gameObject.SetActive(enableRay);
        directInteractor.gameObject.SetActive(enableDirect);
        currentInteractor = !enableDirect ? rayInteractor : directInteractor;
    }
    #endregion ----Methods----
}
