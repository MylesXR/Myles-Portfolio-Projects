using Fusion;
using UnityEngine;

public class InteractionPointThresholds : NetworkBehaviour
{
    [SerializeField] private bool _isActive;
    public bool IsActive
    {
        get { return _isActive; }
        set { _isActive = value; }
    }

    [SerializeField] private int[] _activationThresholds;
    private int _currentPlayers = 0;
    private int _currentActivationLevel = 0;

    public void IncreasePlayerCount()
    {
        RPC_IncreasePlayerCount();
    }

    public void DecreasePlayerCount()
    {
        RPC_DecreasePlayerCount();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_IncreasePlayerCount()
    {
        _currentPlayers++;
        UpdateActivation();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_DecreasePlayerCount()
    {
        _currentPlayers--;
        UpdateActivation();
    }

    private void UpdateActivation()
    {
        for (int i = _currentActivationLevel; i < _activationThresholds.Length; i++)
        {
            if (_currentPlayers >= _activationThresholds[i])
            {
                Activate(i);
            }
        }

        for (int i = _currentActivationLevel; i >= 0; i--)
        {
            if (_currentPlayers < _activationThresholds[i])
            {
                Disactive(i);
            }
        }
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

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_ActivateTargetObject(bool isActive, int activationLevel)
    {
        IsActive = isActive;
        _currentActivationLevel = activationLevel;
    }
}
