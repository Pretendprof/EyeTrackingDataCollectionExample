using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ViveSR.anipal.Eye;
using System.Collections;

/// <summary>
/// Example controller for an experiment menu that allows user to select and run an individual experiment scene
/// </summary>
public class ExperimentMenu_Single : MonoBehaviour
{
    public Dropdown conditionDD;
    public InputField partIDField;
    public Button StartButton;
  
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(WaitForSRFramework());

        InitializeConditions();
    }

    /// <summary>
    /// Waits for SR framework to start before making start button interactable. 
    /// </summary>
    /// <returns></returns>
    IEnumerator WaitForSRFramework()
    {
        StartButton.interactable = false;

        float safety = 0;

        bool ready = false;

        while(!ready)
        {
            if(SRanipal_Eye_Framework.Status == SRanipal_Eye_Framework.FrameworkStatus.WORKING)
            {
                ready = true;
            }

            safety += Time.deltaTime;

            if(safety > 60f)
            {
                Debug.LogError("SRanipal Framework Timeout. Framework not initialized within 60 seconds");

                ready = true;

                Application.Quit();
            }

            yield return null;
        }

        StartButton.interactable = true;
    }

    public void SelectCondition()
    {
        ExperimentController.Instance.currentCondition = (ExperimentController.Conditions)conditionDD.value;
    }

    void InitializeConditions()
    {
        conditionDD.ClearOptions();
        List<ExperimentController.Conditions> opts = Enum.GetValues(typeof(ExperimentController.Conditions)).Cast<ExperimentController.Conditions>().ToList();
        
        conditionDD.AddOptions(opts.ConvertAll(x => x.ToString()));
    }

    public void UpdateParticipantID()
    {
        ExperimentController.Instance.participantID = partIDField.text;
    }

    public void StartExperiment()
    {
        SetSceneInExpController();

        //ExperimentController.Instance.LoadNextScene();
        StartCoroutine(DelayStart());
    }


    IEnumerator DelayStart()
    {
        yield return new WaitForSeconds(2);

        ExperimentController.Instance.LoadNextScene();
    }


    void SetSceneInExpController()
    {
        ExperimentController.Instance.experimentScenes.Clear();

        switch (ExperimentController.Instance.currentCondition)
        {
            case ExperimentController.Conditions.MultiTarget:
                {
                    ExperimentController.Instance.experimentScenes.Add("GridTargetEco");
                    break;
                }
            case ExperimentController.Conditions.HMDLag:
                {
                    ExperimentController.Instance.experimentScenes.Add("HMDLag");
                    break;
                }
            case ExperimentController.Conditions.EcoActions:
                {
                    ExperimentController.Instance.experimentScenes.Add("EcologicalAction");
                    break;
                }
            default:
                {
                    break;
                }
        }
    }

    public void CalibrateEye()
    {
        SRanipal_Eye_v2.LaunchEyeCalibration();

        //bool need = true;

        //int error = SRanipal_Eye_API.IsUserNeedCalibration(ref need);

    }
}
