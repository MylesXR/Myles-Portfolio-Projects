using Kreis.Audio;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomSoundPlayer : MonoBehaviour
{
    public void PlayRandomSound()
    {
        int randomIndex = Random.Range(1, 4);    // Get a random index
        KreisAudioManager.PlayAudio("SFX_PIECE_IN_" + randomIndex);
    }
}
