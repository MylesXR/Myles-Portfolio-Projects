using Fusion;
using MUXR.Networking;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NetworkedVolcapController : NetworkBehaviour
{
    #region ----Fields----
    [Networked(OnChanged = nameof(OnVideoStartTimeChanged), OnChangedTargets = OnChangedTargets.All)]
    public float VideoStartTime { get; set; }
    public HoloVideoObject HoloVideoObject { get => holoVideoObject; }



    [SerializeField] private HoloVideoObject holoVideoObject;
    [SerializeField, Range(0, 1)] public float videoCurrentTimer;
    [SerializeField] public float hologramDurationInNs = 0;
    public Action OnEndPlayback;

    #endregion ----Fields----

    #region ----Methods----

    #region Init

    public override void Spawned()
    {
        if (FusionManager.Runner.GameMode == GameMode.Single)
            return;
        holoVideoObject.OnEndOfStreamNotify += (holoVideoObject) => OnEndPlayback?.Invoke(); ;
        ConfigureVolcap(holoVideoObject.Url);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        if (VideoStartTime != videoCurrentTimer)
            VideoStartTime = videoCurrentTimer;
    }

    public void PauseVolcap()
    {
        holoVideoObject.Pause();
    }

    public void ResetVolcap()
    {
        holoVideoObject.Stop();
    }
    public void ConfigureVolcap(string url)
    {
        if (!Object.StateAuthority)
            holoVideoObject.Open(url);
        FastFoward();
    }

    private void OnDisable()
    {
        Debug.Log("Disable volcap: " + gameObject.name);
        holoVideoObject.Close();
        holoVideoObject.Cleanup();
    }
    #endregion Init

    #region Play
    public void StartVolcap()
    {
        Debug.Log("Start Volcap");
        if (Object.HasStateAuthority)
            RPC_StartVideo();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_StartVideo()
    {
        Debug.Log("Start Volcap local");
        holoVideoObject.Play();
    }
    #endregion Play

    #region FastFoward
    public void FastFoward()
    {
        if (FusionManager.Runner.GameMode != GameMode.Server)
            return;
        var time = GetTimeFromPercentage(VideoStartTime);
        Debug.Log("Volcap time: " + time);
        RPC_FastFoward(time);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_FastFoward(int time)
    {
        holoVideoObject.SeekToTime(time);
    }

    public static void OnVideoStartTimeChanged(Changed<NetworkedVolcapController> changed)
    {
        changed.Behaviour.FastFoward();
    }

    private int GetTimeFromPercentage(float percentage)
    {
        if (holoVideoObject.fileInfo.duration100ns != 0)
            return Mathf.FloorToInt((holoVideoObject.fileInfo.duration100ns*1f / 10000) * percentage);
        else
            return Mathf.FloorToInt(hologramDurationInNs * percentage);
    }

    public float GetTimeOfVolcap()
    {
        if (holoVideoObject.fileInfo.duration100ns != 0)
            hologramDurationInNs = holoVideoObject.fileInfo.duration100ns * 1f / 10000000;

        return hologramDurationInNs;
    }
    #endregion FastFoward

    #endregion ----Methods----
}
