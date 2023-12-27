using Fusion;
using MUXR.Networking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeController : MonoBehaviour //shows the active cube puzzle to the score board
{
    public bool shouldShowInit = true;
    public int cubeIndex = 1;
    private void Start()
    {
        if (FusionManager.Runner.GameMode != GameMode.Server)
            this.gameObject.SetActive(!shouldShowInit);
    }

    void RemoveAnimator()
    {
        this.GetComponent<Animator>().enabled = false;
    }

    public string GetHighscoreLevel()
    {
        return $"LVL{cubeIndex}";
    }

}
