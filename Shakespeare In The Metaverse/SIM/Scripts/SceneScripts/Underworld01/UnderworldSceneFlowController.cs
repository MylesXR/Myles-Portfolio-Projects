using Fusion;
using Kreis.Audio;
using MUXR.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class UnderworldSceneFlowController : NetworkBehaviour
{
    [SerializeField] private SceneLoader sceneLoader;
    public GameObject wallCollider;
    public Animator bridgeAnimator;

    [SerializeField] private Transform witchesPosition;
    private int maxPlayersToStartExperience = 20;
    private bool hasInitiated = false;

    public override void Spawned()
    {
        GetComponent<Spawnable>().OnSpawned += Init;
    }

    public void Init(GameObject originalSpawnable)
    {
        UnderworldSceneFlowController originalUnderworldSceneFlowController = originalSpawnable.GetComponent<UnderworldSceneFlowController>();
        try
        {
            this.wallCollider = originalUnderworldSceneFlowController.wallCollider;
            this.bridgeAnimator = originalUnderworldSceneFlowController.bridgeAnimator;
        }
        catch (Exception e) { Debug.Log("UnderworldSceneFlowController failed init" + gameObject.name); }
    }

    public void Update()
    {
#if !UNITY_STANDALONE_LINUX
        if (Keyboard.current.digit0Key.wasPressedThisFrame)
            StartUnderworld();
#endif
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!Object.HasStateAuthority)
            return;
        if (FusionManager.Runner.ActivePlayers.Count() == maxPlayersToStartExperience)
            StartUnderworld();
    }

    public void StartUnderworld()
    {
        if (hasInitiated)
            return;
        RPC_StartUnderworld();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_StartUnderworld()
    {
        KreisAudioManager.PlayAudio(AudioConsts.D_GONG, new AudioJobOptions(position: witchesPosition, minDistance: 130));
        KreisAudioManager.PlayAudio(AudioConsts.SFX_BRIDGE, new AudioJobOptions(position: witchesPosition, minDistance: 130));
        bridgeAnimator.SetTrigger("Start Bridge");
        StartCoroutine(WaitForBridgeAnimationFinish());

        hasInitiated = true;
    }
    IEnumerator WaitForBridgeAnimationFinish()
    {
        yield return new WaitForSeconds(10);
        wallCollider.SetActive(false);
    }
}
