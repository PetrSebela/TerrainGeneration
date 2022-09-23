using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DebugScript : MonoBehaviour
{
    [Header("Generation")]
    [SerializeField]
    [Range(0, 1)]
    private float _compassScale;

    [SerializeField]
    [Range(0, 1)]
    private float _compassDistance;

    [Header("Performance")]
    [SerializeField]
    private bool _showFPS = true;
    
    [SerializeField]
    private TextMeshProUGUI _fpsDisplay;

    private float deltaTime;
    void LateUpdate()
    {
        Vector3 center = this.transform.position + (this.transform.forward * _compassDistance);
        Debug.DrawLine(center, center + new Vector3(1 * _compassScale, 0, 0), Color.red);
        Debug.DrawLine(center, center + new Vector3(0, 1 * _compassScale, 0), Color.green);
        Debug.DrawLine(center, center + new Vector3(0, 0, 1 * _compassScale), Color.blue);


        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        _fpsDisplay.text = Mathf.Ceil (fps).ToString ();
    }
}
