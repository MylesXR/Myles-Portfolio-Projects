using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraForServerCheck : MonoBehaviour
{
    public Camera cam;
    public FreeFlyCamera freeFlyController;
    private bool isCamActive;

    // Update is called once per frame
    public void Awake()
    {
#if UNITY_SERVER
        cam.gameObject.SetActive(true);
        freeFlyController.enabled = false;
#else
        cam.gameObject.SetActive(false);
        isCamActive = false;
        freeFlyController.enabled = true;
#endif
    }

#if !UNITY_SERVER
    private void Update()
    {
        if (Keyboard.current.minusKey.wasPressedThisFrame)
        {
            isCamActive = !isCamActive;
            cam.gameObject.SetActive(isCamActive);
        }
    }
#endif
}
