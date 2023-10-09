using Fusion;
using MUXR.Networking;
using System;
using UnityEngine;
using UnityEngine.Events;

public class TargetObjectController : NetworkBehaviour
{
    [Header("Params")]
    [SerializeField] private bool activateOnce = true;
    private bool hasBeenActivated;

    [Space()]

    [Header("Events")]
    public UnityEvent _localActivated;
    public UnityEvent _networkedActivated;
    public Action _eventActivated;

    public override void Spawned()
    {
        Spawnable spawnable;
        if (TryGetComponent(out spawnable))
            spawnable.OnSpawned += Init;
    }

    public void Init(GameObject originalSpawnable)
    {
        try
        {
            TargetObjectController originalTargetObject = originalSpawnable.GetComponent<TargetObjectController>();
            this._networkedActivated = originalTargetObject._networkedActivated;
        }
        catch (Exception e) { Debug.Log("Error init targetObjectController: " + gameObject.name); }
    }

    public void Activate()
    {
        if (Object == null || !Object.HasStateAuthority || hasBeenActivated)
            return;

        RPC_ActivateOnClient();

        // Play an animation (e.g. trigger an "Activate" animation state)
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ActivateOnClient()
    {
        Debug.Log("Activate Taget Object");
        hasBeenActivated = true;
        _eventActivated?.Invoke();
        _localActivated?.Invoke();
        _networkedActivated?.Invoke();
    }
}
