using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TaskController : MonoBehaviour
{
    public ViveEyeController viveControls;

    public Transform cam;

    public Text userInstructions;

    public string[] instructions;

    public InitializerBehaviour initializerObj;

    public FocusCubeBehavior focusObj;

    public float taskTime = 10;

    public string fileID = "Experiment";

    protected Vector3 initializerPos = new Vector3(0, -1, 15);
    protected Vector3 instructionPos = new Vector3(0, 100, 700);

    protected void SetUserInstructions(int idx, bool reposition = true)
    {
        SetUserInstructions(instructions[idx], reposition);
    }

    protected void SetUserInstructions(string s, bool reposition = true)
    {
        if (reposition)
        {
            userInstructions.canvas.transform.position = cam.TransformPoint(instructionPos);// + (camera.forward  * 700);
            userInstructions.canvas.transform.rotation = cam.rotation;
        }

        userInstructions.text = s;
    }

    protected void SetInitializer()
    {
        initializerObj.SetHMDRelitivePosition(initializerPos);
        initializerObj.SetupInitializer();
    }

    protected void SetInitializer(Vector3 iniPos)
    {
        initializerObj.SetHMDRelitivePosition(iniPos);
        initializerObj.SetupInitializer();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

}
