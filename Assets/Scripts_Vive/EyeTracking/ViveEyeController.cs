using System;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;
using System.Linq;
using System.Collections.Concurrent;
using ViveSR.anipal.Eye;

/// <summary>
/// This is a generic controller that sets up a ViveEyeDevice and listens for new data from the eyetracker. 
/// Eye data is parsed and converted for later data anaysis and then recorded. Values from the eye tracker
/// task can be passed the the main Unity thread here for use by Unity. 
/// 
/// Note that along with recieving Eye data this script also accesses OpenVR to get current head and other 
/// tracked object positions and rotations. OpenVR can provide these values faster than the Unity update
/// rate so this method acesses more up-to-date tracking data. 
/// 
/// Please cite:
/// 
/// Lamb, M., Brundin, M., Pérez Luque, E., Billing, E. (2022) Eye-tracking beyond peripersonal space in virtual reality: Validation and Best practices. Frontiers in Virtual Reality
/// 
/// if you use this code or inspiration from it for academic resaerch
/// 
/// </summary>
public class ViveEyeController : MonoBehaviour
{
    protected ViveEyeDevice viveEye;// = new ViveEyeDevice(); // viveEye thread and functions for getting data

    // Target of eyeTracking. Used for calculating head angle to test latency
    public Transform targetTransform;
    // Target position that can be passed into hmd thread
    protected Vector3 targetPos = Vector3.zero;

    // vars that take values from hmd thread and pass them to Unity main thread
    public Vector3 eyeVec { get; private set; } = Vector3.zero;
    public Vector3 hmdVec { get; private set; } = Vector3.zero;
    public Vector3 hmdPos { get; private set; } = Vector3.zero;
    public Vector3 gazePnt { get; private set; } = Vector3.zero;
    public Vector3 hmdGazeVec { get; private set; } = Vector3.zero;
    public Vector3 gazeOffset { get; private set; } = Vector3.zero;
    public Quaternion _hmdRot { get; private set; } = Quaternion.identity;

    //[HideInInspector]
    //public Vector3 eyeVec = Vector3.zero, hmdVec = Vector3.zero, hmdPos = Vector3.zero, gazePnt = Vector3.zero, hmdGazeVec = Vector3.zero, gazeOffset = Vector3.zero;
    //[HideInInspector]
    //public Quaternion _hmdRot = Quaternion.identity;
    [HideInInspector]
    public bool isInit = false;


    // offsets used to orient hmd values to camera rig
    public Transform cameraRig;
    protected Vector3 offset_RigPos = Vector3.zero;
    protected Quaternion offset_RigRot = Quaternion.identity;

    // threadsafe queue for passing data from eyetracking thread to Unity main thread
    protected ConcurrentQueue<List<Vector4>> viveData = new ConcurrentQueue<List<Vector4>>();

    // Used to delay hmd tracking values so that eye vectors to world are calcuated correctly
    protected Queue<TrackedDevicePose_t[]> delayedTracking = new Queue<TrackedDevicePose_t[]>();
    // max size of queue. maxQueue = number of frames to delay + 1
    protected int maxQueue = 3;

    // recorder of recording data
    protected DataRecorder recorder = new DataRecorder();

    // enums to allow for recording any combination of eyes
    [System.Flags]
    public enum EyeRec { Left = 1 << 0, Right = 1 << 1, Combine = 1 << 2 }
    
    [EnumFlag]
    public EyeRec eyeRec = EyeRec.Combine | EyeRec.Right;

    // dictionary for initializing recorded variable headers. string is header and int is number of columns for that value (eye independent variables)
    protected Dictionary<string, int> viveContHeader = new Dictionary<string, int>() {
        {"HMDVec", 3 },
        {"HMDPos", 3 },
        {"HMDRot", 4 },
        {"GazeTarget", 3 }
    };

    // these are eye dependent variable headers. Values depend on eye offsets. 
    protected Dictionary<string, int> viveEyeHeader = new Dictionary<string, int>() {
        {"HMD2TargAng", 3 }, 
        {"HMD2EyeAng", 3 },
        {"GazePoint", 3 }
    };

    /// <summary>
    /// Initializes controller and ViveEyeDevice. Subscribes to ViveDataPushAction. 
    /// </summary>
    protected virtual void OnEnable()
    {
        // for if the controller is maintained across scenes. 
        if (viveEye == null)
        {
            viveEye = new ViveEyeDevice();
        }

        // if cameraRig is identified then set offsets accordingly
        if (cameraRig != null)
        {
            OffsetReset();
        }

        // Starts thread on ViveEyeDevice to check for new eyedata. 
        if (viveEye.Init()) 
            viveEye.ViveDataPushAction += RecieveData;
        else
            Debug.Log("Error initializing ViveEye");

        // recorder can be set to wait to write output files until closing file recorder.
        recorder.delayWrite = false;

        isInit = true;
    }

    // When program finishes. exit cleanly
    protected virtual void OnDisable()
    {
        viveEye.ViveDataPushAction -= RecieveData;

        viveEye.StopDevice();

        recorder.OnQuit();
    }

    // Updtate is used to move data between the main unity thread and the primary data thread.  
    protected virtual void Update()
    {
        UpdateHMDData();

        // if a target is specified. This is used to calculate angles and values on the primary data thread
        UpdateTargetPosition();

        // if cameraRig is identified then set offsets accordingly (used to calculate HMD values on primary data thread)
        if (cameraRig != null && (cameraRig.position != offset_RigPos || cameraRig.rotation != offset_RigRot))
            OffsetReset();
    }

    /// <summary>
    /// pulls data off vive data thread and makes availible on main thread
    /// </summary>
    protected void UpdateHMDData()
    {
        // List of values to be pulled from hmd Thread to main thread
        List<Vector4> hmdVals = new List<Vector4>();
        
        // check that there is new data from hmd
        if (!viveData.IsEmpty)
        {
            List<Vector4> tmp;
            // pull most recent data from eyetracker thread
            while (viveData.TryDequeue(out tmp)) { hmdVals = tmp; }

            // parse recieved data.
            hmdPos = hmdVals[0];
            eyeVec = hmdVals[1];
            hmdVec = hmdVals[2];
            _hmdRot = new Quaternion(hmdVals[3][0], hmdVals[3][1], hmdVals[3][2], hmdVals[3][3]);
            hmdGazeVec = hmdVals[4];
            gazePnt = hmdVals[5];
            gazeOffset = hmdVals[6];
        }
    }

    /// <summary>
    /// Calcuates a target position. When no target is specified a position in 1m from the HMD position along Unity's +Z dimension is 
    /// used as the target. Angles calculated below are defined relative to target. 
    /// </summary>
    protected void UpdateTargetPosition()
    {
        if (targetTransform != null)
            targetPos = targetTransform.position;
        else
            targetPos = hmdPos + Vector3.forward;
    }

    /// <summary>
    /// resets offset used for calculating Hmd positon and rotation in global coordinates. 
    /// </summary>
    protected void OffsetReset()
    {
        offset_RigPos = cameraRig.position;
        offset_RigRot = cameraRig.rotation;
    }

    #region helper/example functions
    /// <summary>
    /// Drawing lines to indicate hmd and eye gaze vectors in the scene
    /// </summary>
    protected void DrawHMDVectors(LineRenderer hmd, LineRenderer eye)
    {
        hmd.SetPositions(new Vector3[] { hmdPos, hmdPos + hmdVec });
        eye.SetPositions(new Vector3[] { hmdPos, hmdPos + hmdGazeVec });
    }

    /// <summary>
    /// Drawing lines to indicate hmd and eye gaze vectors in the scene
    /// </summary>
    protected void DrawHMDVectors(LineRenderer eye)
    {
        eye.SetPositions(new Vector3[] { hmdPos, hmdPos + hmdGazeVec });
    }

    /// <summary>
    /// Sets a transform in the position of the gaze point. 
    /// </summary>
    /// <param name="transform"></param>
    protected void VisualizeGazePosition(Transform transform)
    {
        transform.position = gazePnt;
        transform.LookAt(hmdPos);
    }

    /// <summary>
    /// Keyboard functions for debugging. 
    /// </summary>
    protected void CheckKeyboardInputs()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            StartRecording();

        if (Input.GetKeyDown(KeyCode.Escape))
            StopRecording();

        if (Input.GetKeyDown(KeyCode.Q))
            SRanipal_Eye.LaunchEyeCalibration();
    }
    #endregion

    /// <summary>
    /// Starts recorder recording. Uses default file naming.
    /// </summary>
    /// <param name="startImmeadeatly"> True starts recording as soon as data file is set up, false sets up data file but doesn't start recording data</param>
    public virtual void StartRecording(bool startImmeadeatly = true)
    {
        Dictionary<string, int> header = SetHeaders();

        // setup new data file and start recording immeadeatly
        recorder.SetupNewDataFile(header, startImmeadeatly);
    }

    /// <summary>
    /// Starts recorder recording. Adds a file id to the default file naming.
    /// </summary>
    /// <param name="fileID"> string appended to front of file name </param>
    /// <param name="startImmeadeatly"> True starts recording as soon as data file is set up, false sets up data file but doesn't start recording data</param>
    public virtual void StartRecording(string fileID, bool startImmeadeatly = true)
    {
        Dictionary<string, int> header = SetHeaders();

        // setup new data file and start recording immeadeatly
        recorder.SetupNewDataFile(header, startImmeadeatly, fileID);
    }

    /// <summary>
    /// Sets up new headers for values calculated in ViveEyeController.cs based on user specfied values above and selected eyes to record
    /// </summary>
    /// <returns></returns>
    protected Dictionary<string, int> SetHeaders()
    {
        Dictionary<string, int> header = new Dictionary<string, int>();
        viveContHeader.ToList().ForEach(x => header.Add(x.Key, x.Value));
        viveEye.availKeys.ToList().ForEach(x => header.Add(x.Key, x.Value));

        List<EyeRec> eyes = Enum.GetValues(typeof(EyeRec)).Cast<EyeRec>().ToList();

        foreach (EyeRec e in eyes)
        {
            string eyeString = e.ToString();

            foreach(string k in viveEyeHeader.Keys)
            {
                string tmp = eyeString + "_" + k;
                header.Add(tmp, viveEyeHeader[k]);
            }
        }

        return header;
    }

    /// <summary>
    /// Adds to list of headers in data output file. Should be called before setting up a file for recording
    /// Used for user added headers in derived classes.
    /// make sure that 
    /// </summary>
    /// <param name="headers"> String is header string and number is int of columns</param> 
    protected void AddHeaders(Dictionary<string, int> headers)
    {
        headers.ToList().ForEach(x => viveContHeader.Add(x.Key, x.Value));
    }

    /// <summary>
    /// Stops recorder recording. See DataRecorder.cs for more details. 
    /// </summary>
    public virtual void StopRecording()
    {
        recorder.StopRecording();
    }

    /// <summary>
    /// Invoked whenever new data is availible from eyetracker per ViveEyeDevice. Gets HMD values at same rate as EyeTracker. 
    /// HMD values are delayed according to lab testing by two frames to enusure that HMD values from as close to the same time as the
    /// eye tracker data as possible. This delay seems machine independent, but this is not extensively tested. Lag is calculated based
    /// on a vestibulo-ocular reflex task (see HMDLag scene in Unity) and using cross-corrleation analysis. 
    /// </summary>
    /// <param name="values"> data from the eyetracker parsed to a dictionary </param> 
    protected virtual void RecieveData(Dictionary<string, double[]> val)
    {
        Dictionary<string, double[]> values = new Dictionary<string, double[]>(val); // shallow copy of val dictionary ref         

        // Get all transforms for connected vr devices
        List<SteamVR_Utils.RigidTransform> vrDeviceData = GetTrackedDeviceValues();

        // Get HMD postion rotation from OpenVR API
        SteamVR_Utils.RigidTransform hmdLoc = vrDeviceData[0];

        values.Add("HMDPos", SetPositionVar(hmdLoc.pos));
        values.Add("HMDRot", SetQuaternionVar(hmdLoc.rot));

        // calculate HMD forward Vector
        Vector3 tmpHmdVec = (hmdLoc.rot * Vector3.forward).normalized;
        values.Add("HMDVec", SetPositionVar(tmpHmdVec));

        List<EyeRec> eyes = Enum.GetValues(typeof(EyeRec)).Cast<EyeRec>().ToList();

        foreach(EyeRec e in eyes)
        {
            if ((eyeRec & e) != 0)
            {
                CalcEyeGazeVals(e, hmdLoc, ref values);
            }
        }

        // Calculate values for Unity Main Thread. Note Currently only uses combine gaze values
        Vector3 tmpEyeVec = new Vector3((float)values["Combine_GazeDir"][0], (float)values["Combine_GazeDir"][1], (float)values["Combine_GazeDir"][2]).normalized;
        Vector3 offset = new Vector3((float)values["Combine_Origin"][0], (float)values["Combine_Origin"][1], (float)values["Combine_Origin"][2]) * 0.001f;

        MainThreadValues(tmpEyeVec, offset, tmpHmdVec, hmdLoc);

        // add gaze target location to data output
        values.Add("GazeTarget", SetPositionVar(targetPos));

        // Provides a method for calculating additional varibles in derived classes without rewritting the rest of this function. 
        Dictionary<string, double[]> additionalVars = AddtionalVars(vrDeviceData, hmdLoc, values);

        if (additionalVars != null && additionalVars.Count > 0)
            additionalVars.ToList().ForEach(x => values.Add(x.Key, x.Value));

        // record data.
        if (recorder.isRecording)
            recorder.RecordData(values);

        OnRecieveData(values);
    }

    protected virtual void OnRecieveData(Dictionary<string, double[]> val)
    {
        
    }

    /// <summary>
    /// Calculates a set of gaze related values for later data analsysis.
    /// </summary>
    /// <param name="eye"> Eye that valuse are calcuated for</param>
    /// <param name="hmdLoc"> Hmd SteamVR_Utils.RigidTransform as provide by OpenVR API</param>
    /// <param name="values"> reference to dictionary of data values</param>
    protected void CalcEyeGazeVals(EyeRec eye, SteamVR_Utils.RigidTransform hmdLoc, ref Dictionary<string, double[]> values)
    {
        CalcEyeGazeVals(eye, hmdLoc.pos, hmdLoc.rot, ref values);
    }

    /// <summary>
    /// Calculates a set of gaze related values for later data analsysis
    /// </summary>
    /// <param name="eye"> Eye for calulations</param>
    /// <param name="hmdPos"> Hmd Position </param>
    /// <param name="hmdRot"> Hmd Rotation </param>
    /// <param name="values"> reference to dictionary of data values</param>
    protected void CalcEyeGazeVals(EyeRec eye, Vector3 hmdPos, Quaternion hmdRot, ref Dictionary<string, double[]> values)
    {
        // setup eye value keys
        string eyeString = eye.ToString();
        string gazeString = eyeString + "_GazeDir";
        string offsetString = eyeString + "_Origin";

        // Get eye gaze vector 
        Vector3 eyeVec = new Vector3((float)values[gazeString][0], (float)values[gazeString][1], (float)values[gazeString][2]).normalized;
        Vector3 offset = new Vector3((float)values[offsetString][0], (float)values[offsetString][1], (float)values[offsetString][2]) * 0.001f;

        // calculate a point in space at a distance from the vector defined by target postion along the gaze vector
        Vector3 hmdToEyeVec = hmdRot * Vector3.Scale(eyeVec, new Vector3(-1, 1, 1)).normalized; // Align gaze vector with hmd orientation
        Vector3 gazeLoc = GazePoint((hmdPos - offset), targetPos, hmdToEyeVec);
        values.Add(eyeString + "_GazePoint", SetPositionVar(gazeLoc));

        // get hmd angles in degrees relative to gaze target
        Vector3 hmdToTargVec = (targetPos - (hmdPos - offset)).normalized;        // calculate vector from hmd to target
        Vector3 hmdAngles = AngleinDegrees(hmdToTargVec, hmdRot, 90);
        values.Add(eyeString + "_HMD2TargAng", SetPositionVar(hmdAngles));

        // get eye angles in degrees relative to hmd
        Vector3 eyeAngles = AngleinDegrees(Vector3.Scale(eyeVec, new Vector3(1, -1, 1)));
        values.Add(eyeString + "_HMD2EyeAng", SetPositionVar(eyeAngles));
    }

    /// <summary>
    /// Calcuates values to be shared with Unity's main thread. Adds them to a concurrent queue. 
    /// </summary>
    /// <param name="tmpEyeVec"> An eye gaze vector </param>
    /// <param name="offset"> An offset of the eye from the HMD</param>
    /// <param name="tmpHmdVec"> An HMD vector</param>
    /// <param name="hmdLoc"> HMD pos/rotation from OpenVR</param>
    protected void MainThreadValues(Vector3 tmpEyeVec, Vector3 offset, Vector3 tmpHmdVec, SteamVR_Utils.RigidTransform hmdLoc)
    {
        // calculate a point in space at end of gaze vector
        Vector3 hmdToEyeVec = hmdLoc.rot * Vector3.Scale(eyeVec, new Vector3(-1, 1, 1)).normalized; // Align gaze vector with hmd orientation
        Vector3 gazeLoc = GazePoint(hmdLoc.pos, targetPos, hmdToEyeVec);
        Vector4 vecRot = new Vector4(hmdLoc.rot.x, hmdLoc.rot.y, hmdLoc.rot.z, hmdLoc.rot.w);         // convert hmd rotation to vector4 in order to simplify passing data to main thread
       
        // concurrent queue for threadsafe passage of data from eyetracker thread to Unity main thread
        viveData.Enqueue(new List<Vector4>() { hmdLoc.pos, tmpEyeVec, tmpHmdVec, vecRot, hmdToEyeVec, gazeLoc, offset });    
    }

    /// <summary>
    /// For derived class. Allows user to specify additional varibles and methods to be calculated. 
    /// </summary>
    /// <param name="hmdLoc"></param>
    /// <param name="values"></param>
    /// <returns></returns>
    protected virtual Dictionary<string, double[]> AddtionalVars(List<SteamVR_Utils.RigidTransform> vrDeviceData, SteamVR_Utils.RigidTransform hmdLoc, Dictionary<string, double[]> values)
    {      
        return null;
    }

    /// <summary>
    /// Gets HMD data from OpenVR in real time so that no unexpected delays are added due to Unity's frame rate
    /// Note that eyetracking pipeline requires approx. 2 frames processing time so the current eye tracking values
    /// were recorded 2 eyetracker frames ago. Accurate calculations of gaze values should be done from state of HMD when eye 
    /// values were recorded. 
    /// 
    /// </summary>
    /// <returns></returns>
    protected SteamVR_Utils.RigidTransform GetHMDLocation()
    {
        List<SteamVR_Utils.RigidTransform> tmp = GetTrackedDeviceValues(1);

        return tmp[0];
    }

    /// <summary>
    /// Gets tracked device data from OpenVR in real time so that no unexpected delays are added due to Unity's frame rate
    /// Note that eyetracking pipeline requires approx. 2 frames processing time so the current eye tracking values
    /// were recorded 2 eyetracker frames ago. Accurate HMD related calculations of gaze values should be done from state of HMD when eye 
    /// values were recorded. 
    /// </summary>
    /// <param name="numDevices"> Number of tracked devices (starting with index 0 that yoy want to get data for. Note that SteamVR specifies 
    /// indices and they can change on device disconnec/reconnect. HMD is always 0. Base stations are typically 1 and 2. But this can always change</param>
    /// <param name="delayAll"> True delays all values the same amount. False returns current values for all tracked devices except HMD. Set
    /// maxQueue to 1 if you don't want any delayed values.</param>
    /// <returns></returns>
    protected List<SteamVR_Utils.RigidTransform> GetTrackedDeviceValues(int numDevices = 15, bool delayAll = true)
    {
       
        // Get tracked poses from OpenVR. 0 will be hmd all ther tracked devices will be indexed 1-15
        TrackedDevicePose_t[] trackedDevicePose_T = new TrackedDevicePose_t[numDevices];
        // https://github.com/ValveSoftware/openvr/wiki/IVRSystem::GetDeviceToAbsoluteTrackingPose
        OpenVR.System.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseStanding, 0, trackedDevicePose_T); // Get headset position now
                                                                                                                                 //OpenVR.System.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseStanding, HMDToEyetrackerDelay(), trackedDevicePose_T); // Get estimate of headset position framerate times 2 ago
        // use a queue to hold hmd values for 2 frames to reduce effects of latency on eye gaze calculations
        delayedTracking.Enqueue(trackedDevicePose_T);

        // Note that this approach will align all data with eyetracking as collected including all tracked devices if this data is 
        // used for something other than data analysis, more recent data should be used. 
        // once queue is full, start pulling oldest hmd data out for eye gaze calculations
        if (delayedTracking.Count >= maxQueue)
        {
            while (delayedTracking.Count > maxQueue - 1)
            {
                if (delayAll)
                {
                    // use this if you want all data synced with eyetracking for post analysis
                    trackedDevicePose_T = delayedTracking.Dequeue();
                }
                else
                {
                    // use this if you only want HMD synced with eyetracking
                    TrackedDevicePose_t[] tmp = delayedTracking.Dequeue();
                    trackedDevicePose_T[0] = tmp[0];
                }
            }
        }

        List<SteamVR_Utils.RigidTransform> trackedDeviceTransforms = new List<SteamVR_Utils.RigidTransform>();

        for(int i = 0; i < trackedDevicePose_T.Length; i++)
        {
            // Always convert HMD values, only convert others if connnected
            if (i>1 && !trackedDevicePose_T[i].bDeviceIsConnected)
            {
                continue;
            }

            // Conditional can be uncommented to only record devices that are currently connected. Must ensure HMD always gives values. Any other devices 
            // that code depends on must also return values even on disconnect or other safeties must be in place
            //if(trackedDevicePose_T[i].bDeviceIsConnected)
            //{
              trackedDeviceTransforms.Add(TrackedDeviceToUnity(trackedDevicePose_T[i]));
            //}
        }

        return trackedDeviceTransforms;
    }

    /// <summary>
    /// Converts coordinate frame of from OpenVR to Unity coordinate frame. Note that current versions of Unity do this automatically for camera position but
    /// for raw data these calculations have to be handled mannually.
    /// </summary>
    /// <param name="trackedDevice"> the device to be converted to Unity's coordinate frame</param>
    /// <returns></returns>
    SteamVR_Utils.RigidTransform TrackedDeviceToUnity(TrackedDevicePose_t trackedDevice)
    {
        SteamVR_Utils.RigidTransform transformVals = new SteamVR_Utils.RigidTransform(trackedDevice.mDeviceToAbsoluteTracking);

        // Apply camera offset relative to cameraRig postion, rotation, and scale
        Matrix4x4 m = Matrix4x4.TRS(offset_RigPos, offset_RigRot, Vector3.one);
        transformVals.pos = m.MultiplyPoint3x4(transformVals.pos);

        // Apply rotation offset relative to cameraRig rotation
        transformVals.rot = offset_RigRot * transformVals.rot;

        return transformVals;
    }

    /// <summary>
    /// Gets angles in degrees between two vectors given a to vector that is defined in a world frame. 
    /// This is used to calcuate angle of HMD relative to a target. If the target is directly in front of the HMD
    /// i.e. the HMD is oriented towards the target, then the angle of the HMD to target = 0. 
    /// 
    /// If no target is specied the target is placed 1m from the HMD along the +Z world axis. In this case 0 degrees
    /// means that the HMD is oriented perpendicular teh specifed Unity axis and is directed in the positive direction.
    /// </summary>
    /// <param name="to">Vector angle is measured to</param> 
    /// <param name="localFrame">Rotation of head </param> 
    /// <param name="zOffset">Offset used to shift z rotations from 180</param> 
    /// <returns></returns>
    protected Vector3 AngleinDegrees(Vector3 to, Quaternion localFrame, float zOffset = 0)
    { 
        Vector3 angles = Vector3.zero;

        Quaternion relativeAngleQuat = Quaternion.Inverse(localFrame) * Quaternion.LookRotation(to, Vector3.up);
        // Extract angle 
        angles.x = relativeAngleQuat.eulerAngles.x; // angle of head up down relative to gaze target

        relativeAngleQuat = Quaternion.Inverse(localFrame) * Quaternion.LookRotation(to, Vector3.forward);
        angles.y = relativeAngleQuat.eulerAngles.y; // angle of hed left right relative to gaze target
        relativeAngleQuat = Quaternion.Inverse(localFrame) * Quaternion.LookRotation(to, Vector3.right);
        angles.z = relativeAngleQuat.eulerAngles.z - zOffset; // yaw of head relative gaze target

        for (int i = 0; i < 3; i++)
        {
            if (angles[i] > 180)
                angles[i] -= 360;
        }

        return angles;
    }

    /// <summary>
    /// Gets angles in degrees between two vectors in a local frame.
    /// This function returns the angle in degrees such that 0 degrees is perpendicular with  
    /// forward in the to vector's local coordinate frame. 
    /// </summary>
    /// <param name="to"></param> vector angle is measured to
    /// <param name="zOffset"></param> offset used to shift z rotations from 180
    /// <returns></returns>
    protected Vector3 AngleinDegrees(Vector3 to, float zOffset = 0)
    {
        Vector3 angles = Vector3.zero;

        if(to == Vector3.zero)
        {
            return angles * 999;
        }

        Quaternion relativeAngleQuat = Quaternion.LookRotation(to, Vector3.up);
        angles.x = relativeAngleQuat.eulerAngles.x; // angle of head up down relative to gaze target
        relativeAngleQuat = Quaternion.LookRotation(to, Vector3.forward);
        angles.y = relativeAngleQuat.eulerAngles.y; // angle of hed left right relative to gaze target
        relativeAngleQuat = Quaternion.LookRotation(to, Vector3.right);
        angles.z = relativeAngleQuat.eulerAngles.z - zOffset; // yaw of head relative gaze target

        for (int i = 0; i < 3; i++)
        {
            if (angles[i] > 180)
                angles[i] -= 360;
        }

        return angles;
    }

    /// <summary>
    /// calculate a position in the world where the individual is looking. Uses a target position to determing distance
    /// </summary>
    /// <param name="hmdPosition">position of hmd</param> 
    /// <param name="targPos">a target used to calculate distance to gaze point from hmd</param> 
    /// <param name="eyeVec">eye gaze vector from eye tracker</param> 
    /// <returns></returns>
    protected Vector3 GazePoint(Vector3 hmdPosition, Vector3 targPos, Vector3 eyeVec)
    {
        // TODO: Test correction introduced by ViveSR in thier example Gazeray script (so far doesn't seem to make a difference)
        //Vector3 heading = (hmdPosition - (Vector3.up *0.05f)) - targPos;
        Vector3 heading = hmdPosition - targPos;

        float dist = heading.magnitude;
        Vector3 tmp = hmdPosition + eyeVec * dist;

        return tmp;
    }

    /// <summary>
    /// calculate a position in the world where the individual is looking. Distance is specified
    /// </summary>
    /// <param name="hmdPosition"></param> position of hmd
    /// <param name="dist"></param> distance to gaze point from hmd
    /// <param name="eyeVec"></param> eye vector from eye tracker
    /// <returns></returns>
    protected Vector3 GazePoint(Vector3 hmdPosition, float dist, Vector3 eyeVec)
    {
        Vector3 tmp = hmdPosition + eyeVec * dist;

        return tmp;
    }

    /// <summary>
    /// Another solution for getting previous hmd location. This approach doesn't use a queue.
    /// Instead, OpenVR estimates a the location of the hmd given some delay calculated here. 
    /// The delay in this case is -2 * screen frequency minus seconds since last frame sycn
    /// </summary>
    /// <returns></returns>
    protected float HMDToEyetrackerDelay()
    {
        float fSecondsSinceLastVsync = 0;
        ulong unused = 0;
        OpenVR.System.GetTimeSinceLastVsync(ref fSecondsSinceLastVsync, ref unused);

        ETrackedPropertyError error = ETrackedPropertyError.TrackedProp_UnknownProperty;
        float fDisplayFrequency = OpenVR.System.GetFloatTrackedDeviceProperty(OpenVR.k_unTrackedDeviceIndex_Hmd, ETrackedDeviceProperty.Prop_DisplayFrequency_Float, ref error);
        float fFrameDuration = 1f / fDisplayFrequency;
        
        return (fFrameDuration * -2) + fSecondsSinceLastVsync;
    }

    /// <summary>
    /// Used to convert vector3's to float[]
    /// </summary>
    /// <param name="val"></param> vector3 to be converted
    /// <returns></returns>
    protected double[] SetPositionVar(Vector3 val)
    {
        double[] tmp = new double[3] { val.x, val.y, val.z };

        return tmp;
    }

    /// <summary>
    /// Used to convert Quaternion's to float[]
    /// </summary>
    /// <param name="val"></param> Quaternion to be converted
    /// <returns></returns>
    protected double[] SetQuaternionVar(Quaternion val)
    {
        double[] tmp = new double[4] { val.x, val.y, val.z, val.w };

        return tmp;
    }

    protected double[] SetSingleVar(double val)
    {
        double[] tmp = new double[1] { val };

        return tmp;
    }

}
