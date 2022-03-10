using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VRCopyText : MonoBehaviour
{
    public Text accText, copyAccText;
    public Text precText, copyPrecText;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        copyAccText.text = accText.text;
        copyPrecText.text = precText.text;
    }
}
