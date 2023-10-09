using Fusion;
using Kreis.Audio;
using MUXR.Networking;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class InteractionPoint : NetworkBehaviour
{
    public InputActionReference inputActionReferenceLeft;
    public InputActionReference inputActionReferenceRight;
    [SerializeField] private bool _isActive;
    [HideInInspector] public int interactionPointIndex = -1;

    public bool IsActive
    {
        get { return _isActive; }
        set { _isActive = value; }
    }

    public UnityEvent OnActive;
    public UnityEvent OnDeactivate;
    public UnityEvent IncreasedValueEvents; //UW1 events for stones
    public UnityEvent DecreaseValueEvents;
    private bool wasIncreasingLastFrame = false; // This variable to store the state from the previous frame


    public int[] _activationThresholds = new int[1] { 1 };
    private int _currentPlayers = 0;
    private int _currentActivationLevel = 0;
    private bool hittingButton = false;
    private bool isIncreasing = false;
    private float interactionValue = 0f;
    [SerializeField] private float interactionLerpRate = 0.8f; // Adjust rate of change as necessary
    [Networked] private NetworkBool IsHovering { get; set; }
    public Renderer wireRenderer;

    public override void Spawned()
    {
        inputActionReferenceLeft.action.performed += (ctx) => hittingButton = true;
        Debug.Log("hitting left button is true");
        inputActionReferenceLeft.action.canceled += (ctx) => hittingButton = false;
        Debug.Log("hitting left button is false");

        inputActionReferenceRight.action.performed += (ctx) => hittingButton = true;
        Debug.Log("hitting right button is true");
        inputActionReferenceRight.action.canceled += (ctx) => hittingButton = false;
        Debug.Log("hitting right button is false");

        if (wireRenderer == null)
            return;

        var cableMaterials = wireRenderer.GetComponent<MeshRenderer>().materials;
        for (int i = 0; i < cableMaterials.Length; i++)
        {
            if (cableMaterials[i].HasProperty("_TransitionValue"))
            {
                Material newMat = cableMaterials[i];
                newMat.SetFloat("_TransitionValue", 0);
                cableMaterials[i] = newMat;
            }
        }
    }

    public void IncreaseLoadingBar() //called from vicinity on hover.  
    {
        if (hittingButton)
            RPC_SetHover(true);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_SetHover(NetworkBool shouldHover)
    {
        StopAllCoroutines();
        IsHovering = shouldHover;
        StartCoroutine(SetHoverCooldown());
    }

    IEnumerator SetHoverCooldown()
    {
        yield return new WaitForSeconds(1);
        IsHovering = false;
    }

    public void Update()
    {

        if (!Object.HasStateAuthority)
            return;

        if (IsHovering)
        {
            // Increase interaction value when hovering
            if (interactionValue < 100)
            {
                interactionValue += interactionLerpRate;
                // Check for reaching the threshold so it only activates once, otherwise it increases player count rapidly.
                if (interactionValue >= 100 && !isIncreasing)
                    IncreasePlayerCount();


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
                // Check for reaching the threshold
                NetworkBool initGoingDown = interactionValue < 100 && wasIncreasingLastFrame;
                if (interactionValue < 100 && wasIncreasingLastFrame)
                    DecreasePlayerCount();

                Debug.Log("Interaction value: " + interactionValue);
                RPC_ClientAffordancesDown(interactionValue, initGoingDown);
            }
            wasIncreasingLastFrame = false;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ClientAffordancesUp(float interactionValue, NetworkBool init)
    {
        SetShaderTransitionValue(interactionValue);
        if (init)
        {
            IncreasedValueEvents?.Invoke();
            KreisAudioManager.StopAudio(AudioConsts.SFX_CABLE_DOWN);
            KreisAudioManager.PlayAudio(AudioConsts.SFX_CABLE_UP, new AudioJobOptions(volumen: 0.7f, speed: (1 + (interactionValue / 100)), position: this.transform, minDistance: 7));
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ClientAffordancesDown(float interactionValue, NetworkBool init)
    {
        SetShaderTransitionValue(interactionValue);
        if (init)
        {
            DecreaseValueEvents?.Invoke();
            KreisAudioManager.StopAudio(AudioConsts.SFX_CABLE_UP);
            KreisAudioManager.PlayAudio(AudioConsts.SFX_CABLE_DOWN, new AudioJobOptions(volumen: 0.7f, speed: (1 + (1 - (1 / (interactionValue / 100)))), position: this.transform, minDistance: 7));
        }
    }

    private void SetShaderTransitionValue(float value)
    {
        foreach (Material mat in wireRenderer.materials)
            mat.SetFloat("_TransitionValue", value);
    }

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

    [ContextMenu("Increase player count")]
    public void IncreasePlayerCountWithoutHittingButton()
    {
        RPC_IncreasePlayerCount();
    }

    public void DecreasePlayerCount()
    {
        isIncreasing = false;
        RPC_DecreasePlayerCount();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_IncreasePlayerCount()
    {
        isIncreasing = true;
        _currentPlayers++;
        UpdateActivation();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_DecreasePlayerCount()
    {
        isIncreasing = false;
        if (_currentPlayers > 0)
            _currentPlayers--;
        UpdateActivation();
    }

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

    public void Activate(int activationLevel)
    {
        _currentActivationLevel = activationLevel;
        RPC_ActivateTargetObject(true, activationLevel);
        Debug.Log($"Activate Level: {activationLevel + 1}");
    }

    public void Disactive(int activationLevel)
    {
        _currentActivationLevel = activationLevel;
        RPC_ActivateTargetObject(false, activationLevel);
        Debug.Log($"Disactive Level: {activationLevel + 1}");
    }

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
