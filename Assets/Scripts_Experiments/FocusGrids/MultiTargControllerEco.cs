using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

/// <summary>
/// Task Controller for the Multitarget Experimental task. 
/// 
/// Task has one phase
/// 1) Participant is presented focus targets in positions defined by visual angles and target grid dimensions/distance.
/// </summary>
public class MultiTargControllerEco : TaskController
{
    public float restTime = 20;
    public RigRecenter rp;

    public bool StabilizeTarget = false;
    public bool RandomizeTargetOrder = false;
    
    // defines the number of rows and columns in a grid
    [Tooltip("Rows and Columns of stimuli. NOTE: Array must be same size as displayGridDim")]
    public Vector2Int[] targetGridSize;

    // For each display grid dimensions there must be a target grid size. 
    [Tooltip("First two dimensions are total visual angle, z component is distance from viewer. NOTE: Array must be same size as targetGridSize")]
    public Vector3[] displayGridDimensions;
    public int numberOfPresentations = 1;

    public CameraFadeBehavior sceneFader;

    public InitializerGazeBehavior iniGazeBehavior;

    // Start is called before the first frame update
    void Start()
    {
        // A check if target grid size and grid dimensions arrays are the same size, If not exit scene. 
        if(targetGridSize.Length != displayGridDimensions.Length)
        {
            Debug.LogError("Length of grid dimension and size arrays does not match, Cannot run.");

            ExperimentController.Instance.LoadNextScene();

            return;
        }

        // setup file id for this scene
        fileID = ExperimentController.Instance.participantID + "_" + fileID;

        StartCoroutine(StartTask());
    }

    IEnumerator StartTask()
    {
        // Recenter camera rig
        rp.ActivateRecenter();

        // Setup Initializers
        initializerObj.SetupInitializer();
        iniGazeBehavior.ResetAllignment();

        float fadeTime = 3f;
        // Fade from black to scene
        sceneFader.BeginFadeIn(fadeTime);
        yield return new WaitForSeconds(fadeTime);

        // Set stabalization mode. 
        initializerObj.stabalizePos = StabilizeTarget;
        focusObj.stabalizePos = StabilizeTarget;

        StartCoroutine(TaskRun());
    }

    IEnumerator TaskRun()
    {
        yield return new WaitForSeconds(1f);

        // Provide instructions
        SetUserInstructions(0, false);

        yield return new WaitForSeconds(2f);

        //Wait for user to initialize task
        while (!initializerObj.participantReady)
        {
            yield return null;
        }

        initializerObj.participantReady = false;
        SetUserInstructions("", false);

        // Setup an array for grid boundaries
        VisualAngleBoundaries[] vbArray = new VisualAngleBoundaries[displayGridDimensions.Length];

        // calculate grid boundaries. 
        for (int j = 0; j < displayGridDimensions.Length; j++)
        {
            vbArray[j] = new VisualAngleBoundaries(displayGridDimensions[j]);
        }

        // presents all grid locations once and then pauses for the number of presentations specified
        for (int jj = 0; jj < numberOfPresentations; jj++)
        {
            // initialize grid locations relative to viewer based on vbArray. Gives a list of these locations
            List<Vector3Int> gridVals = SetupTargetGrid(1);

            // Presentation Logic
            for (int i = 0; i < gridVals.Count; i++)
            {
                // initializer 
                initializerObj.SetupInitializer();
                iniGazeBehavior.ResetAllignment();

                while (!initializerObj.participantReady)
                {
                    yield return null;
                }

                // get stimulus locations for this trial
                int xLoc = gridVals[i].x;
                int yLoc = gridVals[i].y;
                int vbIDX = gridVals[i].z;

                // setup string to encode distance in output file
                string distString = ((int)(vbArray[vbIDX].distance * 10f)).ToString("000");

                // wait an amount of time before displaying stimulus
                yield return new WaitForSeconds(0.3f);

                // setup trialname for file and start recording
                string tmp = string.Join("_", new string[] { fileID, distString, xLoc.ToString("00"), yLoc.ToString("00"), displayGridDimensions[vbIDX].x.ToString("00"), displayGridDimensions[vbIDX].y.ToString("00"), jj.ToString("0"), i.ToString("00") });
                viveControls.StartRecording(tmp, true);

                // wait one frame to ensure recording has started
                yield return null;

                // present stimulus
                SetFocusLocation(xLoc, yLoc, vbIDX, vbArray[vbIDX]);

                // wait amount of time specified by task time
                yield return new WaitForSeconds(taskTime);

                // stop recording 
                viveControls.StopRecording();

                yield return null;

                // remove object (after stoping recording to avoid artifiacts in final frames due to stimulus disappearing. 
                focusObj.RemoveFocus();
            }

            if ((jj + 1) == numberOfPresentations)
                continue;

            // Logic for pause. 
            yield return new WaitForSeconds(0.5f);

            SetUserInstructions("Relax. You may start when the squares reappear.", false);

            yield return new WaitForSeconds(restTime);

            SetUserInstructions("", false);
        }

        // After last stimulus is removed
        yield return new WaitForSeconds(0.5f);
                
        //  Provide indication that trials are complete
        SetUserInstructions("Relax", false);

        //Wait before fade out
        yield return new WaitForSeconds(3f);

        float fadeTime = 3f;
        // fade to black to ease any changes beetween scenes
        sceneFader.BeginFadeOut(fadeTime);
        
        // 
        yield return new WaitForSeconds(fadeTime);

        // Load next scene
        ExperimentController.Instance.LoadNextScene();
    }

    /// <summary>
    /// Sets location of focus object based on grid index, grid size, and distance 
    /// </summary>
    /// <param name="xLoc"> row index</param>
    /// <param name="yLoc"> column index </param>
    /// <param name="cur"> Current target grid index</param>
    /// <param name="vb"> visual boundaries to be applied</param>
    void SetFocusLocation(int xLoc, int yLoc, int cur, VisualAngleBoundaries vb)
    {
        // calculate a ratio indicating grid location for x dimension
        float xRatio = 0;
        if (xLoc != 0)
            xRatio = xLoc / ((float)targetGridSize[cur].x - 1);
        else if (targetGridSize[cur].x == 1)
            xRatio = 0.5f;

        // calculate a ratio indicating grid location for y dimension
        float yRatio = 0;
        if (yLoc != 0)
            yRatio = yLoc / ((float)targetGridSize[cur].y - 1);
        else if (targetGridSize[cur].y == 1)
            yRatio = 0.5f;

        // calculate postion give boundaries and ratio
        float xPos = xRatio * (vb.xMax * 2) + vb.xMin;
        float yPos = yRatio * (vb.yMax * 2) + vb.yMin;

        // Set stimulus location relative to HMD
        focusObj.SetHMDRelitivePosition(new Vector3(xPos, yPos, vb.distance));
    }


    /// <summary>
    /// Sets up list of grid vector3s for all possible grid locations and depths (if no input, randomize entire
    /// list of repeated values. 
    /// </summary>
    /// <returns></returns>
    List<Vector3Int> SetupTargetGrid()
    {
        List<Vector3Int> gridVals = SetupTargetGrid(numberOfPresentations);

        return gridVals;
    }

    /// <summary>
    /// Sets up list of grid vector3s for all possible grid locations and depths
    /// </summary>
    /// <param name="repititions">number of repititions to include for each grid location</param>
    /// <returns></returns>
    List<Vector3Int> SetupTargetGrid(int repititions)
    {
        List<Vector3Int> gridVals = new List<Vector3Int>();

        for (int ii = 0; ii < repititions; ii++)
        {
            for (int iii = 0; iii < displayGridDimensions.Length; iii++)
            {
                for (int i = targetGridSize[iii].y - 1; i > -1; i--)
                {
                    for (int j = 0; j < targetGridSize[iii].x; j++)
                    {
                        gridVals.Add(new Vector3Int(j, i, iii));
                    }
                }
            }
        }

        if (RandomizeTargetOrder)
        {
            return RandomizeTargets(gridVals);
        }

        return gridVals;
    }

    /// <summary>
    /// Impliments a simple Fisher Yates randomization algorthm to randomize the order stimulus locations in a list. 
    /// </summary>
    /// <param name="tmp">Stimulus location list</param>
    /// <returns></returns>
    List<Vector3Int> RandomizeTargets(List<Vector3Int> tmp)
    {
        System.Random rng = new System.Random();
        int n = tmp.Count;
        
        // Fisher Yates to randomize list order
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);

            Vector3Int val = tmp[k];
            tmp[k] = tmp[n];
            tmp[n] = val;
        }

        return tmp;
    }

}
