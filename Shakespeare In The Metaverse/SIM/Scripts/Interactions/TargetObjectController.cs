using Fusion;
using MUXR.Networking;
using System;
using UnityEngine;
using UnityEngine.Events;
                         
public class TargetObjectController : NetworkBehaviour //This script is activated by the CircuitController script.
{
    
    [Header("Params")]
    [SerializeField] private bool activateOnce = true; // Whether the target object should activate only once
    private bool hasBeenActivated; // Flag to track if the target object has been activated

    
    [Space()]
    [Header("Events")]
    public UnityEvent _localActivated; // Local activation event
    public UnityEvent _networkedActivated; // Networked activation event
    public Action _eventActivated; // Action for activation event

    
    public override void Spawned()
    {
        // Attach the initialization method to the OnSpawned event
        Spawnable spawnable;
        if (TryGetComponent(out spawnable))
            spawnable.OnSpawned += Init;
    }

    // Initialization method, used for cloned objects
    public void Init(GameObject originalSpawnable)
    {
        try
        {
            // Obtain the original TargetObjectController and copy networked activation event to spawned clones
            TargetObjectController originalTargetObject = originalSpawnable.GetComponent<TargetObjectController>();
            this._networkedActivated = originalTargetObject._networkedActivated;
        }
        catch (Exception e) { Debug.Log("Error init targetObjectController: " + gameObject.name); }
    }

    
    public void Activate()
    {
        // Check conditions for activation
        if (Object == null || !Object.HasStateAuthority || hasBeenActivated)
            return;

        // Trigger RPC to activate on clients
        RPC_ActivateOnClient();

    }

    // RPC method to activate the target object on clients
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ActivateOnClient()
    {
        // Log activation message
        Debug.Log("Activate Target Object");

        // Set the activation flag to true
        hasBeenActivated = true;

        // Trigger local, networked, and custom activation events
        _eventActivated?.Invoke();
        _localActivated?.Invoke();
        _networkedActivated?.Invoke();
    }
}
