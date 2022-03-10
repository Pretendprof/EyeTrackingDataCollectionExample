using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System;
using ViveSR.anipal.Eye;
using System.Runtime.InteropServices;
/*
This code was built to provide an example of how to impliment eyetracking using the HTC Vive Pro Eye and
the SRanipal SDK for scientific resaerch contexts. If you use this or any related code for published scientific 
research, please cite 

Lamb, M., Brundin, M., Pérez Luque, E., Billing, E. (2022) Eye-tracking beyond peripersonal space in virtual reality: Validation and Best practices. Frontiers in Virtual Reality

and provide a link to the github repository https://github.com/Pretendprof. If you use this
software in a commercial product, along with attribution, please consider supporting the developer or the developer's resaerch
financially. 
This software is provided as an example. No gaurantee of ongoing support is given. Please read and understand the 
code before utilizing in a critical system. 

Maurice Lamb
University of Skövde
email: maurice.lamb at his dot se
*/

/// <summary>
/// ViveEyeDevice provides the primary task interface to the SRanipal Dll. It is designed to run as fast as the eye tracker (120hz for 
/// vive Pro Eye). 
/// 
/// By default the device converts the EyeData_V2 class from SRanipal to a Dictionary<string, float[]>. This is because in the context of data 
/// collection 1) string headers will make later analysis much simpler and less error prone; 2) As the data is passed along, further
/// processing will extend the set of variables to be recorded Dictionaries are better suited to changes based on differnt experimental
/// need. 3) It seems that unless the eye trackers gets considerably faster, or the virtual scene significantly more resource 
/// intensive, speed differences are negligible. 
/// 
/// Note that this script runs a thread seperate from the main Unity thread, so all values must be moved to the main thread before Unity 
/// can use them. For pure data collection, there is no need to move values to the main Unity thread. For an example of how to process 
/// eyetracker data along with sharing values to the main thread, see ViveEyeController.cs. 
/// </summary>
public class ViveEyeDevice
{
    // An action that can be triggered whenever new Eyetracker data is availible. Dictionary contains all values from eyetrackers along 
    // with human readable string for tag. 
    public Action<Dictionary<string, double[]>> ViveDataPushAction;
    public Action<EyeData_v2> ViveEyeDataPushAction;

    bool parseDataToDict = true;

    // Task for checking eye tracking data along with cancelation token to ensure clean program exit
    Task eyeTrackerTask;
    CancellationTokenSource cts;
    private Stopwatch timing;

    // Strings used for data output keys and organizing data across Classes. 
    string[] eyeVarsStr = { "Origin", "GazeDir", "PupilSensor", "Diameter", "Openness", "Validity" };
    string[] eyeTypesStr = { "Combine", "Left", "Right" };

    // list of eye data variables names for organizing and data headers. Key is primary id (and used to sperate eye types) the list<string> is a list of sub-eye data types. 
    Dictionary<string, List<string>> eyeVars = new Dictionary<string, List<string>>();

    // List of all possible eye data variable names along with an integer specifying how many values are associated with the eye data variable
    public Dictionary<string, int> availKeys = new Dictionary<string, int>();

    // EyeData_V2 types for data pulled for SRanipal SDK
    public static EyeData_v2 eye_dataTask = new EyeData_v2();
    public EyeData_v2 eye_dataGet = new EyeData_v2();

    // Keeps track of if task is initialized and running
    public bool isInit = false;
    volatile bool running = false;

    // for clean shutdown
    public bool shuttingDown = false;

    public bool useCallback = true;

    public bool avail = false;

    /// <summary>
    /// The next 3 helper functions convert data types to float arrays. 
    /// </summary>
    double[] SetPositionVar(Vector3 val)
    {
        double[] tmp = new double[3] { val.x, val.y, val.z };

        return tmp;
    }

    double[] SetPositionVar(Vector2 val)
    {
        double[] tmp = new double[3] { val.x, val.y, 0f };

        return tmp;
    }

    double[] SetSingleVar(double val)
    {
        double[] tmp = new double[1] { val };

        return tmp;
    }


    /// <summary>
    /// Initialize Vive eye tracker. If using GetEyeVals then you can pass false for 
    /// threadRun to only initalize the eyeVars that you want to pay attention to. 
    /// </summary>
    /// <param name="varNames"> List of all eye varible names to be passed from eye tracker to ViveDataPushAction</param>
    /// <param name="threadRun"> Specify if task should be started </param>
    public bool Init(Dictionary<string, List<string>> varNames, bool threadRun = true)
    {
        if (threadRun && running) // task already running. Assumes user wants to listen to task. true means a task is there to listen to
            return true;

        if (isInit && !threadRun) // if task run is false the tell user Init is already done. GetEyeData can be called
            return false;

        // set up and start new task
        if (threadRun)
        {

            // used to keep track of timing across processes
            timing = new Stopwatch();
            timing.Start();

            cts = new CancellationTokenSource();

            eyeTrackerTask = new Task(Worker, cts.Token, TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness);
            eyeTrackerTask.Start();
        }

        eyeVars.Clear();
        eyeVars = varNames;

        return true;
    }

    /// <summary>
    /// Required class for IL2CPP scripting backend support
    /// </summary>
    internal class MonoPInvokeCallbackAttribute : System.Attribute
    {
        public MonoPInvokeCallbackAttribute() { }
    }

    private static void EyeCallback(ref EyeData eye_data)
    {
        EyeData eye_dataTask = eye_data;
        UnityEngine.Debug.Log("seq " + eye_dataTask.frame_sequence);

    }

    /// <summary>
    /// Initialize all ViveEye Vars. If using GetEyeVals then you can pass false for 
    /// threadRun. 
    /// </summary>
    /// <param name="threadRun"> Specify if task should be started </param>
    public bool Init(bool threadRun = true)
    {
        if (threadRun && running) // thread already running. Assumes user wants to listen to thread. true means a thread is there to listen to
            return true;

        if (isInit && !threadRun) // if thread run is false the tell user Init is already done. GetEyeData can be called
            return false;

        if (threadRun)
        {
            timing = new Stopwatch();
            timing.Start();

            cts = new CancellationTokenSource();
            eyeTrackerTask = new Task(Worker, cts.Token, TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness);
            eyeTrackerTask.Start();

            //SRanipal_Eye.WrapperRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye.CallbackBasic)EyeCallback));

        }

        if (parseDataToDict)
            SetupEyeVars();

        return true;
    }

    /// <summary>
    /// for enabling/disabling dictionary parsing. 
    /// </summary>
    /// <param name="val"> true parses eyedata to dictionary and calls dictionary action, false does not pares data or call dictionary action</param>
    public void SetDictionaryParsing(bool val)
    {
        // if enabling dictionary parsing after thread is set to run, then need to setup eyeVars
        if(val & running)
        {
            SetupEyeVars();
        }

        parseDataToDict = val;
    }

    /// <summary>
    /// Sets up all Eye data variable names for data organization and output headers
    /// </summary>
    void SetupEyeVars()
    {
        eyeVars.Clear();
        availKeys.Clear();

        // for frames from eye tracker
        eyeVars.Add("EyeFrames", new List<string>());
        availKeys.Add("EyeFrames", 1);

        // for system stopwatch timer
        availKeys.Add("ViveTiming", 1);
        
        // Organize by eye type (Left, Right, Combined)
        foreach (string s in eyeTypesStr)
        {
            eyeVars.Add(s, new List<string>());
            foreach (string ev in eyeVarsStr)
            {
                if (s == "Combine" && (ev == "Diameter" || ev == "Openness" || ev == "PupilSensor"))
                    continue;

                int cnt = 3;
                if (ev == "Diameter" || ev == "Openness" || ev == "Validity")
                    cnt = 1;

                if (ev == "PupilSensor")
                    cnt = 2;

                eyeVars[s].Add(ev);

                availKeys.Add((s + "_" + ev), cnt); 
            }            
        }
    }

    /// <summary>
    /// If you want to check values independent of the task below.
    /// </summary>
    /// <param name="values"> Parsed Eye Data values. </param>
    /// <returns></returns>
    public bool GetEyeVals(out Dictionary<string, double[]> values)
    {
        values = new Dictionary<string, double[]>();
        ViveSR.Error error;

        // If a task was initialized pull then return the most recent eye tracking data from the task
        if (running)
        {
            eye_dataGet = eye_dataTask;

            error = ViveSR.Error.WORK;
        }
        else // no task is running so get data now. 
        {
          error = SRanipal_Eye_API.GetEyeData_v2(ref eye_dataGet);
        }

        // if data was successfully pulled then parse data. 
        if (error == ViveSR.Error.WORK)
        {
            values = ParseEyeVals(eye_dataGet);

            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Get data that returns unparsed EyeData_V2 class
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    public bool GetEyeVals(out EyeData_v2 values)
    {
        values = new EyeData_v2();

        ViveSR.Error error;

        // If a task was initialized then pull then return the most recent eye tracking data from the task
        if (running)
        {
            values = eye_dataTask;

            return true;
        }
        else // no task is running so get data now. 
        {
            error = SRanipal_Eye_API.GetEyeData_v2(ref eye_dataGet);
        }

        // if data was successfully pulled 
        if (error == ViveSR.Error.WORK)
        {
            values = eye_dataGet;

            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Worker constantly checks vive device as fast as possible. Each new frame is parsed and 
    /// an event triggered to pass the data to Unity scripts. Note that this event cannot trigger 
    /// MonoBehaviour dependent events, so the event values will need to be passed through Update or Fixedupdate 
    /// before using. 
    /// </summary>
    void Worker()
    {
        int prevFrame = -1;
        running = true;

        try
        {
            while (running)
            {
                // Handles cancelation before get data
                if (cts.IsCancellationRequested)
                    cts.Token.ThrowIfCancellationRequested();

                // If framwork stops working this pauses eye data processing
                // Unity can shut down framework before this device, this check should avoid crashes in that case
                if (SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.WORKING)
                    continue;

                // get eye data
                ViveSR.Error error = SRanipal_Eye_API.GetEyeData_v2(ref eye_dataTask);
                // if data was recieved
                if (error == ViveSR.Error.WORK)
                {
                    EyeData_v2 tmpEyeData = eye_dataTask; // creates local copy of eye_dataTask


                    //  UnityEngine.Debug.Log("OTside: " +Stopwatch.GetTimestamp() / (Stopwatch.Frequency / 1000));
                    // Check if this is new data
                    if (tmpEyeData.frame_sequence != prevFrame)
                    {
                        prevFrame = tmpEyeData.frame_sequence;
                        //UnityEngine.Debug.Log("inside: " + Stopwatch.GetTimestamp() / (Stopwatch.Frequency / 1000));

                        // by default dictionary is enabled, this can be disabled if desired.
                        if (parseDataToDict)
                        {
                            Dictionary<string, double[]> vals = ParseEyeVals(tmpEyeData);

                            // only push data if others are listening
                            ViveDataPushAction?.Invoke(vals);
                        }

                        // only push data if others are listening
                        ViveEyeDataPushAction?.Invoke(tmpEyeData);
                    }
                }
            }
        }
        catch (Exception) // Anything that disrupts getting eye data retrival, but primarily catches cancellation exception
        {

        }
        finally
        {
            // for anyone listening if the thread is running. Used by StopDevice()
            running = false; 

            // ensure that ref to SRanipal_Eye_API is released
            eye_dataTask = new EyeData_v2();
        }
    }

    /// <summary>
    /// Parses EyeData_v2 for use and labeling at data output.  
    /// </summary>
    /// <param name="eye_Data"></param>
    /// <returns></returns>
    Dictionary<string, double[]> ParseEyeVals(EyeData_v2 eye_Data)
    {
        Dictionary<string, double[]> parsedVals = new Dictionary<string, double[]>();

        parsedVals.Add("ViveTiming", new double[1] { Stopwatch.GetTimestamp() }); // / (Stopwatch.Frequency / 1000) }); //(float)timing.Elapsed.TotalMilliseconds });

        foreach (string s in eyeVars.Keys)
        {
            if (s == "EyeFrames")
            {
   
                double[] tmp = new double[1] { eye_Data.frame_sequence };

                parsedVals.Add(s, tmp);
            }
            else
            {
                string eye = s;

                if (!Enum.TryParse(eye.ToUpper(), out GazeIndex gaze))
                    continue;

                SingleEyeData data;

                switch (gaze)
                {
                    case GazeIndex.COMBINE:
                        {
                            data = eye_Data.verbose_data.combined.eye_data;
                            break;
                        }
                    case GazeIndex.LEFT:
                        {
                            data = eye_Data.verbose_data.left;
                            break;
                        }
                    case GazeIndex.RIGHT:
                        {
                            data = eye_Data.verbose_data.right;

                            break;
                        }
                    default:
                        {
                            continue;
                        }
                }

                foreach (string var in eyeVars[eye])
                {
                    string tmpVar = var;
                    double[] tmp;

                    switch (tmpVar)
                    {
                        case "Origin":
                            {
                                tmp = SetPositionVar(data.gaze_origin_mm);
                                break;
                            }
                        case "GazeDir":
                            {
                                tmp = SetPositionVar(data.gaze_direction_normalized);
                                break;
                            }
                        case "PupilSensor":
                            {
                                tmp = SetPositionVar(data.pupil_position_in_sensor_area);
                                break;
                            }
                        case "Diameter":
                            {
                                tmp = SetSingleVar(data.pupil_diameter_mm);
                                break;
                            }
                        case "Openness":
                            {
                                tmp = SetSingleVar(data.eye_openness);
                                break;
                            }
                        case "Validity":
                            {
                                float validity = 0;

                                if (data.GetValidity(SingleEyeDataValidity.SINGLE_EYE_DATA_GAZE_DIRECTION_VALIDITY))
                                    validity = 1;

                                tmp = SetSingleVar(validity);
                                break;
                            }
                        default:
                            continue;
                    }

                    string outVar = s + "_" + tmpVar;

                    parsedVals.Add(outVar, tmp);
                }
            }
        }
        return parsedVals;        
    }

    /// <summary>
    /// Stop the device safely. This must be called on Application quit or Disable wherever a
    /// ViveEyeDevice is initialized. 
    /// </summary>
    public void StopDevice()
    {
        if (!running)
            return;

        //SRanipal_Eye.WrapperUnRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye.CallbackBasic)EyeCallback));


        cts.Cancel();

        Stopwatch saftey = new Stopwatch();
        saftey.Start();

        // a small safety to ensure that cancelation token is handled. 
        while (true)
        {
            if (saftey.ElapsedMilliseconds > 350)
            {
                break;
            }
        }

        // dispose of cancelationtokensource to ensure it isn't missed by GC
        cts.Dispose();
    }

    //~ViveEyeDevice()
    //{
    //    if (running)
    //        StopDevice();
    //}
    
}

public enum DataSendType
{
    Dictionary = (1<<0),
    ViveEyeData = (1 << 1)
}
