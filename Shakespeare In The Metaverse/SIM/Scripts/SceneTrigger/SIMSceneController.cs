using Fusion;
using Kreis.Audio;
using MagicLightmapSwitcher;
using MUXR;
using MUXR.Networking;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SIMSceneController : NetworkBehaviour
{
    [SerializeField] private SceneLoader sceneLoader;
    [SerializeField] private string nameOfNextScene;
    [SerializeField] private bool useTriggerKeys = true;

#if UNITY_EDITOR
    public void OnValidate()
    {
        if (FindObjectOfType<LightTrigger>() == null)
            MagicLightmapSwitcher.MagicLightmapSwitcher.SetLightmaps();
    }
#endif

    private void OnEnable()
    {
        if (FindObjectOfType<LightTrigger>() == null)
            MagicLightmapSwitcher.MagicLightmapSwitcher.SetLightmaps();
    }


    public void Update()
    {
#if !UNITY_STANDALONE_LINUX
        if (Keyboard.current.digit9Key.wasPressedThisFrame)
            RPC_TriggerAllIP();
        if (Keyboard.current.digit8Key.wasPressedThisFrame)
            RPC_StartVolcap();
        if (Keyboard.current.digit7Key.wasPressedThisFrame)
            RPC_EndScene();
#endif
    }
    public override void Spawned()
    {
        if (sceneLoader == null)
            sceneLoader = this.GetComponent<SceneLoader>();

        //Init interaction points index for SFX
        var interactionPoints = FindObjectsOfType<InteractionPoint>();
        for (int i = 0; i < interactionPoints.Length; i++)
            interactionPoints[i].interactionPointIndex = i;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_TriggerAllIP()
    {
        foreach (var ip in FindObjectsOfType<InteractionPoint>())
            ip.Activate(0);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_StartVolcap()
    {
        foreach (var volcapSequencer in FindObjectsOfType<VolcapSequenceController>(false))
            volcapSequencer.PlaySequence();
    }

    public void EndScene(float delay)
    {
        RPC_EndScene(delay);
    }

    public void EndScene()
    {
        Debug.Log("Loading Next Scene: " + nameOfNextScene);
        RPC_EndScene();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_EndScene(float secondsToWait = 0)
    {
        Debug.Log("attempting to end scene...");
        if (Object.HasStateAuthority)
        {
            nameOfNextScene = "QUIT";
            secondsToWait += 10;
        }

        StartCoroutine(_EndScene(secondsToWait));
    }

    IEnumerator _EndScene(float secondsToWait)
    {
        //Put shader smoke effect on
        yield return new WaitForSeconds(secondsToWait - 2);
        if (!Object.HasStateAuthority)
            KernRig.Instance.EnableSmokeTransition(true);
        KreisAudioManager.StopAllAudio();
        KreisAudioManager.PlayAudio(AudioConsts.OST_TRANSITION);
        yield return new WaitForSeconds(2);

        if (nameOfNextScene.Equals("QUIT"))
            Application.Quit();
        else
            Debug.Log("Loading " + nameOfNextScene);

        sceneLoader.Load(nameOfNextScene);

    }
}
