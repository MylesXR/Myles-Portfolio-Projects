using Kreis.Audio;
using MUXR.Networking;
using System;
using System.Collections.Generic;
using UnityEngine;


public class AudioSceneThemeController : MonoBehaviour
{
    #region ----Fields----
    public bool atGameStarted = false;
    public List<SceneAudioData> initAudioKeys = new List<SceneAudioData>();
    #endregion ----Fields----

    #region ----Methods----
    public void Start()
    {
        if (atGameStarted)
            Init();
        else
            FusionManager.Instance.OnGameStarted += Init;
    }

    private void Init()
    {
        foreach (var initAudioData in initAudioKeys)
            KreisAudioManager.PlayAudio(initAudioData.key, initAudioData.options);
    }
    #endregion ----Methods----
}

[Serializable]
public class SceneAudioData
{
    public string key;
    public AudioJobOptions options;
}
