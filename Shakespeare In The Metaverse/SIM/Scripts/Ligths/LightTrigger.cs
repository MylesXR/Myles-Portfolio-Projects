using MagicLightmapSwitcher;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class LightTrigger : MonoBehaviour
{
    #region ----Fields----
    [Header("References")]
    public GameObject MLSmanager;
    public StoredLightingScenario lightingScenario;

    [Header("LightsData")]
    public bool[] lightsState = new bool[3];
    public List<StoredLightmapData> lightmapsData = new List<StoredLightmapData>();

    private Dictionary<string, StoredLightmapData> lightmapsDataTable = new Dictionary<string, StoredLightmapData>();
    private RuntimeAPI runtimeAPI;
    private string previousLightMap = "00";
    private string currentLightMapIndex = "00";

    public float daySwitchTransitionTime = 3;
    private bool shouldBlend = false;
    #endregion ----Fields----

    #region ----Methods----
    IEnumerator Start()
    {
        runtimeAPI = new RuntimeAPI();
        foreach (var lightmapData in lightmapsData)
            lightmapsDataTable.Add(lightmapData.dataName, lightmapData);

        currentLightMapIndex = String.Join("", lightsState.Select(b => b ? 1 : 0).ToArray());
        for (int i = 0; i < lightsState.Length; i++)
            lightsState[i] = false;

        yield return new WaitForSeconds(1.5f);
        UpdateLightIndex();
    }

    void LateUpdate()
    {
        if (!shouldBlend)
            return;

        if (runtimeAPI.currentBlendingTime > 1)
            shouldBlend = false;

        runtimeAPI.BlendLightmaps(1, lightingScenario);
    }

    // Use to turn on a light
    public void SwitchLight(int indexOfLight)
    {
        Debug.Log("Switching light: " + indexOfLight);
        if (indexOfLight > lightsState.Length - 1)
            indexOfLight = lightsState.Length - 1;
        else if (indexOfLight < 0)
            indexOfLight = 0;

        lightsState[indexOfLight] = !lightsState[indexOfLight];
        UpdateLightIndex();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            return;
        UpdateLightIndex();
    }
    //TODO: If two lights lighted at same time, the blending doesn't occur because of the coroutine. It just swaps two lighted ligthmap.

    public void Update()
    {
        if (Keyboard.current.pKey.wasPressedThisFrame)
            SwitchLight(0);
        if (Keyboard.current.lKey.wasPressedThisFrame)
            Destroy(MLSmanager);
    }

    private void OnDestroy()
    {
        Destroy(MLSmanager);
    }

    [ContextMenu("Change Daylight")]
    public void ChangeLightToDaylight()//Reference on porter target object
    {
        previousLightMap = currentLightMapIndex;
        currentLightMapIndex = "DAY";
        ChangeLightmapScenario();
        StartCoroutine(ChangeSkybox());
    }

    IEnumerator ChangeSkybox()
    {
        int initValue = 0;
        int endValue = 1;

        float elapsedTime = 0;
        while (elapsedTime < daySwitchTransitionTime)
        {
            Debug.Log("BF Elapsed: " + elapsedTime);
            float t = elapsedTime / daySwitchTransitionTime;

            float lerpedValue = Mathf.Lerp(initValue, endValue, t);
            RenderSettings.skybox.SetFloat("_SkyboxSwitch", lerpedValue);

            elapsedTime += Time.deltaTime;

            yield return null;
            Debug.Log("AF Elapsed: " + elapsedTime);
        }
        RenderSettings.skybox.SetFloat("_SkyboxSwitch", endValue);
    }

    private void OnDisable()
    {
        RenderSettings.skybox.SetFloat("_SkyboxSwitch", 0);
    }

    private void UpdateLightIndex()
    {
        previousLightMap = currentLightMapIndex;
        currentLightMapIndex = String.Join("", lightsState.Select(b => b ? 1 : 0).ToArray());
        ChangeLightmapScenario();
    }

    [ContextMenu("Change lm")]
    private void ChangeLightmapScenario()
    {
        try
        {
            lightingScenario.blendableLightmaps[0].lightingData = lightmapsDataTable[previousLightMap];
            lightingScenario.blendableLightmaps[1].lightingData = lightmapsDataTable[currentLightMapIndex];

            lightingScenario.lastBlendableLightmapsCount = 2;
            lightingScenario.SynchronizeCustomBlendableData(true);

            runtimeAPI.ResetBlendingTime(0);
            StopAllCoroutines();
            StartCoroutine(EnableBlending());
        }
        catch (Exception e)
        {
            StopAllCoroutines();
        }
    }
    IEnumerator EnableBlending()
    {
        MLSmanager.SetActive(false);
        yield return new WaitForEndOfFrame();
        MLSmanager.SetActive(true);
        shouldBlend = true;
    }
    #endregion ----Methods----
}
