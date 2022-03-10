using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// ViveEyeController for Eyetracking validation study. 
/// 
/// Assigns viveEye from an ExperimentController instance when scene is loaded
/// </summary>
public class ViveEyeController_ExperimentVersion : ViveEyeController
{
    public LineRenderer linesHMD, linesEye;
    public Transform gazePoint;

    public bool visualizeEyeMovement = false;


    protected override void OnEnable()
    {
        StartCoroutine(DelayedOnEnable());
    }

    // waits for ExperimentController to be loaded before running base OnEnable
    IEnumerator DelayedOnEnable()
    {
        while(viveEye == null)
        {
            viveEye = ExperimentController.Instance.viveEye;
            yield return null;
        }

        base.OnEnable();

        //recorder.delayWrite = true;
    }

    protected override void Update()
    {
        base.Update();

        if (visualizeEyeMovement)
        {
            if (linesEye != null)
            {
                DrawHMDVectors(linesEye);
            }

            if (gazePoint != null)
            {
                VisualizeGazePosition(gazePoint);
            }
        }
    }

    //protected override Dictionary<string, double[]> AddtionalVars(List<SteamVR_Utils.RigidTransform> vrDeviceData, SteamVR_Utils.RigidTransform hmdLoc, Dictionary<string, double[]> values)
    //{
    //    if(vrDeviceData.Count < 2)
    //    {
    //        return null;
    //    }

    //    UnityEngine.Debug.Log("Total: " + vrDeviceData.Count);

    //    for(int i = 1; i < vrDeviceData.Count; i++)
    //    {
    //        UnityEngine.Debug.Log("Device " + i + ": " + vrDeviceData[i].pos + "  " + vrDeviceData[i].rot);
    //    }

    //    return base.AddtionalVars(vrDeviceData, hmdLoc, values); 
    //}

    // doesn't shut down ViveEyeDevice in ExperimentController instance so that it can be used for next scene. 
    protected override void OnDisable()
    {
        viveEye.ViveDataPushAction -= RecieveData;

        recorder.OnQuit();
    }

}
