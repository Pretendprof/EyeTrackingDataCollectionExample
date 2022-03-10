using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Used to fade in and out camera during scene or task setup changes
/// </summary>
public class CameraFadeBehavior : MonoBehaviour
{
    Renderer[] rends;

    void Awake()
    {
        gameObject.SetActive(true);
        rends = GetComponentsInChildren<Renderer>();   
    }

    public void BeginFadeIn(float FadeTimeSec)
    {
        gameObject.SetActive(true);

        StartCoroutine(FadeIn(FadeTimeSec));
    }

    public void BeginFadeOut(float FadeTimeSec)
    {
        gameObject.SetActive(true);

        StartCoroutine(FadeOut(FadeTimeSec));
    }

    private IEnumerator FadeOut(float FadeTimeSec)
    {
        for (int i = 0; i < rends.Length; i++)
        {
            Color col = rends[i].material.color;

            float alpha = 0;

            while (col.a <= 1)
            {
                col = AdjustAlpha(col, alpha / FadeTimeSec);

                rends[i].material.color = col;

                alpha += Time.deltaTime;

                yield return null;
            }
        }

    }

    private IEnumerator FadeIn(float FadeTimeSec)
    {
        for (int i = 0; i < rends.Length; i++)
        {
            Color col = rends[i].material.color;

            float alpha = FadeTimeSec;

            while (col.a >= 0)
            {
                col = AdjustAlpha(col, alpha / FadeTimeSec);

                rends[i].material.color = col;

                alpha -= Time.deltaTime;

                yield return null;
            }

            gameObject.SetActive(false);

        }

    }

    Color AdjustAlpha(Color col, float a)
    {
        return new Color(col.r, col.g, col.b, a);
    }


}
