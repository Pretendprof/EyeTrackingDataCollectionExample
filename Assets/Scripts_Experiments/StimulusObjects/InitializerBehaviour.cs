using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Focus object that is used to initialize trials
/// 
/// NOTE: For fade behaviors to work, the primary object material Rendering mode should be set to Transparent
/// </summary>
public class InitializerBehaviour : FocusCubeBehavior
{
    [HideInInspector]
    public bool participantReady = false;

    Renderer myRend;
    Collider myCollider;

    bool initializing = false, fading = false;

    void Awake()
    {
        //expCont = GameObject.FindObjectOfType<ExperimentController>();
        myRend = GetComponent<Renderer>();
        myCollider = GetComponent<Collider>();
    }

    /// <summary>
    /// When called initializer starts to fade. 
    /// </summary>
    public void Activate()
    {
        if (!fading)
        {
            StartCoroutine(Fade());
        }
    }

    /// <summary>
    /// Resets initalizer to initial setup
    /// </summary>
    public void ResetInitializer()
    {
        SetupInitializer();
    }

    /// <summary>
    /// Sets up initializer 
    /// </summary>
    public void SetupInitializer()
    {
        StopAllCoroutines();

        SetScale();

        initializing = false;

        myCollider.enabled = true;

        fading = false;
        myRend.enabled = true;
        Color color = myRend.material.color;
        color.a = 1;
        myRend.material.color = color;

        participantReady = false;
    }

    /// <summary>
    /// Indicates that participant is ready. 
    /// </summary>
    void InitializeTrial()
    {
        participantReady = true;
    }

    /// <summary>
    /// Fading 
    /// </summary>
    /// <returns></returns>
    IEnumerator Fade()
    {
        fading = true;

        initializing = true;
        Color color = myRend.material.color;

        while (initializing)
        {
            color.a -= 0.01f;

            myRend.material.color = color;

            if (color.a < 0.25f)
            {
                myCollider.enabled = false;
                myRend.enabled = false;
               
                initializing = false;

                InitializeTrial();

                break;
            }

            yield return new WaitForSeconds(0.01f);
        }

    }

    /// <summary>
    /// removes initializer
    /// </summary>
    public void ClearInitializer()
    {
        myCollider.enabled = false;
        myRend.enabled = false;

        fading = false;

    }

}
