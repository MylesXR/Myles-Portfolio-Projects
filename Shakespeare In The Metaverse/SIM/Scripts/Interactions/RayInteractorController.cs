using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class RayInteractorController : NetworkBehaviour
{
    public XRBaseInteractor rayInteractor;
    public InputActionReference inputActionReference;

    private void OnEnable()
    {
        inputActionReference.action.performed += OnButtonPress;
        inputActionReference.action.canceled += OnButtonRelease;
    }

    private void OnDisable()
    {
        inputActionReference.action.performed -= OnButtonPress;
        inputActionReference.action.canceled -= OnButtonRelease;
    }

    private void OnButtonPress(InputAction.CallbackContext context)
    {
        RPC_ActivateRayInteractor(true);
    }

    private void OnButtonRelease(InputAction.CallbackContext context)
    {
        RPC_ActivateRayInteractor(false);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_ActivateRayInteractor(bool isActive)
    {
        if (rayInteractor != null)
        {
            rayInteractor.enabled = isActive;
        }
    }
}
