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
        // Pokud nemáme cíl, zkusíme ho najít podle Tagu
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }
}