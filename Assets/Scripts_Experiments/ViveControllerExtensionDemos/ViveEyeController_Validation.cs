using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.UI;
using ViveSR.anipal.Eye;


/// <summary>
/// Includes code to calculate windowed precision and accuracy along with blink and vaility detection.
/// </summary>

public class ViveEyeController_Validation : ViveEyeController
{
    bool srIsLoaded = false;

    public Text rmsText;
    public Text accuracyText;
    public Image validityImage;
    public Slider openSlide;
    // public Text skewText;

    ConcurrentQueue<double[]> mainRmsQueue = new ConcurrentQueue<double[]>();
    ConcurrentQueue<double[]> mainAccQueue = new ConcurrentQueue<double[]>();
    ConcurrentQueue<int> mainValidityQueue = new ConcurrentQueue<int>();
    ConcurrentQueue<double> mainOpenQueue = new ConcurrentQueue<double>();

    int maxWindowSize = 30;

    // Values for RMS precision calculations
    Queue<double> vertDiffQueue = new Queue<double>();
    Queue<double> horDiffQueue = new Queue<double>();

    double rootSqVertDiffRunningSumRMS = 0;
    double recentVertValRMS = 0;

    double rootSqHorDiffRunningSumRMS = 0;
    double recentHorValRMS = 0;

    // Values for Accuracy calculations
    Queue<double> vertAccWindowQueue = new Queue<double>();
    Queue<double> horAccWindowQueue = new Queue<double>();

    double vertRunningSumAcc = 0;
    double horRunningSumAcc = 0;

    double[] acc = new double[] { 0, 0 };
    double[] rms = new double[] { 0, 0 };
    int valid = 0;
    double open = 0;

    protected override void OnEnable()
    {
        StartCoroutine(DelayedOnEnable());
    }

    // waits for ExperimentController to be loaded before running base OnEnable
    IEnumerator DelayedOnEnable()
    {
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
                srIsLoaded = true;

                Application.Quit();
            }

            yield return null;
        }

        while (viveEye == null)
        {
            viveEye = ExperimentController.Instance.viveEye;
            yield return null;
        }

        base.OnEnable();

        //recorder.delayWrite = true;
    }

    // doesn't shut down ViveEyeDevice in ExperimentController instance so that it can be used for next scene. 
    protected override void OnDisable()
    {
        viveEye.ViveDataPushAction -= RecieveData;

        recorder.OnQuit();
    }

    protected override void Update()
    {
        CheckQueues();

        SetCheckInfo();

        base.Update();
    }

    void CheckQueues()
    {
        while (mainAccQueue.Count > 0)
        {
            mainAccQueue.TryDequeue(out acc);
        }

        while (mainRmsQueue.Count > 0)
        {
            mainRmsQueue.TryDequeue(out rms);
        }

        while (mainValidityQueue.Count > 0)
        {
            mainValidityQueue.TryDequeue(out valid);
        }

        while (mainOpenQueue.Count > 0)
        {
            mainOpenQueue.TryDequeue(out open);
        }
    }

    void SetCheckInfo()
    {
        if (Mathf.Abs((float)acc[0]) > 3f || Mathf.Abs((float)acc[1]) > 3f)
        {
            accuracyText.color = Color.red;
        }
        else
        {
            accuracyText.color = Color.green;
        }

        if (Mathf.Abs((float)rms[0]) > 0.35f || Mathf.Abs((float)rms[1]) > 0.35 || rms[0] == 0f)
        {
            rmsText.color = Color.red;
        }
        else
        {
            rmsText.color = Color.green;
        }

        if (valid < 1)
        {
            validityImage.color = Color.red;
        }
        else
        {
            validityImage.color = Color.green;
        }

        openSlide.value = (float)open;

        accuracyText.text = "Hor: " + acc[0].ToString("0.00") + "  Vert: " + acc[1].ToString("0.00");
        rmsText.text = "Hor: " + rms[0].ToString("0.000") + "   Vert: " + rms[1].ToString("0.000");
    }


    protected override void OnRecieveData(Dictionary<string, double[]> val)
    {
        double horEyeAng = val["Combine_HMD2EyeAng"][1];
        double vertEyeAng = val["Combine_HMD2EyeAng"][0];

        double[] curRMS = CalcuateRMS(horEyeAng, vertEyeAng);

        mainRmsQueue.Enqueue(curRMS);

        double horAccAng = horEyeAng + val["Combine_HMD2TargAng"][1]; // val["Combine_HMD2TargAng"][1];// 
        double vertAccAng = vertEyeAng + val["Combine_HMD2TargAng"][0]; //val["Combine_HMD2TargAng"][0];//

        double[] curAccuracy = CalculateAccuracy(horAccAng, vertAccAng);

        mainAccQueue.Enqueue(curAccuracy);

        int tmpValid = (int)val["Combine_Validity"][0];

        mainValidityQueue.Enqueue(tmpValid);

        double tmpOpen = (val["Right_Openness"][0] + val["Left_Openness"][0]) / 2;

        mainOpenQueue.Enqueue(tmpOpen);
    }

    /// <summary>
    /// Calulates average realtime RMS using a moving window specified by maxWindowSize. 
    /// RMS specified in degree angles relative to HMD
    /// </summary>
    /// <param name="horVal">Horizontal angle of eyes in head</param>
    /// <param name="vertVal">Vertical angle of eyes in head</param>
    /// <returns></returns>
    double[] CalcuateRMS(double horVal, double vertVal)
    {
        double[] rms = new double[] { 0, 0 };

        #region Horizontal
        // Get horical eye angle for this frame
        double horTmp = horVal;
        // caluate difference between current frame and last frame squared
        double sqHorDiff = Math.Pow((horTmp - recentHorValRMS), 2);
        // square root of squared distance
        double rootHorDiff = Math.Sqrt(sqHorDiff);

        // addroot value to diffQueue
        horDiffQueue.Enqueue(rootHorDiff);

        // Add root difference to a running sum
        rootSqHorDiffRunningSumRMS += rootHorDiff;

        // FIFO means that once queue is max size oldest value should be removed so window size remains constant
        while (horDiffQueue.Count > maxWindowSize)
        {
            // if oldest value is removed from queue then subtract value from running sum
            rootSqHorDiffRunningSumRMS -= horDiffQueue.Dequeue();
        }

        // calculate mean for root square diff using current queue size
        rms[0] = rootSqHorDiffRunningSumRMS / horDiffQueue.Count;

        // recent hor becomse current
        recentHorValRMS = horTmp;
        #endregion
        #region Vertical
        // Get vertical eye angle for this frame
        double vertTmp = vertVal;
        // caluate difference between current frame and last frame squared
        double sqVertDiff = Math.Pow((vertTmp - recentVertValRMS), 2);
        // square root of squared distance
        double rootVertDiff = Math.Sqrt(sqVertDiff);

        // addroot value to diffQueue
        vertDiffQueue.Enqueue(rootVertDiff);

        // Add root difference to a running sum
        rootSqVertDiffRunningSumRMS += rootVertDiff;

        // FIFO means that once queue is max size oldest value should be removed so window size remains constant
        while (vertDiffQueue.Count > maxWindowSize)
        {
            // if oldest value is removed from queue then subtract value from running sum
            rootSqVertDiffRunningSumRMS -= vertDiffQueue.Dequeue();
        }

        // calculate mean for root square diff using current queue size
        rms[1] = rootSqVertDiffRunningSumRMS / vertDiffQueue.Count;

        // recent vert becomse current
        recentVertValRMS = vertTmp;
        #endregion

        return rms;
    }

    /// <summary>
    /// Calulates realtime average Accuracy using a moving window specified by maxWindowSize. 
    /// Accuracy specifed in degree angles of HMD plus eye angles relative to gaze target. 
    /// 0 = perfectly accurate.
    /// </summary>
    /// <param name="horVal">Horizontal angle of hmd + eye angles relative to target</param>
    /// <param name="vertVal">Vertical  angle of hmd + eye angles relative to target</param>
    /// <returns></returns>
    double[] CalculateAccuracy(double horVal, double vertVal)
    {
        double[] acc = new double[] { 0, 0 };

        // add new value to running sum
        horRunningSumAcc += horVal;
        // add new value to window queue
        horAccWindowQueue.Enqueue(horVal);

        // when window queue too big remove oldest value
        while (horAccWindowQueue.Count > maxWindowSize)
        {
            // FIFO removes oldest value in queue
            horRunningSumAcc -= horAccWindowQueue.Dequeue();
        }

        // calulate average accuracy
        acc[0] = horRunningSumAcc / horAccWindowQueue.Count;

        // add new value to running sum
        vertRunningSumAcc += vertVal;
        // add new value to window queue
        vertAccWindowQueue.Enqueue(vertVal);

        // when window queue too big remove oldest value
        while (vertAccWindowQueue.Count > maxWindowSize)
        {
            // FIFO removes oldest value in queue
            vertRunningSumAcc -= vertAccWindowQueue.Dequeue();
        }

        // calulate average accuracy
        acc[1] = vertRunningSumAcc / vertAccWindowQueue.Count;

        return acc;
    }

}
