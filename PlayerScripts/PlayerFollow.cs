using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    // Target to follow (our Player)
    public Transform target;

    // Offset ensures the camera stays centered but "above" the scene (Z axis)
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    // LateUpdate is called after all movement is calculated
    // This prevents camera stuttering
    void LateUpdate()
    {
        if (target != null)
        {
            // Set camera position to target position + offset
            transform.position = target.position + offset;
        }
    }
}