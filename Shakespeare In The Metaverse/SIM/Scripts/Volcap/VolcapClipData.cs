using MyBox;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class VolcapClipData : ICloneable
{
    public bool enable = true;
    public bool shouldStartFromLastOne;
    [Range(0.01f, 500)] public float startTime;

    public bool shouldLoop = false;
    [ConditionalField("shouldLoop")] public float endTime;
    [Range(0, 500)] public float delayToDissapear;

    public List<VolcapTransitionData> volcapTransitionData;
    public NetworkedVolcapController volcapClip;
    public List<VolcapEventData> eventsList;

    public object Clone()
    {
        return this.MemberwiseClone();
    }
}

[Serializable]
public class VolcapTransitionData
{
    public Transform targetPoint;
    public float startTime;
    public float duration;
}

[Serializable]
public class VolcapEventData
{
    public bool shouldExecuteEvent = true;
    public bool hasDelay = false;
    [ConditionalField(nameof(hasDelay))][Range(0.01f, 120)] public float delayToPlayEvent = 0.1f;

    public UnityEvent events;
}
