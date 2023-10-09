using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnderworldLampOn : MonoBehaviour
{
    public MeshRenderer meshRenderer;
    public int materialIndex;
    public Color initColor;
    public Color finalColor;
    public float transitionTime;

    public void Start()
    {
        meshRenderer.sharedMaterials[materialIndex].SetColor("_EmissionColor", initColor * 4f);
        meshRenderer.sharedMaterials[materialIndex].color = initColor;
    }

    [ContextMenu("Turn on")]
    public void TurnOnLamp()
    {
        StartCoroutine(_TurnOnLamp());
    }

    IEnumerator _TurnOnLamp()
    {
        float elapsedTime = 0;
        meshRenderer.sharedMaterials[materialIndex].SetFloat("_EmissionScaleUI", 10.4f);
        while (elapsedTime < transitionTime)
        {
            float t = elapsedTime / transitionTime;

            Color lerpedValue = Color.Lerp(initColor, finalColor, t);

            meshRenderer.sharedMaterials[materialIndex].color = lerpedValue;
            meshRenderer.sharedMaterials[materialIndex].SetColor("_EmissionColor", lerpedValue * 4f);

            elapsedTime += Time.deltaTime;

            yield return null;
        }

        meshRenderer.sharedMaterials[materialIndex].color = finalColor;
        meshRenderer.sharedMaterials[materialIndex].SetColor("_EmissionColor", finalColor * 4f);
    }
}
