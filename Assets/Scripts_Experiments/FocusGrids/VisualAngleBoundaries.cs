using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class is used to convert visual angles into position boundaries relative to the HMD
/// min and max values indicate the min and max values for the specified dimension relative to the HMD 
/// given a specfied set of visual angles and a distance
/// 
/// Note: Currently assumes symetrical boundary definitions. Could be defined to accomodate asymetrical boundaries. 
/// </summary>
public class VisualAngleBoundaries 
{
    // Class constructors
    public VisualAngleBoundaries() { }

    /// <summary>
    /// Initializs Visual boundaries object. Input angles specify the total visual angle
    /// </summary>
    /// <param name="thetaX">Horizontal limits</param>
    /// <param name="thetaY">vertical limits</param>
    /// <param name="dist">Distance</param>
    /// <param name="thetaInRads">Specifies if angles are in radians</param>
    public VisualAngleBoundaries(float thetaX, float thetaY, float dist, bool thetaInRads = false)
    {
        VisualAngleBounds(thetaX, thetaY, dist, thetaInRads);
    }

    /// <summary>
    /// Initializs Visual boundaries object. Input angles specify the total visual angle
    /// </summary>
    /// <param name="limits"> vector3 limits x = horizontal limits, y = vertical limits and z = distance</param>
    /// <param name="thetaInRads"> Specifies if angles are in radians</param>
    public VisualAngleBoundaries(Vector3 limits, bool thetaInRads = false)
    {
        VisualAngleBounds(limits, thetaInRads);
    }

    // Varibles for boundary definition on each dimension
    public float xMin { get; /*private*/ set; } = 0;
    public float xMax { get; /*private*/ set; } = 0;

    public float yMin { get; /*private*/ set; } = 0;
    public float yMax { get; /*private*/ set; } = 0;

    // distance from viewer
    public float distance { get; /*private*/ set; } = 0;

    // Boundary definitions used to initialize class
    public Vector3 boundaryDefs { get; /*private*/ set; } = Vector3.one;

    // allows user to sepcify that boundaries are defined in thetas
    public bool thetaInRadians { get; /*private*/ set; } = false;

    /// <summary>
    /// Returns location of object boundaries for an object which takes up visual angle specified by thetaDeg 
    /// at a distance from the view specified by dist assuming object is centered on viewer. these values can be offset
    /// if not centered on viewer. Vertical and horzontal angles can be used to get 2D boundaries of 2D object 
    /// within the visual angle. Call this function for each dimension. 
    /// 
    /// Used to specify location of stimulus in terms of visual angle. 
    /// 
    /// returned values are specified in viewer local space. Convert to world space using transform.TransformPoint(vector3)
    /// or make object child ov viewer. 
    /// </summary>
    /// <param name="theta">total visual angle</param> 
    /// <param name="dist">distance from view. </param> 
    /// <returns></returns>
    public float[] VisualAngleBounds(float theta, float dist, bool rads = false)
    {
        float degConv = Mathf.Deg2Rad;

        if (rads)
            degConv = 1;

        float bound = dist * Mathf.Tan(0.5f * degConv * theta);

        return new float[] { bound, -bound };
    }

    // Public function to generate a visual boundaries object. 
    public VisualAngleBoundaries VisualAngleBounds(float thetaX, float thetaY, float dist, bool thetaInRads = false)
    {
        float[] boundX = VisualAngleBounds(thetaX, dist, thetaInRads);
        float[] boundY = VisualAngleBounds(thetaY, dist, thetaInRads);

        xMax = boundX[0];
        xMin = boundX[1];

        yMax = boundY[0];
        yMin = boundY[1];

        distance = dist;

        boundaryDefs = new Vector3(thetaX, thetaY, dist);
        thetaInRadians = thetaInRads;

        return this;
    }

    // Public function to generate a visual boundaries object. 
    public VisualAngleBoundaries VisualAngleBounds(Vector3 limits, bool thetaInRads = false)
    {
        return VisualAngleBounds(limits.x, limits.y, limits.z, thetaInRads);
    }

}
