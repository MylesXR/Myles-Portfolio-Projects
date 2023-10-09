using MUXR.Networking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndGame : MonoBehaviour
{
    public void Endgame(int secondsBeforeEnd)
    {
        Debug.Log("EndingGame");
        StartCoroutine(_EndGame(secondsBeforeEnd));
    }

    IEnumerator _EndGame(int secondsBeforeEnd)
    {
        if (FusionManager.Runner != null && FusionManager.Runner.GameMode == Fusion.GameMode.Server)
            yield break;

        yield return new WaitForSeconds(secondsBeforeEnd);
        Application.Quit();
    }
}
