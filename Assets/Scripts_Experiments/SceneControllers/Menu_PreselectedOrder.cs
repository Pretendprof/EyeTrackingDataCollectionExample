using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ViveSR.anipal.Eye;
using System.Linq;

/// <summary>
/// Example of a menu controller for a preselected set and order of experimental scenes
/// Order of scene presentation can be randomized using RandomizeSceneOrder()
/// </summary>
public class Menu_PreselectedOrder : MonoBehaviour
{
    public InputField partIDField;

    public Button StartButton;

    public string[] scenes;

    public GameObject focusText;

    public Text calibrationText;
    public FocusCubeBehavior calibrationObject;

    // used to determine when SR is launched. 
    bool srIsLoaded = false;

    private void Start()
    {
        StartCoroutine(WaitForSRFramework());
    }

    private void Update()
    {
        if(!srIsLoaded)
            return;

        if(Input.GetKeyDown(KeyCode.DownArrow))
        {
            CalibrateEye();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartExperiment();
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            StartCoroutine(delaySetCalibration());
        }

        if(Input.GetKeyDown(KeyCode.A))
        {
            ToggleFocusText(!focusText.activeSelf);
        }
    }

    /// <summary>
    /// Waits for SR framework to start before making start button interactable. 
    /// </summary>
    /// <returns></returns>
    IEnumerator WaitForSRFramework()
    {
        StartButton.interactable = false;
        partIDField.interactable = false;

        float safety = 0;

        while (!srIsLoaded)
        {
            if (SRanipal_Eye_Framework.Status == SRanipal_Eye_Framework.FrameworkStatus.WORKING)
            {
                srIsLoaded = true;
            }

            safety += Time.deltaTime;

            if (safety > 60f)
            {
                Debug.LogError("SRanipal Framework Timeout. Framework not initialized within 60 seconds");

                srIsLoaded = true;

                Application.Quit();
            }

            yield return null;
        }

        // StartButton.interactable = true;
        partIDField.interactable = true;
    }

    public void UpdateParticipantID()
    {
        ExperimentController.Instance.participantID = partIDField.text;
        StartButton.interactable = true;
    }

    public void StartExperiment()
    {
        //ExperimentController.Instance.experimentScenes.AddRange(RandomizeSceneOrder());
        //ExperimentController.Instance.experimentScenes.AddRange(scenes);

        //ExperimentController.Instance.LoadNextScene();

        StartCoroutine(DelayStart());
    }

    IEnumerator DelayStart()
    {
        yield return new WaitForSeconds(2);

        ExperimentController.Instance.experimentScenes.AddRange(scenes);

        ExperimentController.Instance.LoadNextScene();
    }

    // run Vive Eye calibration
    public void CalibrateEye()
    {
        SRanipal_Eye_v2.LaunchEyeCalibration();

        bool need = true;

        int error = SRanipal_Eye_API.IsUserNeedCalibration(ref need);

        if(need)
        {
            calibrationText.text = " Calibration FAILED!!!";
        }
        else
        {
            calibrationText.text = " Calibration Complete";
            StartCoroutine(delaySetCalibration());

        }

    }

    IEnumerator delaySetCalibration()
    {
        yield return new WaitForSeconds(1);

        SetCalibrationCheck();
    }

    void SetCalibrationCheck()
    {
        calibrationObject.SetHMDRelitivePosition(Vector3.forward);

        ToggleFocusText(false);
    }

    void ToggleFocusText(bool val)
    {
        focusText.SetActive(val);
    }

    /// <summary>
    /// Fisher Yates to randomize scene order if needed
    /// </summary>
    /// <returns></returns>
    string[] RandomizeSceneOrder()
    {
        if (scenes.Length < 1)
            return new string[0];

        string[] tmp = scenes;
        System.Random rng = new System.Random();

        int n = tmp.Length;

        while(n > 1)
        {
            n--;
            int k = rng.Next(n + 1);

            string val = tmp[k];
            tmp[k] = tmp[n];
            tmp[n] = val;                
        }

        return tmp;
    }


}
