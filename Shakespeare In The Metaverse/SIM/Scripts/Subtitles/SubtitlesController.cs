using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;
using System;

public class SubtitlesController : MonoBehaviour
{
    public GameObject canvas;
    public TMP_Text subtitleTMP;
    private string currentText = "";

    public void ShowText(string fullText, float delayBetweenLetters = .1f)
    {
        canvas.SetActive(true);
        StartCoroutine(_ShowText(fullText, delayBetweenLetters));
    }

    IEnumerator _ShowText(string fullText, float delayBetweenLetters)
    {
        subtitleTMP.text = "";
        for (int i = 0; i < fullText.Length; i++)
        {
            char currentLetter = fullText.Substring(i, 1).ToCharArray()[0];
            if (currentLetter == '+')
            {
                delayBetweenLetters -= .01f;
                continue;
            }

            if (currentLetter == '-')
            {
                delayBetweenLetters += .01f;
                continue;
            }
            if (currentLetter == '*')
            {
                yield return new WaitForSeconds(delayBetweenLetters * 5);
                continue;
            }

            subtitleTMP.text += currentLetter;
            yield return new WaitForSeconds(delayBetweenLetters);
            if (currentLetter == ',' || currentLetter == '.')
                yield return new WaitForSeconds(delayBetweenLetters * 5);
        }
    }
}
