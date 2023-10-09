using Kreis.Audio;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioHelper : MonoBehaviour
{
    #region ----Fields----
    public List<AudioCue> audioCues = new List<AudioCue>();
    #endregion ----Fields----

    #region ----Methods----
    #region Key
    public void OnValidate()
    {
        if (audioCues == null || audioCues.Count < 1)
            audioCues = new List<AudioCue>() { new AudioCue() };
    }

    public void ChangeAudioKey(string newAudioKey)
    {
        audioCues[0].key = newAudioKey;
    }

    public void ChangeAudioKeyAndPlay(string newAudioKey)
    {
        ChangeAudioKey(newAudioKey);
        AudioPlay();
    }
    #endregion Key

    #region Audio Options
    public void ChangeAudioOptions(AudioJobOptions options)
    {
        audioCues[0].options = options;
    }

    public void SetLoop(bool loop)
    {
        audioCues[0].options.loop = loop;
    }

    public void SetDelay(float delay)
    {
        audioCues[0].options.delay = delay;
    }

    public void SetPosition(Transform position)
    {
        audioCues[0].options.position = position;
    }

    public void SetVolume(float volume)
    {
        audioCues[0].options.volume = volume;
    }

    public void SetFadeIn(bool fadeIn)
    {
        if (fadeIn)
            audioCues[0].options.fadeIn = new AudioFadeInfo(true, 2);
        else
            audioCues[0].options.fadeIn = new AudioFadeInfo(false, 0);
    }

    public void ChangeAudioOptionsAndPlay(AudioJobOptions options)
    {
        ChangeAudioOptions(options);
        AudioPlay();
    }

    public void SetCopyIndex(int index)
    {
        audioCues[0].options.copyIndex = index;
    }

    public void SetPositionAndPlay(Transform position)
    {
        SetPosition(position);
        AudioPlay();
    }
    #endregion Audio Options

    #region Play
    [ContextMenu("TestAudioPlay")]
    public void AudioPlay(int index = 0)
    {
        Debug.Log("Playing some stuff: " + audioCues[index].key);
        KreisAudioManager.PlayAudio(audioCues[index].key, audioCues[index].options);
    }

    [ContextMenu("TestAudioPlayAll")]
    public void AudioPlayAllCues()
    {
        Debug.Log("Playing all cues");
        for (int i = 0; i < audioCues.Count; i++)
        {
            audioCues[i].options.interruptCurrentCue = false;
            AudioPlay(i);
        }
    }

    [ContextMenu("TestAudioStop")]
    public void AudioStop(int index = 0)
    {
        KreisAudioManager.StopAudio(audioCues[index].key, audioCues[index].options);
    }

    public void StopAllAudioOfATrack(AudioTrack track)
    {
        KreisAudioManager.StopAllAudioInATrack(track);
    }
    #endregion Play
    #endregion ----Methods----
}
