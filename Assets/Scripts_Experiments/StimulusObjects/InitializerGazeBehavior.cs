using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the gaze compentent of initialization. Uses user Eye gaze to indicate readiness. 
/// </summary>
public class InitializerGazeBehavior : MonoBehaviour
{
    // Object used to initialize trial
    InitializerBehaviour gazeInitializer;

    // used to guide user to align HMD orientation to a specified point (typically the initializer)
    public FocusCubeBehavior alignmentObject;

    public ViveEyeController viveEye;

    // specify whether alignment is required
    public bool requireHMDAlign = false;

    bool isAligned = false;
    
    bool allignmentActive = true;

    void Update()
    {
        // don't do anything if no viveEyeDevice is present or ready
        if(viveEye == null || !viveEye.isInit)
            return;

        // Check alignment
        if(requireHMDAlign)
        {
            if(allignmentActive)
            {
                MoveHMDAlignObject();
            }
        }

        // Check for eyegaze on initialization object
        RegionCast();
    }

    void RegionCast()
    {
        // for when user must direct HMD towards the initializer
        if(requireHMDAlign)
        {
            if (!isAligned)
            {
                if(gazeInitializer != null)
                    ResetInitializer();

                return;
            }
        }

        int layer = ~(1 << 10);

        RaycastHit hit;
        // check if user is looking at initializer
        if (Physics.Raycast(viveEye.hmdPos + viveEye.gazeOffset, viveEye.hmdGazeVec, out hit, 20, layer))
        {
            InitializerBehaviour tmp = hit.collider.GetComponent<InitializerBehaviour>();
                
            // if user is looking at an initializer
            if (tmp != null)
            {
                // if an itializer is already registered, then do nothing
                if (gazeInitializer != null)
                    return;

                // Otherwise activate initializer countdown/fade
                tmp.Activate();
                gazeInitializer = tmp;
            }
            else if(gazeInitializer != null) // If tmp is null but gazeInitializer had been previously assigned. NOTE this happens once trial is initialized. 
            {
                ResetInitializer();
            }
        }
        else // If nothing is hit by raycast
        {
            // If gazeInitializer had been previously assigned
            if (gazeInitializer != null)
            {
                ResetInitializer();
            }
        }
    }

    /// <summary>
    /// Resets initializer when gaze and/or alignment are not directed at initializer
    /// </summary>
    void ResetInitializer()
    {
        // if the participant isn't ready
        if (!gazeInitializer.participantReady)
        {
            gazeInitializer.ResetInitializer(); // reset initializer to starting state
        }
        else if(requireHMDAlign)
        {
            isAligned = false;
            allignmentActive = false;
            alignmentObject.RemoveFocus();
        }

        // remove previous assignment of initializer
        gazeInitializer = null;
    }

    /// <summary>
    /// handles moving alignment object to indicate where HMD is directed. 
    /// </summary>
    void MoveHMDAlignObject()
    {
        int layer = 1 << 10;
        RaycastHit hit;

        if (Physics.Raycast(viveEye.hmdPos + viveEye.gazeOffset, viveEye.hmdVec, out hit, 20, layer))
        {
            alignmentObject.SetPosition(hit.point, true);
        }
    }

    /// <summary>
    ///  when alignment object is in contact with initializer
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "Allignment")
        {
            isAligned = true;
        }
    }

    /// <summary>
    /// When alignment object is not in contact with initialzer
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Allignment")
        {
            isAligned = false;
        }
    }

    /// <summary>
    /// Resets alignment object
    /// </summary>
    public void ResetAllignment()
    {
        allignmentActive = true;
        alignmentObject.SetScale();
    }


}

