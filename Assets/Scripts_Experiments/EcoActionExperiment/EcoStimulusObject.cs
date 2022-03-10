
using UnityEngine;

/// <summary>
/// Focus object used in ecoAction task
/// </summary>
public class EcoStimulusObject : FocusCubeBehavior
{
    Renderer myRend;
    Color iniColor;
    Vector3 iniPosition;

    private void Start()
    {
        myRend = GetComponent<Renderer>();
        iniColor = myRend.material.color;
        iniPosition = transform.position;
    }

    public void MoveObject(float speed, Vector3 dir)
    {
        transform.Translate(dir * speed * Time.deltaTime, Space.World);
    }

    public float CalculateDistance(Vector3 dim)
    {
        // get position only on relevant measurment dimensions
        Vector3 tmp = Vector3.Scale(dim, transform.position);
        Vector3 tmpHead = Vector3.Scale(dim, head.position);

        float distance = Vector3.Distance(tmp, tmpHead);

        return distance;
    }

    /// <summary>
    /// Reset object to initial state. 
    /// </summary>
    public void ResetToBegining()
    {
        transform.position = iniPosition;
        myRend.material.color = iniColor;
        myRend.material.SetColor("_EmissionColor", iniColor);
    }

    /// <summary>
    /// Change color of object to indicate phase completion
    /// </summary>
    /// <param name="col"></param>
    public void SignalTaskPhaseComplete(Color col)
    {
        myRend.material.color = col;
        myRend.material.SetColor("_EmissionColor", col);
    }
}
