using Fusion;
using Kreis.Audio;
using MUXR.Networking;
using MyBox;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Spawnable))]
public class VolcapSequenceController : NetworkBehaviour
{
    #region ----Fields----
    [Header("Timeline")]
    public VolcapTimeline timeline;

    [Header("Preferences")]
    public bool disableOnAwake = false;
    public bool playSequenceOnEnabled = false;
    public float outtroDelay = 2f;

    [Header("Volcap sequence events")]
    public List<VolcapClipData> volcapClips = new List<VolcapClipData>();
    public UnityEvent onSequenceEnd;
    #endregion ----Fields----

    #region ----Methods----
    #region Init
    private void OnValidate()
    {
        timeline.MaxValue = GetSequenceMaxTimeVolcap();
    }

    public override void Spawned()
    {

        Spawnable spawnable;
        if (TryGetComponent(out spawnable))
            spawnable.OnSpawned += Init;

        foreach (var volcapDataTemp in volcapClips)
            volcapDataTemp .volcapClip.gameObject.SetActive(true);
    }

    public void Init(GameObject originalSpawnable)
    {
        if (disableOnAwake)
            this.transform.parent.gameObject.SetActive(false);
    }

    public void OnEnable()
    {
        try
        {
            if (playSequenceOnEnabled)
                PlaySequence();
        }
        catch (Exception e) { }
    }
    #endregion Init

    #region PlaySequence
    float initialTime = 0;
    int initialTimeline = 0;
    bool isPlaying = false;

    public void Update()
    {
        if (!isPlaying)
            return;
        timeline.Value += Time.deltaTime;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_PlaySequence()
    {
        PlaySequence();
    }

    private float GetSequenceMaxTimeVolcap()
    {
        float maxTimeVolcap = -1;
        foreach (var volcapData in volcapClips)
        {
            float finalTimeOfVolcap = volcapData.startTime + volcapData.volcapClip.GetTimeOfVolcap();
            if (finalTimeOfVolcap > maxTimeVolcap)
                maxTimeVolcap = finalTimeOfVolcap;
        }
        return maxTimeVolcap;
    }

    [ContextMenu("Pause sequence")]
    public void PauseSequence()
    {
        StopAllCoroutines();
        isPlaying = false;
        foreach (var volcapData in volcapClips)
            volcapData.volcapClip.PauseVolcap();
    }

    [ContextMenu("Play sequence")]
    public void PlaySequence()
    {
        initialTime = Time.time;
        isPlaying = true;
        VolcapClipData lastClip = null;
        List<VolcapClipData> tempVolcapClips = new List<VolcapClipData>(volcapClips);
        foreach (var volcapDataTemp in tempVolcapClips)
        {
            var volcapData = (VolcapClipData)volcapDataTemp.Clone();
            if (!volcapData.enable)
                volcapData.volcapClip.gameObject.SetActive(false);
            else
            {
                //volcapData.volcapClip.ResetVolcap();
                SetVolcapStartTime(volcapData, ref lastClip);
                if (CheckIfInsideTimeline(volcapData))
                    StartCoroutine(PlayVolcap(volcapData));
            }
        }

        if (Object.HasStateAuthority)
            StartCoroutine(WaitForSeconds(GetSequenceMaxTimeVolcap() + outtroDelay, () => RPC_OnSequenceEnd()));
    }

    private bool CheckIfInsideTimeline(VolcapClipData volcapData)
    {
        if (volcapData.endTime > timeline.Value)
        {
            if (volcapData.startTime < timeline.Value && Object.HasStateAuthority)
            {
                //StartTime
                var volcapPercentage = (timeline.Value - volcapData.startTime) / volcapData.volcapClip.GetTimeOfVolcap();

                volcapData.volcapClip.VideoStartTime = Mathf.Min(.99f, volcapPercentage);
                volcapData.volcapClip.FastFoward();

                //Delay to dissapear
                float volcapEndTime = volcapData.startTime + volcapData.volcapClip.GetTimeOfVolcap();
                if (timeline.Value > volcapEndTime)
                    volcapData.delayToDissapear = volcapData.endTime - timeline.Value;

                //Volcap Events
                foreach (var volcapEvent in volcapData.eventsList)
                {
                    bool shouldExecuteEvent = timeline.Value < volcapEvent.delayToPlayEvent + volcapData.startTime;
                    if (shouldExecuteEvent)
                        volcapEvent.delayToPlayEvent = (volcapEvent.delayToPlayEvent + volcapData.startTime) - timeline.Value;
                    volcapEvent.shouldExecuteEvent = shouldExecuteEvent;
                }
                volcapData.startTime = 0;
            }
            else
                volcapData.startTime = Mathf.Max(0, volcapData.startTime - timeline.Value);
            return true;
        }
        return false;
    }

    private void SetVolcapStartTime(VolcapClipData volcapData, ref VolcapClipData lastClip)
    {
        if (volcapData.shouldStartFromLastOne && lastClip != null)
        {
            if (Object != null && Object.HasStateAuthority)
                volcapData.startTime = lastClip.startTime + lastClip.volcapClip.GetTimeOfVolcap();
        }

        if (!volcapData.shouldLoop || volcapData.endTime == 0)
            volcapData.endTime = volcapData.startTime + volcapData.volcapClip.GetTimeOfVolcap() + volcapData.delayToDissapear;
        Debug.Log("Seconds to wait: " + volcapData.startTime);

        lastClip = volcapData;
    }

    IEnumerator PlayVolcap(VolcapClipData volcapData)
    {
        yield return new WaitForSeconds(volcapData.startTime);
        Debug.Log($"Init volcap: {gameObject.name} time {volcapData.startTime}");

        volcapData.volcapClip.gameObject.SetActive(true);
        //StartCoroutine(_VolcapDissolveRoutine(false, volcapData.volcapClip.transform.GetChild(0).GetComponent<MeshRenderer>(), 0));

        if (Object != null && Object.HasStateAuthority)
        {
            Debug.Log("Start Volcap sequence clip");
            yield return new WaitForSeconds(2);
            StartVolcap(volcapData.volcapClip);
        }
        PlayVolcapEvents(volcapData.eventsList);

        foreach (var transition in volcapData.volcapTransitionData)
            DoTranstion(transition, volcapData.volcapClip.HoloVideoObject.transform);

        if (!Object.HasStateAuthority)
            yield break;

        ////Execute dissolve effect out
        //RPC_OnVolcapEnd(volcapData.volcapClip, volcapData.endTime);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_OnVolcapEnd(NetworkedVolcapController volcap, float secondsToWait)
    {
        StartCoroutine(_VolcapDissolveRoutine(true, volcap.transform.GetChild(0).GetComponent<MeshRenderer>(), secondsToWait));
    }

    public void DissolveOutVolcap(NetworkedVolcapController volcap)
    {
        StartCoroutine(_VolcapDissolveRoutine(true, volcap.transform.GetChild(0).GetComponent<MeshRenderer>(), 0.01f));
    }

    public void DissolveInVolcap(NetworkedVolcapController volcap)
    {
        StartCoroutine(_VolcapDissolveRoutine(false, volcap.transform.GetChild(0).GetComponent<MeshRenderer>(), 0.01f));
    }

    public void AppearVolcap(NetworkedVolcapController volcap)
    {
        MeshRenderer meshRenderer = volcap.transform.GetChild(0).GetComponent<MeshRenderer>();
        meshRenderer.sharedMaterial.SetFloat("_EffectLocation", Mathf.FloorToInt(-2 + (meshRenderer.transform.position.y)));
    }

    public void DisappearVolcap(NetworkedVolcapController volcap)
    {
        volcap.transform.GetChild(0).GetComponent<MeshRenderer>().enabled = false;
    }

    IEnumerator _VolcapDissolveRoutine(bool enableDissolve, MeshRenderer meshRenderer, float secondsToWait)
    {
        if (secondsToWait > 0)
            yield return new WaitForSeconds(secondsToWait);
        //Lerp between two values
        if (enableDissolve)
            meshRenderer.enabled = true;

        int onValue = Mathf.FloorToInt(-2 + (meshRenderer.transform.position.y));
        int offValue = Mathf.FloorToInt(4 + (meshRenderer.transform.position.y));

        int initValue = enableDissolve ? onValue : offValue;
        int endValue = enableDissolve ? offValue : onValue;

        float elapsedTime = 0;
        while (elapsedTime < transitionTime)
        {
            float t = elapsedTime / transitionTime;

            float lerpedValue = Mathf.Lerp(initValue, endValue, t);
            meshRenderer.sharedMaterial.SetFloat("_EffectLocation", lerpedValue);

            elapsedTime += Time.deltaTime;

            yield return null;
        }
        meshRenderer.sharedMaterial.SetFloat("_EffectLocation", endValue);

        if (enableDissolve)
        {
            meshRenderer.enabled = false;
            meshRenderer.gameObject.SetActive(false);
        }
    }

    public void DoTranstion(VolcapTransitionData transitionData, Transform volcap)
    {
        StartCoroutine(_DoTranstion(transitionData, volcap));
    }

    IEnumerator _DoTranstion(VolcapTransitionData transitionData, Transform volcap)
    {
        yield return new WaitForSeconds(transitionData.startTime);
        KreisAudioManager.PlayAudio("D_FOOTSTEPS", new AudioJobOptions(volumen: 0.5f));
        float timeElapsed = 0f;
        Vector3 volcapOriginalPosition = volcap.position;

        while (timeElapsed < transitionData.duration)
        {
            float t = timeElapsed / transitionData.duration;
            volcap.position = Vector3.Lerp(volcapOriginalPosition, transitionData.targetPoint.position, t);

            timeElapsed += Time.deltaTime;
            yield return null;
        }

        volcap.position = transitionData.targetPoint.position;
    }

    public float transitionTime = 4f;

    private void PlayVolcapEvents(List<VolcapEventData> volcapEventsData)
    {
        foreach (var volcapEvent in volcapEventsData)
        {

            Debug.Log($"Volcap events init: {gameObject.name} time {volcapEvent.delayToPlayEvent}");
            if (volcapEvent.hasDelay)
                StartCoroutine(WaitForSeconds(volcapEvent.delayToPlayEvent, () => volcapEvent.events?.Invoke()));
            else
                volcapEvent.events?.Invoke();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_OnSequenceEnd()
    {
        isPlaying = false;
        onSequenceEnd?.Invoke();
    }

    private void StartVolcap(NetworkedVolcapController volcapEventController)
    {
        Debug.Log("Volcap start on sequence" + volcapEventController.name);
        volcapEventController.StartVolcap();
    }
    #endregion PlaySequence

    #region Helpers
    public IEnumerator WaitForSeconds(float seconds, Action callback, bool isRealtime = true)
    {
        if (isRealtime)
            yield return new WaitForSecondsRealtime(seconds);
        else
            yield return new WaitForSeconds(seconds);

        callback?.Invoke();
        Debug.Log($"Volcap events finish: {gameObject.name} ");
    }
    #endregion Helpers
    #endregion ----Methods----
}
