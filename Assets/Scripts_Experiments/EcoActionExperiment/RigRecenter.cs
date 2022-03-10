using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Recenters camera rig so that camera is centered on a specifed location 
/// </summary>
public class RigRecenter : MonoBehaviour
{
    // Relvant transforms
    public Transform cam, rig, center;
    public float headHeight = 0;

    void Update()
    {

    }

    /// <summary>
    /// Starts recenter process. Camera rig is moved so that camera is centered on specified transform location 
    /// </summary>
    /// <param name="useActualHeight">Use the camera's actual height</param>
    public void ActivateRecenter(bool useActualHeight = false)
    {
        PositionCameraRig(useActualHeight);

        FaceForward();
    }

    /// <summary>
    /// To move center to a new location when recentering
    /// </summary>
    /// <param name="cent">Where the camera will be located/oriented</param>
    /// <param name="useActualHeight">Use the camera's actual height</param>
    public void ActivateRecenter(Transform cent, bool useActualHeight = false)
    {
        center = cent;

        ActivateRecenter(useActualHeight);
    }

    /// <summary>
    /// To move center to a new location when recentering, and change height
    /// </summary>
    /// <param name="cent">Where the camera will be located/oriented</param>
    /// <param name="height">A specified height in meters to place initial head position</param>
    /// <param name="useActualHeight">Use the camera's actual height</param>
    public void ActivateRecenter(Transform cent, float height, bool useActualHeight)
    {
        center = cent;
        headHeight = height;

        ActivateRecenter(useActualHeight);
    }

    /// <summary>
    /// Sets camera rig location to match specified center. 
    /// </summary>
    /// <param name="useActualHeight">Use the camera's actual height</param>
    void PositionCameraRig(bool useActualHeight = false)
    {
        Vector3 camOffset = rig.position - cam.position; 

        Vector3 tmp = center.position + camOffset;

        if(useActualHeight)
        {
            tmp.y = 0; 
        }
        else
        {
            tmp.y += headHeight;
        }

        rig.position = tmp;
    }

    /// <summary>
    /// Changes camera orientation to match center orientation. 
    /// </summary>
    void FaceForward()
    {
        GameObject pivot = new GameObject();
        pivot.transform.position = cam.position; // place pivot at camera position
        pivot.transform.eulerAngles = new Vector3(0, cam.eulerAngles.y, 0);

        rig.parent = pivot.transform; // make the pivot the parent of camera rig
        pivot.transform.parent = center;

        pivot.transform.eulerAngles = new Vector3(0, center.eulerAngles.y, 0);

        rig.parent = null;
        pivot.transform.parent = null;
        Destroy(pivot);
    }
}
