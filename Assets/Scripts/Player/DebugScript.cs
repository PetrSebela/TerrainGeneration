using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugScript : MonoBehaviour
{
    [SerializeField]
    [Range(0, 1)]
    private float _compassScale;

    [SerializeField]
    [Range(0, 1)]
    private float _compassDistance;

    void LateUpdate()
    {
        Vector3 center = this.transform.position + (this.transform.forward * _compassDistance);
        Debug.DrawLine(center, center + new Vector3(1 * _compassScale, 0, 0), Color.red);
        Debug.DrawLine(center, center + new Vector3(0, 1 * _compassScale, 0), Color.green);
        Debug.DrawLine(center, center + new Vector3(0, 0, 1 * _compassScale), Color.blue);
    }
}
