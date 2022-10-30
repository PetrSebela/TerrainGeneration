using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamRoitate : MonoBehaviour
{

    [SerializeField] private float _rotationSpeed;
    void LateUpdate()
    {
        this.transform.Rotate(0, _rotationSpeed * Time.deltaTime, 0);
    }
}
