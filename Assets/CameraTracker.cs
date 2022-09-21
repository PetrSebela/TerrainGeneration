using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraTracker : MonoBehaviour
{
    [SerializeField]
    private Transform _camPosition;

    void Update()
    {
        transform.position = _camPosition.position;
    }
}
