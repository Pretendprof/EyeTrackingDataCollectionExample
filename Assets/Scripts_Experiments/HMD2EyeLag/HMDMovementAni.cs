using System.Collections;
using UnityEngine;

/// <summary>
/// Quick scripted solution to visualizing HMD movement instructions
/// </summary>
public class HMDMovementAni : MonoBehaviour
{
    bool running = true;

    public void BeginRotation(Vector3 rotationVector, float frequency, float maxAngle, float offset = 0)
    {
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        running = true;

        StartCoroutine(RunRotation(rotationVector, frequency, maxAngle, offset));
    }

    public void EndRotation()
    {
        gameObject.SetActive(false);

        running = false;
        StopAllCoroutines();
    }

    IEnumerator RunRotation(Vector3 rotationVector, float frequency, float maxAngle, float offset)
    {
        while (running)
        {
            float angle = Mathf.Sin(Time.time * frequency) * maxAngle;

            transform.rotation = Quaternion.AngleAxis(angle + offset, rotationVector);

            yield return null;
        }
    }

    private void OnDisable()
    {
        EndRotation();
    }


}
