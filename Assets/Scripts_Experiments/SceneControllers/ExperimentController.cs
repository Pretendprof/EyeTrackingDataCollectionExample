using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Experiment controller follows a Singleton pattern and maintains experimentally relevant objects across 
/// experiment scene changes
/// </summary>
public class ExperimentController : MonoBehaviour
{
    private static ExperimentController _instance;
    public static ExperimentController Instance { get { return _instance; } }

    // A ViveEyeDevice that gets new data from the eyetracker
    public ViveEyeDevice viveEye = new ViveEyeDevice();

    // possitble experiment conditions
    public enum Conditions
    {
        MultiTarget,
        EcoActions,
        HMDLag // Note this was used to determine HMD-Eyetracker latency. 
    }

    // current experimental condition
    public Conditions currentCondition = Conditions.MultiTarget;

    // a participant ID value that can be set by the researcher
    public string participantID = "999";

    // the menuScenes name
    public string MenuSceneName = "Menu_SingleSelect";

    // List of scenes that comprise the exeperiment
    public List<string> experimentScenes = new List<string>();
    int curSceneIndex = 0;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }

        DontDestroyOnLoad(_instance);
    }

    /// <summary>
    /// Loads next scene in the list of experiment scenes
    /// </summary>
    public void LoadNextScene()
    {
        // if all experiment scenes are complete returns to menu
        if (curSceneIndex >= experimentScenes.Count)
        {
            curSceneIndex = 0;
            SceneManager.LoadScene(MenuSceneName);
            return;
        }

        int tmp = curSceneIndex;
        curSceneIndex++;

        StartCoroutine(LoadAsyncScene(experimentScenes[tmp]));
    }

    // used to load scenes more smoothly and reduce breaks/stutters in VR experience (Still use fade in/out for transition)
    IEnumerator LoadAsyncScene(string scene)
    {
        Scene currentS = SceneManager.GetActiveScene();

        SceneManager.LoadScene(scene, LoadSceneMode.Additive);

        yield return null;

        AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(currentS);
    }

    // make sure that ViveEye device is shut down when Unity program exits. 
    private void OnDisable()
    {
       viveEye.StopDevice();
    }

    private void OnApplicationQuit()
    {
        viveEye.StopDevice();
    }
}
