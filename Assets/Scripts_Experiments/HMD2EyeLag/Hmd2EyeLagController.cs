using System.Collections;
using UnityEngine;

/// <summary>
/// A vestibulo-ocular reflex task to test latency between HMD movement and Eye-tracking values. Cross-corrleation analysis 
/// can be used to determine delay required to aligne HMD and Eye-tracking signals. 
/// 
/// Task as implimented here instructs horizontal and then vertical head movements. 
/// </summary>
public class Hmd2EyeLagController : TaskController
{
    // simple animation to illistrate instructions
    public HMDMovementAni hmdAni;

    public Vector3 iniPosition = new Vector3(0, -1, 5);

    // Start is called before the first frame update
    void Start()
    {                
        StartCoroutine(StartTask());
    }

    IEnumerator StartTask()
    {
        yield return new WaitForSeconds(0.5f);

        fileID = ExperimentController.Instance.participantID + "_HMDLag";

        initializerObj.stabalizePos = true;
        focusObj.stabalizePos = true;

        StartCoroutine(TaskRun());
    }

    IEnumerator TaskRun()
    {
        yield return new WaitForSeconds(2f);

        SetUserInstructions(0);

        hmdAni.BeginRotation(Vector3.up, 1.5f, 30, 90);

        SetInitializer(iniPosition);   

        while (!initializerObj.participantReady)
        {
            yield return null;
        }

        hmdAni.EndRotation();

        initializerObj.participantReady = false;
        userInstructions.text = "";

        yield return new WaitForSeconds(1);

        viveControls.StartRecording(fileID, true);

        focusObj.IntializePosition();

        yield return new WaitForSeconds(taskTime);

        viveControls.StopRecording();

        focusObj.RemoveFocus();

        yield return new WaitForSeconds(0.5f);

        SetUserInstructions("Relax");

        yield return new WaitForSeconds(3f);

        SetUserInstructions(1);

        hmdAni.BeginRotation(Vector3.right, 1.5f, 30);

        SetInitializer(iniPosition);

        while (!initializerObj.participantReady)
        {
            yield return null;
        }

        hmdAni.EndRotation();

        initializerObj.participantReady = false;
        userInstructions.text = "";

        yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));

        viveControls.StartRecording(fileID, true);

        focusObj.IntializePosition();

        yield return new WaitForSeconds(taskTime);

        viveControls.StopRecording();

        focusObj.RemoveFocus();

        yield return new WaitForSeconds(0.5f);
        
        SetUserInstructions("Relax");

        yield return new WaitForSeconds(3f);

        SetUserInstructions("");

        ExperimentController.Instance.LoadNextScene();

    }
}
