using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
/*
This code was built to provide an example of how to impliment eyetracking using the HTC Vive Pro Eye and
the SRanipal SDK for scientific resaerch contexts. If you use this or any related code for published scientific 
research, please cite """""" and provide a link to the github repository https://github.com/Pretendprof. If you use this
software in a commercial product, along with attribution, please consider supporting the developer or the developer's resaerch
financially. 
This software is provided as an example. No gaurantee of ongoing support is given. Please read and understand the 
code before utilizing in a critical system. 

Maurice Lamb
University of Skövde
email: maurice.lamb at his dot se
*/

/// <summary>
/// Its a data recorder. Data is stored in a csv format. 
/// 
/// Data is recorded as it is processed so no seperate record thread is run. As a result the DataRecorder runs
/// at the data rate which is set by ViveEyeDevice. 
/// 
/// Data can be writen as it is produced or it can be written when StopRecording is called. If multiple data recorders 
/// are instanced and multiple data streams are recorded at once, delay record will avoid bottlenecks or issues
/// due to all the streams running at once. Otherwise, saving data as it comes reduces the risk of lost data.
/// </summary>
public class DataRecorder 
{
    StreamWriter fs; // steamer for writing to a file

    List<string> header = new List<string>();

    public bool isRecording = false;

    bool isWriting = false;

    // specify if data should be written with a delay (when StopRecord is called) or not
    public bool delayWrite = false;
    // list for delay data 
    List<string> delayedData = new List<string>();
    string fullFilename = "";

    bool fileInitialized = false;

    /// <summary>
    /// Setup a new file for recording
    /// </summary>
    /// <param name="h"> Keys specify header string and values indicate number of columns needed (e.g. a vector 3 for HMDPos would be <"HMDPos", 3>)</param>
    /// <param name="startImmeadeatly">Specifies if file recording should start now or if only initializing file</param>
    /// <param name="fileID">A string value appended to the file name</param>
    public void SetupNewDataFile(Dictionary<string, int> h, bool startImmeadeatly = false, string fileID = "999")
    {
        // stop any previous recording
        StopRecording();

        // setup headers
        SetHeader(h);

        // generate a new file
        fileInitialized = CreateFile(fileID);

        // start recording if true.
        if (startImmeadeatly)
            StartRecording();
    }

    public void SetupNewDataFile(Dictionary<string, int> h, string fileID = "999")
    {
        SetupNewDataFile(h, false, fileID);
    }

    /// <summary>
    /// Setup header of output file
    /// </summary>
    /// <param name="h">Keys specify header string and values indicate number of columns needed (e.g. a vector 3 for HMDPos would be <"HMDPos", 3>)</param>
    void SetHeader(Dictionary<string, int> h)
    {
        header.Clear();

        header.Add("S0100_RecTime"); // add header for keeping track of system time at record

        foreach(string s in h.Keys)
        {
            string id = "";

            if (h[s] == 1)
            {
                // single values. Header starts with S01
                id = "S" + h[s].ToString("D2"); 
            }
            else
            {
                // multiple values (vectors and arrays). Header starts with V## where ## = total number of columns for this header
                id = "V" + h[s].ToString("D2"); 
            }

            // generate unique header for each column (add two digits ## to id indicating which column this is.
            for (int i = 0; i < h[s]; i++)
            {
                string tmp = id + i.ToString("D2") + "_";
                // append string header value from key
                header.Add(tmp + s);
            }
        }
    }

    /// <summary>
    /// Creates a file and initializes filestream
    /// </summary>
    /// <param name="fileID">A tag to identify the file by. Appended to a unique file name</param>
    /// <returns></returns>
    bool CreateFile(string fileID = "999")
    {
        // record in persistantDataPath (location depends on OS). 
        string folder = Application.persistentDataPath + "/EyeRecordingData";
        Directory.CreateDirectory(folder);

        // Uncomment if not sure where persistant data path is and/or Unity Docs are unclear.
        //UnityEngine.Debug.Log(folder);

        // Check for other files with same fileID
        string[] files = Directory.GetFiles(folder, fileID + "*.csv");
        int itt = files.Length;

        // generate a unique ID for the file name using supplied file ID, filecount itt, and unique(ish) timestamp
        DateTime now = DateTime.Now;
        string[] tmpID = new string[] { fileID, "_", itt.ToString("D3"), "_", now.Minute.ToString("D2"), now.Second.ToString("D2"), now.Millisecond.ToString("D3") };
        string tmpFileName = "/" + String.Join("", tmpID) + ".csv";

        fullFilename = folder + tmpFileName;

        string tmpData = string.Join(",", header.ToArray());

        // if delayWrite Add header to data for delay output and return
        if (delayWrite)
        {
            delayedData.Add(tmpData);
            return true;
        }

        // Create file with filestream and add headder. 
        fs = new StreamWriter(fullFilename, true);

        fs.WriteLine(tmpData);

        fs.Flush(); // clear memory buffer      

        return true;

    }

    /// <summary>
    /// Start recording. File must be inialized to record
    /// </summary>
    /// <returns></returns>
    public bool StartRecording()
    {
        if (!fileInitialized)
            return false;

        isRecording = true;

        return isRecording;
    }

    /// <summary>
    /// Pauses recording without disposing of fs
    /// </summary>
    public void PauseRecording()
    {
        isRecording = false;
    }

    /// <summary>
    /// Stops Recording and disposes of fs
    /// </summary>
    public void StopRecording()
    {
        isRecording = false;

        // writes file if delayWrite is true
        if(delayWrite && delayedData.Count > 1)
        {
            WriteDelayData();
        }

        // if there is fs is setup, then writes data
        if (fs != null && fs.BaseStream != null)
        {
            while(isWriting)
            {
                // Avoids fs from being disposed during a write cycle
            }

            fs.Dispose();

            fileInitialized = false;
        }
    }

    /// <summary>
    /// data to be recorded is alligned with existing headers and converted to strings in a string[]
    /// If delaywrite is true, string is added to delayWrite list
    /// If delaywrite is false, string is written to fs.
    /// </summary>
    /// <param name="values">data to be recorded (string key must match header, double[] length must match number of columns</param>
    /// <returns></returns>
    public bool RecordData(Dictionary<string, double[]> values)
    {
        // if start recording hasn't been called then nothing happens
        if (!isRecording)
            return false;

        string[] tmpData = new string[header.Count];


        foreach(string s in values.Keys)
        {
            //Find the first column with the header matching s (from the recorded data keys)
            int val = header.FindIndex(x => x.Substring(6) == s);

            // No matching header found
            if (val == -1)
                continue;

            // get data for each column
            for(int i = 0; i < values[s].Length; i++)
            {
                tmpData[val + i] = values[s][i].ToString();
            }
        }

        // timestamp to check delay from eyedata to record. divide by 10,000 to get value in miliseconds
        tmpData[0] = Stopwatch.GetTimestamp().ToString();// / (Stopwatch.Frequency / 1000)).ToString();

        // write the data (or add to delayData list)
        WriteData(tmpData);

        return true;
    }

    /// <summary>
    /// Writes parsed string[] data from RecordData(). Concatenates as a single comma seperated string for a single output row
    /// </summary>
    /// <param name="curFrameData">strings to be written to a file</param>
    void WriteData(string[] curFrameData)
    {
        string tmpData = string.Join(",", curFrameData);

        if (!isRecording)
            return;

        if (delayWrite)
        {
            delayedData.Add(tmpData);
            return;
        }

        isWriting = true;

        fs.WriteLine(tmpData);

        fs.Flush(); // clear memory buffer            

        isWriting = false;
    }

    /// <summary>
    /// For delay write. Writes all data with some lag. Currently called when StopRecord() is called
    /// </summary>
    /// <returns></returns>
    bool WriteDelayData()
    {
        fs = new StreamWriter(fullFilename, true);

        for (int i = 0; i< delayedData.Count; i++)
        {
            fs.WriteLine(delayedData[i]);

            fs.Flush();
        }

        fs.Dispose();

        delayedData.Clear();

        return true;
    }

    /// <summary>
    /// Should be called on application quite to clean up fs.
    /// NOTE: DataRecorder does not inherit from MonoBehavior so this should be called Whereever a DataRecorder is instanced
    /// </summary>
    public void OnQuit()
    {
        if (fs != null && fs.BaseStream != null)
        {
            fs.Dispose();
        }

        delayedData.Clear();
    }

    /// <summary>
    /// I don't trust Unity's shutdown process to fully respect the finalizer. But here it is just in case.
    /// </summary>
    ~DataRecorder()
    {
        OnQuit();
    }
}
