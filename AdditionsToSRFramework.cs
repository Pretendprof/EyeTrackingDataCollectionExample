using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdditionsToSRFramework : MonoBehaviour
{
    /// <summary>
    /// These code snippits had been added to the SRanipal_Eye_Framework.cs file to deal with some issues of file order and timing in the original project. Not all of this code may be needed, but it should be put into the SRanipal_Eye_Framework.cs
    /// and the existing Awake(), Start(), and OnDestroy() functions should be overwritten in that file. 
    /// </summary>

    public enum FrameworkStatus { STOP, START, WORKING, ERROR, NOT_SUPPORT, STOPPING } // Added STOPPING to allow for Unity stopping the framework while another thread is still listening

    private void Awake()
    {
        StartCoroutine(DelayAwake());
    }

    IEnumerator DelayAwake()
    {
        yield return new WaitForSeconds(0.15f);

        if (Mgr != null && Mgr != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Mgr = this;
        }

        DontDestroyOnLoad(Mgr);
    }

    void Start()
    {
        StartCoroutine(DelayStart());
    }

    IEnumerator DelayStart()
    {
        yield return new WaitForSeconds(0.15f);

        StartFramework();

    }

    void OnDestroy()
    {
        if (Mgr != null && Mgr == this)
        {
            // Added to ensure that listening thread doesn't try to access framework after stopped
            Status = FrameworkStatus.STOPPING;

            StopFramework();
        }
    }
}
