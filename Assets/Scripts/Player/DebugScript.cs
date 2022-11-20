using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DebugScript : MonoBehaviour
{
    [Header("Generation")]
    [SerializeField][Range(0, 1)] private float _compassScale;
    [SerializeField][Range(0, 1)] private float _compassDistance;

    [SerializeField] private TextMeshProUGUI _distanceDisplay;

    [SerializeField] private TextMeshProUGUI _velocityDisplay;
    [SerializeField] private Rigidbody _rb;

    [SerializeField] private TextMeshProUGUI _chunkPositionDisplay;
    [SerializeField] private TextMeshProUGUI _positionDisplay;



    [Header("Performance")]
    [SerializeField] private bool _showFPS = true;
    [SerializeField] private TextMeshProUGUI _fpsDisplay;

    private float deltaTime;
    void LateUpdate()
    {
        Vector3 center = this.transform.position + (this.transform.forward * _compassDistance);
        Debug.DrawLine(center, center + new Vector3(1 * _compassScale, 0, 0), Color.red);
        Debug.DrawLine(center, center + new Vector3(0, 1 * _compassScale, 0), Color.green);
        Debug.DrawLine(center, center + new Vector3(0, 0, 1 * _compassScale), Color.blue);

        if (_showFPS)
        {
            deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
            float fps = 1.0f / deltaTime;
            _fpsDisplay.text = "FPS : " + Mathf.Ceil(fps).ToString();
        }


        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, Mathf.Infinity, ~LayerMask.NameToLayer("Ground")))
        {
            _distanceDisplay.text = "Distance : " + hit.distance.ToString();
            _chunkPositionDisplay.text = "Chunk : " + hit.transform.name;
        }
        else
        {
            _distanceDisplay.text = "Distance : 0";
            _chunkPositionDisplay.text = "Chunk : ";
        }

        _velocityDisplay.text = "Speed : " + Mathf.Round(_rb.velocity.magnitude).ToString();
        Vector3 position = transform.position;
        _positionDisplay.text = "Position : " + new Vector3Int((int)position.x, (int)position.y, (int)position.z).ToString();
    }
}
