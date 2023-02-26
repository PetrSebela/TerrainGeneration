using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
public class ButtonHighlightFix : MonoBehaviour
{
    void Update()
    {
        if ((Input.GetAxis("Mouse X") != 0) || (Input.GetAxis("Mouse Y") != 0)) {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}
