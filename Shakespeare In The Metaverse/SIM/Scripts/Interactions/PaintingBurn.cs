using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaintingBurn : MonoBehaviour
{
    public MeshRenderer meshRenderer;
    public float delay;
    public float animTime;
    public void BurnPainting()//Called on target object painting event
    {
        StartCoroutine(_BurnPainting());
    }

    IEnumerator _BurnPainting()
    {
        if (delay > 0)
            yield return new WaitForSeconds(delay);

        float initValue = -1.7f;
        float endValue = 1;

        float elapsedTime = 0;
        while (elapsedTime < animTime)
        {
            float t = elapsedTime / animTime;

            float lerpedValue = Mathf.Lerp(initValue, endValue, t);
            foreach (var material in meshRenderer.materials)
                material.SetFloat("_BurnVal", lerpedValue);


            elapsedTime += Time.deltaTime;

            yield return null;
        }
        foreach (var material in meshRenderer.materials)
            material.SetFloat("_BurnVal", endValue);


        initValue = .05f;
        endValue = -0.15f;
        elapsedTime = 0;
        animTime /= 2;
        while (elapsedTime < animTime)
        {
            float t = elapsedTime / animTime;

            float lerpedValue = Mathf.Lerp(initValue, endValue, t);
            foreach (var material in meshRenderer.materials)
                material.SetFloat("_YET", lerpedValue);

            elapsedTime += Time.deltaTime;

            yield return null;
        }
        foreach (var material in meshRenderer.materials)
            material.SetFloat("_YET", endValue);
    }
}
