using System.Collections;
using UnityEngine;

/// <summary>
/// Task Controller for the EcoActions Experimental task. 
/// 
/// Task has two phases
/// 1) Participant sits as task object moves towards them
/// 2) Participant moves (by moving through physical space) towards task object
/// </summary>
public class EcoActionsTaskController : TaskController
{
    public RigRecenter rp;

    public CameraFadeBehavior sceneFader;

    public EcoStimulusObject ecoObj;

    public InitializerGazeBehavior iniGazeBehavior;

    public float objMinDistance = 1.5f;
    public float objMovespeed = 1;
    public Vector3 actionDirection = Vector3.forward;

    public Color taskPhaseCompleteColor = Color.red;

    bool isReady = false;

    private void Start()
    {
        initializerPos = new Vector3(0, -1, 10);
        instructionPos = new Vector3(0, 1, 7);

        // setup file id for this scene
        fileID = ExperimentController.Instance.participantID + "_" + fileID;

        StartCoroutine(StartTask());
    }

    private void Update()
    {
        // spacebar starts the condition when the participant is in place
        if(Input.GetKeyDown(KeyCode.Space))
        {
            isReady = true;
        }

        // in case recenter is incorrect. Z can be pressed to reset camera position to head
        if(Input.GetKeyDown(KeyCode.Z))
        {
            rp.ActivateRecenter(true);
        }
    }

    IEnumerator StartTask()
    {
        // do not begin trial until participant is in position (standing) and affirms readiness (researcher presses spacebar to initialize)
        while (!isReady)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.1f);

        // This sets the participant in centered on chair in the room 
        rp.ActivateRecenter(true);

        initializerObj.SetupInitializer();
        iniGazeBehavior.ResetAllignment();

        // Fade in from black
        sceneFader.BeginFadeIn(2f);

        yield return new WaitForSeconds(2f);

        StartCoroutine(TaskRun());
    }

    IEnumerator TaskRun()
    {
        yield return new WaitForSeconds(1f);

        // Instruct User to initialize the task with further task instructions
        SetUserInstructions(0, false);

        // Wait for participant to indicate ready. 
        while (!initializerObj.participantReady)
        {
            yield return null;
        }

        // Once participant indicates ready. Remove instructions and initializer
        initializerObj.participantReady = false;
        SetUserInstructions("", false);

        // Begin recording
        viveControls.StartRecording(fileID + "_sitting", true);

        // calculate distance of object from participant
        float distance = ecoObj.CalculateDistance(actionDirection);

        // wait one second before starting
        yield return new WaitForSeconds(1f);

        // Move object towards the participant until it is min distance away
        while (distance > objMinDistance)
        {
            // Move object
            ecoObj.MoveObject(-objMovespeed, actionDirection);

            // check distance
            distance = ecoObj.CalculateDistance(actionDirection);

            yield return null;
        }

        // Wait for 1 second before removing stimulus
        yield return new WaitForSeconds(1f);

        viveControls.StopRecording();
        
        // Reset object to starting place
        ecoObj.ResetToBegining();

        // Provide new instructions to user
        SetUserInstructions(1, false);

        // Set up initailzation objects
        initializerObj.SetupInitializer();
        iniGazeBehavior.ResetAllignment();

        // Wait for user 
        while (!initializerObj.participantReady)
        {
            yield return null;
        }

        // remove intializer instructions and object
        initializerObj.participantReady = false;
        SetUserInstructions("", false);

        // Check distance to user
        distance = ecoObj.CalculateDistance(actionDirection);
        
        viveControls.StartRecording(fileID + "_walking", true);

        // Wait until user has walked to closer than minDistance
        while (distance > objMinDistance)
        {
            distance = ecoObj.CalculateDistance(actionDirection);

            yield return null;
        }

        // changes color of object to indicate that participant can stop walking
        ecoObj.SignalTaskPhaseComplete(taskPhaseCompleteColor);

        yield return new WaitForSeconds(1f);

        viveControls.StopRecording();

        SetUserInstructions("Relax. Please remove the headset and rest", false);

        // This delay is primarily to give user time to remove HMD
        yield return new WaitForSeconds(5f);

        // fade out in case HMD is still on. 
        sceneFader.BeginFadeOut(2f);

        yield return new WaitForSeconds(2f);

        // Move to next scene
        ExperimentController.Instance.LoadNextScene();
    }

}
