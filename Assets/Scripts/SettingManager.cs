using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingManager : MonoBehaviour
{
    private Dictionary<string,Vector2Int> resolutionMap = new Dictionary<string, Vector2Int>(){
        {"2560x1440",new Vector2Int(2560,1440)},
        {"1920x1080",new Vector2Int(1920,1080)},
        {"1366x768",new Vector2Int(1366,768)},
        {"1280x720",new Vector2Int(1280,720)},
    };

    void Start()
    {
        DontDestroyOnLoad(this);   
    }

    // public void ChangeResolution(){
        // Debug.Log("Resolution change");
        // Vector2Int res = resolutionMap[resolutionDropdown.options[resolutionDropdown.value].text];
        // Screen.SetResolution(res.x, res.y, FullScreenMode.FullScreenWindow);
    // }
}
