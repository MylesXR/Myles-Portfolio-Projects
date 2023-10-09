using Fusion;
using Kreis.Audio;
using MagicLightmapSwitcher;
using MUXR;
using MUXR.Networking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public struct LimboSubtitleData
{
    [TextArea()] public string subtitles;
    public float speed;
    public float subtitleVODelay;
}

public class LimboSceneFlowController : NetworkBehaviour
{
    [SerializeField] private SubtitlesController subtitlesController;
    [SerializeField] private List<LimboSubtitleData> witchesTranscriptions;
    [SerializeField] private Transform witchesPosition;
    [SerializeField] private SceneLoader sceneLoader;
    [SerializeField] private float secondsToNextScene = 30;
    [SerializeField] private float triggerVOThresholdSeconds = 180;
    private bool hasInitAlready = false;
    public int testCurrent = -1;
    private int nextCanonScene = 0;

#if UNITY_EDITOR
    public void OnValidate()
    {
        if (FindObjectOfType<LightTrigger>() == null)
            MagicLightmapSwitcher.MagicLightmapSwitcher.SetLightmaps();
    }
#endif
    IEnumerator Start()
    {
        KreisAudioManager.PlayAudio(AudioConsts.OST_TRANSITION);
        yield return new WaitForSeconds(6);
        KreisAudioManager.PlayAudio("OST_LIMBO");
    }

    private void OnEnable()
    {
        if (FindObjectOfType<LightTrigger>() == null)
            MagicLightmapSwitcher.MagicLightmapSwitcher.SetLightmaps();
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
            return;

        hasInitAlready = false;
        if (sceneLoader == null)
            sceneLoader = this.GetComponent<SceneLoader>();


        foreach (var avatar in FindObjectsOfType<AvatarController>())
            avatar.DeactivateAvatar();

        ModuleManager.Settings.photonVoiceVolumen = 0;

        var currentCanonScene = PlayerPrefs.GetInt("CurrentCanonScene", 0);

        if (currentCanonScene != 0)
            StartLimbo();
        else
            StartCoroutine(_StartVOThreshold());

        FusionManager.Events.PlayerJoined.AddListener(OnPlayerJoined);
    }

    private void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {

        if (Object.HasStateAuthority) return;

        foreach (var avatar in FindObjectsOfType<AvatarController>())
            avatar.DeactivateAvatar();
    }

    IEnumerator _StartVOThreshold()
    {
        yield return new WaitForSeconds(triggerVOThresholdSeconds);
        StartLimbo();
    }

    IEnumerator VOExecution(int currentCanonScene)
    {
        KreisAudioManager.PlayAudio($"VO_LIMBO_WITCHES_{currentCanonScene + 1}");

        yield return new WaitForSeconds(witchesTranscriptions[currentCanonScene].subtitleVODelay);
        subtitlesController.ShowText(
            witchesTranscriptions[currentCanonScene].subtitles,
                delayBetweenLetters: witchesTranscriptions[currentCanonScene].speed
            );
    }

    public void Update()
    {
        if (Object != null && Object.HasStateAuthority)
            return;
#if !UNITY_STANDALONE_LINUX
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
            StartLimbo();
        if (Keyboard.current.digit0Key.wasPressedThisFrame)
            RPC_StartLimbo();
        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            PlayerPrefs.SetInt("CurrentCanonScene", 1);
            sceneLoader.Load("SIM_Underworld_01");
        }
        if (Keyboard.current.f2Key.wasPressedThisFrame)
        {
            PlayerPrefs.SetInt("CurrentCanonScene", 2);
            sceneLoader.Load("SIM_Lobby");
        }
        if (Keyboard.current.f3Key.wasPressedThisFrame)
        {
            PlayerPrefs.SetInt("CurrentCanonScene", 3);
            sceneLoader.Load("SIM_Underworld_02");
        }
        if (Keyboard.current.f4Key.wasPressedThisFrame)
        {
            PlayerPrefs.SetInt("CurrentCanonScene", 4);
            sceneLoader.Load("SIM_Dining_Hall");
        }
#endif
    }

    [Rpc(RpcSources.Proxies, RpcTargets.Proxies)]
    private void RPC_StartLimbo()
    {
        StartLimbo();
    }

    private void StartLimbo()
    {
        if (!hasInitAlready || testCurrent > -1)
        {
            hasInitAlready = true;
            StartLimboClient();
        }
    }

    public void StartLimboClient() //refactor to be less repetitive
    {
        if (Object.HasStateAuthority)
            return;

        var currentCanonScene = PlayerPrefs.GetInt("CurrentCanonScene", 0);
        //var currentCanonScene = nextCanonScene != -1 ? nextCanonScene : PlayerPrefs.GetInt("CurrentCanonScene", 0);

        if (testCurrent > -1)
        {
            subtitlesController.StopAllCoroutines();
            StopAllCoroutines();
            subtitlesController.subtitleTMP.text = "";
            currentCanonScene = testCurrent;
        }

        if (currentCanonScene < witchesTranscriptions.Count)
            StartCoroutine(VOExecution(currentCanonScene));

        string nameOfNextScene = "";

        switch (currentCanonScene)
        {
            case 0:
                Debug.Log("First visit to limbo");
                //Add Substitles here.
                nameOfNextScene = "SIM_Underworld_01";
                break;
            case 1:
                Debug.Log("Second visit to limbo.");
                //Add Substitles here.
                nameOfNextScene = "SIM_Lobby";
                break;
            case 2:
                Debug.Log("Third visit to limbo.");
                nameOfNextScene = "SIM_Underworld_02";
                break;
            case 3:
                Debug.Log("Fourth visit to limbo.");
                nameOfNextScene = "SIM_Dining_Hall";
                break;
            case 4:
                Debug.Log("Credit Scene");
                nameOfNextScene = "QUIT";
                return;

            default:
                Debug.Log("Something went wrong, putting player back to last visit"); //Unsure about ideal behviour
                                                                                      //maybe visitsToLimbo -1 and then reload scene?
                return;
        }

        StartCoroutine(WaitForSeconds(secondsToNextScene, nameOfNextScene, currentCanonScene));
    }

    IEnumerator WaitForSeconds(float seconds, string nameOfNextScene, int currentCanonScene)
    {
        yield return new WaitForSeconds(seconds - 3);
        yield return NextScene(nameOfNextScene, currentCanonScene);
    }

    IEnumerator NextScene(string nameOfNextScene, int currentCanonScene)
    {
        if (Object.HasStateAuthority)
            yield break;

        ModuleManager.Settings.photonVoiceVolumen = 4;
        KreisAudioManager.StopAllAudio();

        currentCanonScene = currentCanonScene < 4 ? currentCanonScene + 1 : 0;
        PlayerPrefs.SetInt("CurrentCanonScene", currentCanonScene);
        Debug.Log("Loading scene " + nameOfNextScene);
        KernRig.Instance.EnableSmokeTransition(true);
        KreisAudioManager.PlayAudio(AudioConsts.OST_TRANSITION);
        yield return new WaitForSeconds(3);

        if (testCurrent > -1)
            yield break;

        if (nameOfNextScene.Equals("QUIT"))
            Application.Quit();
        else
            sceneLoader.Load(nameOfNextScene);
    }

    private void OnApplicationQuit()
    {
        ServerUtil.GetWebResponse("ROOM_DELETE", ModuleManager.Settings.LoadBalancer, ("worldID", WorldStatus.WorldId), ("roomID", WorldStatus.WorldId));
    }
}