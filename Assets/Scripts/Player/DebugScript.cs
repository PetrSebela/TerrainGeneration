using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DebugScript : MonoBehaviour
{
    [Header("General setup")]
    [SerializeField] private KeyCode ToggleDebug;
    
    [Header("Component references")]
    [SerializeField] private ChunkManager ChunkManager;
    [SerializeField] private GameObject DebugContainer;
    [SerializeField] private TextMeshProUGUI FPSComponent;
    [SerializeField] private TextMeshProUGUI PlayerPosition;
    [SerializeField] private TextMeshProUGUI ControllerType;

    private float deltaTime;

    private bool DebugActive;
    
    void Start(){
        DebugActive = DebugContainer.activeSelf;
    }
    void LateUpdate()
    {
        if(Input.GetKeyDown(ToggleDebug)){
            DebugActive = !DebugActive;
            DebugContainer.SetActive(DebugActive);
        }

        // if(!DebugActive)
        //     return;


        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        FPSComponent.text = string.Format("FPS : {0}", Mathf.Ceil(fps).ToString());
        
        Vector3 position = ChunkManager.TrackedObject.position;
        PlayerPosition.text = string.Format("Position [x,y,z] : {0}",new Vector3Int((int)position.x, (int)position.y, (int)position.z).ToString());
        
        ControllerType.text = string.Format("Current controller type : {0}", ChunkManager.PlayerController.ControllerType.ToString());
    }
}
