using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for objects that are the target of user focus
/// </summary>
public class FocusCubeBehavior : MonoBehaviour
{
    public Transform head;

    public bool stabalizePos = false;
    Vector3 hmd2TargetOffset = Vector3.one;

    //public Vector3 baseScale = new Vector3(0.01f, 0.001f, 0.00001f);
    [Tooltip("Set Size of focus object based on horizontal and vertical visual angle that covers object (in degrees)")]
    public Vector2 visualAngle = new Vector2(2, 2);

    public float depth = 0.001f;

    bool visible = false;

    // Update is called once per frame
    protected virtual void Update()
    {
        if (visible && stabalizePos)
        {
            StabalizePosition();
        }
    }

    /// <summary>
    /// Stablizes postion relative to HMD. Keeps object directly in front of participant at a fixed distance regardless
    /// of participant movement
    /// </summary>
    protected void StabalizePosition()
    {
        transform.position = head.position + hmd2TargetOffset;
        transform.LookAt(head);
    }

    public void IntializePosition()
    {
        visible = true;
        transform.position = head.position + (head.forward * 1f);
        transform.LookAt(head);
        hmd2TargetOffset = transform.position - head.position;
    }

    public void SetHMDRelitivePosition(Vector3 offset)
    {
        visible = true;
        transform.position = head.TransformPoint(offset);
        transform.LookAt(head);
        SetScale(offset.z);
        hmd2TargetOffset = transform.position - head.position;
    }

    public void SetPosition(Transform t)
    {
        SetPosition(t.position, t.rotation);
    }

    public void SetPosition(Vector3 pos, bool setScale = false)
    {
        SetPosition(pos, Quaternion.identity);

        if (setScale)
            SetScale(Vector3.Distance(head.position, pos));
    }

    public void SetPosition(Vector3 pos, Quaternion rot)
    {
        transform.position = pos;
        transform.rotation = rot;
    }

    public void SetScale(float dist)
    {
        float degConv = Mathf.Deg2Rad;

        float horVal = dist * Mathf.Tan(degConv * visualAngle.x);
        float vertVal = dist * Mathf.Tan(degConv * visualAngle.y);

        Vector3 tmp = new Vector3(horVal, vertVal, depth);

        transform.localScale = tmp;
    }

    public void SetScale()
    {
        float dist = Vector3.Distance(head.position, transform.position);

        SetScale(dist);
    }

    public void RemoveFocus()
    {
        visible = false;
        transform.position = Vector3.one * 1000;
        hmd2TargetOffset = transform.position;
    }
}
