using Fusion;
using Kreis.Audio;
using MUXR.Networking;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class InteractionPoint : NetworkBehaviour //This scripts events are triggered by the script Vicinity. Vicinity is part of the
                                                 //Kern systems in other applications and is required to interact with the custom XR Rig.
{
    // References to input actions for left and right buttons, uses trigger.
    public InputActionReference inputActionReferenceLeft;
    public InputActionReference inputActionReferenceRight;

    // Variables to control the state and behavior of the InteractionPoint.
    [SerializeField] private bool _isActive; // Whether the InteractionPoint is currently active.
    [HideInInspector] public int interactionPointIndex = -1; // Index of the InteractionPoint.

    // Properties for accessing the state of the InteractionPoint
    public bool IsActive { get { return _isActive; } set { _isActive = value; } }

    // Unity events triggered on certain actions.
    public UnityEvent OnActive; // Event triggered when the InteractionPoint becomes active.
    public UnityEvent OnDeactivate; // Event triggered when the InteractionPoint becomes inactive.
    public UnityEvent IncreasedValueEvents; // Events triggered on increasing interaction value, used for shader and sound.
    public UnityEvent DecreaseValueEvents; // Events triggered on decreasing interaction value, used for shader and sound.

    // Variables for managing interaction value and thresholds
    private bool wasIncreasingLastFrame = false; // Variable to store the state from the previous frame.
    public int[] _activationThresholds = new int[1] { 1 }; // Activation thresholds for different levels.
    private int _currentPlayers = 0; // Number of players interacting, some InteractionPoints require multiple players to activate.
    private int _currentActivationLevel = 0; // Current activation level, some InteractionPoints can be triggered more then once 
                                             // this is used for single interaction points that have multiple stages of activation.
    private bool hittingButton = false; // Flag indicating if a button is being pressed.
    private bool isIncreasing = false; // Flag indicating if interaction value is increasing.
    private float interactionValue = 0f; // Current interaction value.
    [SerializeField] private float interactionLerpRate = 0.8f; // Rate of change for interaction value, used to change speed of 
                                                               // interactionValue if wire is much shorter or longer then average.

    // Networked variable to track hovering state
    [Networked] private NetworkBool IsHovering { get; set; }

    // Reference to the wire renderer for visual feedback, randerer has a shader value that is affected by interactionValue.
    public Renderer wireRenderer;

    // Called when the object is spawned
    public override void Spawned()
    {
        // Set up button callbacks for both left and right buttons
        inputActionReferenceLeft.action.performed += (ctx) => hittingButton = true;
        inputActionReferenceLeft.action.canceled += (ctx) => hittingButton = false;

        inputActionReferenceRight.action.performed += (ctx) => hittingButton = true;
        inputActionReferenceRight.action.canceled += (ctx) => hittingButton = false;

        // Initialize wire renderer materials if available
        if (wireRenderer == null)
            return;

        var cableMaterials = wireRenderer.GetComponent<MeshRenderer>().materials;
        for (int i = 0; i < cableMaterials.Length; i++)
        {
            //_TransitionValue is the value in the shader affected by interactionValue,
            //this is used to provide the visual of electricity on the wire.
            if (cableMaterials[i].HasProperty("_TransitionValue"))
            {
                Material newMat = cableMaterials[i];
                newMat.SetFloat("_TransitionValue", 0);
                cableMaterials[i] = newMat;
            }
        }
    }

    // Called when the player hovers over the interaction point, this is called by a UnityEvent in Vicinty scipt.
    public void IncreaseLoadingBar()
    {
        // Check if a button is being pressed, then trigger hover
        if (hittingButton)
            RPC_SetHover(true);
    }

    // RPC method to set the hover state
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_SetHover(NetworkBool shouldHover)
    {
        StopAllCoroutines();
        IsHovering = shouldHover;
        StartCoroutine(SetHoverCooldown());
    }

    // Cooldown for hover state to prevent rapid changes and confusion of players.
    IEnumerator SetHoverCooldown()
    {
        yield return new WaitForSeconds(1);
        IsHovering = false;
    }

    public void Update()
    {
        // Check if the object has state authority
        if (!Object.HasStateAuthority)
            return;

        // Check if the interaction point is hovering
        if (IsHovering)
        {
            // Increase interaction value when hovering
            if (interactionValue < 100)
            {
                interactionValue += interactionLerpRate;

                // Check for reaching the threshold to activate.
                // If activated this InteractionPoint can affect CircuitController.
                if (interactionValue >= 100 && !isIncreasing)
                    IncreasePlayerCount();

                // Log interaction value and update visual and audio feedback
                Debug.Log("Interaction value: " + interactionValue);
                NetworkBool isInitIncrease = !wasIncreasingLastFrame;
                RPC_ClientAffordancesUp(interactionValue, isInitIncrease);
                wasIncreasingLastFrame = true;
            }
        }
        else
        {
            // Decrease interaction value when not hovering
            if (interactionValue > 0)
            {
                interactionValue -= interactionLerpRate;

                // Check for reaching the threshold to deactivate. This threshold is anything less than full because
                // players are ment to hold there electricity at the object to keep it active until all other players
                // have also activated their InteractionPoint. This ensures group collaberation to turn on TargetObjects.
                NetworkBool initGoingDown = interactionValue < 100 && wasIncreasingLastFrame;
                if (interactionValue < 100 && wasIncreasingLastFrame)
                    DecreasePlayerCount();

                // Log interaction value and update visual and audio feedback
                Debug.Log("Interaction value: " + interactionValue);
                RPC_ClientAffordancesDown(interactionValue, initGoingDown);
            }
            wasIncreasingLastFrame = false;
        }
    }

    // RPC method for handling affordances when interaction value increases
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ClientAffordancesUp(float interactionValue, NetworkBool init)
    {
        SetShaderTransitionValue(interactionValue);
        if (init)
        {
            //Initalize the UnityEvents that take place when value is increasing, such as shader value and sound.
            IncreasedValueEvents?.Invoke();
            KreisAudioManager.StopAudio(AudioConsts.SFX_CABLE_DOWN);
            KreisAudioManager.PlayAudio(AudioConsts.SFX_CABLE_UP, new AudioJobOptions(volumen: 0.7f, speed: (1 + (interactionValue / 100)), position: this.transform, minDistance: 7));
        }
    }

    // RPC method for handling affordances when interaction value decreases
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ClientAffordancesDown(float interactionValue, NetworkBool init)
    {
        SetShaderTransitionValue(interactionValue);
        if (init)
        {
            //Initialize the UnityEvents that take place when the value is decreasing.
            DecreaseValueEvents?.Invoke();
            KreisAudioManager.StopAudio(AudioConsts.SFX_CABLE_UP);
            KreisAudioManager.PlayAudio(AudioConsts.SFX_CABLE_DOWN, new AudioJobOptions(volumen: 0.7f, speed: (1 + (1 - (1 / (interactionValue / 100)))), position: this.transform, minDistance: 7));
        }
    }

    // Set shader transition value for visual feedback
    private void SetShaderTransitionValue(float value)
    {
        foreach (Material mat in wireRenderer.materials)
            mat.SetFloat("_TransitionValue", value);
    }

    // Increase the player count and trigger RPC
    public void IncreasePlayerCount()
    {
        if (Object == null)
            return;
        if (!Object.HasStateAuthority && !hittingButton)
            return;
        if (isIncreasing)
            return;

        isIncreasing = true;
        RPC_IncreasePlayerCount();
    }

    // Context menu method for increasing player count without hitting the button,
    //this is used when a TargetObject UnityEvent triggers a InteractionPoint instead of player count
    [ContextMenu("Increase player count")]
    public void IncreasePlayerCountWithoutHittingButton()
    {
        RPC_IncreasePlayerCount();
    }

    // Decrease the player count and trigger RPC, this is used to ensure all players are activating together simultaneously.
    public void DecreasePlayerCount()
    {
        isIncreasing = false;
        RPC_DecreasePlayerCount();
    }

    // RPC method to increase player count
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_IncreasePlayerCount()
    {
        isIncreasing = true;
        _currentPlayers++;
        UpdateActivation();
    }

    // RPC method to decrease player count
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_DecreasePlayerCount()
    {
        isIncreasing = false;
        if (_currentPlayers > 0)
            _currentPlayers--;
        UpdateActivation();
    }

    // Update the activation state based on player count and thresholds, all interaction points
    // in CircuitManager are required to be active to trigger TargetObject. 
    private void UpdateActivation()
    {
        if (_currentActivationLevel >= _activationThresholds.Length)
            _currentActivationLevel = _activationThresholds.Length - 1;

        for (int i = _currentActivationLevel; i < _activationThresholds.Length; i++)
            if (_currentPlayers >= _activationThresholds[i])
                Activate(i);

        for (int i = _currentActivationLevel; i >= 0; i--)
            if (_currentPlayers < _activationThresholds[i])
                Disactive(i);
    }

    // Activate the interaction point and trigger RPC
    public void Activate(int activationLevel)
    {
        _currentActivationLevel = activationLevel;
        RPC_ActivateTargetObject(true, activationLevel);
        Debug.Log($"Activate Level: {activationLevel + 1}");
    }

    // Deactivate the interaction point and trigger RPC
    public void Disactive(int activationLevel)
    {
        _currentActivationLevel = activationLevel;
        RPC_ActivateTargetObject(false, activationLevel);
        Debug.Log($"Disactive Level: {activationLevel + 1}");
    }

    // RPC method to activate or deactivate the target object
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ActivateTargetObject(bool isActive, int activationLevel)
    {
        if (isActive)
            OnActive?.Invoke();
        else
            OnDeactivate?.Invoke();

        IsActive = isActive;
        _currentActivationLevel = activationLevel;
    }
}
