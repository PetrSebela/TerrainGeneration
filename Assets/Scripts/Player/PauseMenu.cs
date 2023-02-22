using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
public class PauseMenu : MonoBehaviour
{
    public TMP_Dropdown resolutionDropdown;
    private Dictionary<string,Vector2Int> resolutionMap = new Dictionary<string, Vector2Int>(){
        {"2560x1440",new Vector2Int(2560,1440)},
        {"1920x1080",new Vector2Int(1920,1080)},
        {"1366x768",new Vector2Int(1366,768)},
        {"1280x720",new Vector2Int(1280,720)},
    };

    public SimulationSettings simulationSettings;
    public ChunkManager chunkManager;
    public TMP_Text SeedDisplay;
    public TMP_Text FOVdisplay;
    public Camera cam;

    public void Start(){
        SeedDisplay.text = "Currently on seed : " + chunkManager.SeedGenerator.seed;  
        resolutionDropdown.value = resolutionDropdown.options.FindIndex(option => option.text == Screen.width + "x" + Screen.height);
    }

    public void QuitSimulation(){
        Debug.Log("Quitting");
        Application.Quit();
    }

    public void SaveWorld(){
        SerializationHandler.SaveTerrain(chunkManager);
    }

    public void LoadWorld(){
        SerializationHandler.GetSavedTerrains();
    }

    public void BackToMenu(){
        SceneManager.LoadScene("MainMenu");
    }

    public void ChangeResolution(){
        Debug.Log("Resolution change");
        Vector2Int res = resolutionMap[resolutionDropdown.options[resolutionDropdown.value].text];
        Screen.SetResolution(res.x, res.y, FullScreenMode.FullScreenWindow);
        UserConfig uc = new UserConfig();
        uc.WinWidth = res.x;
        uc.WinHeight = res.y;
        UserConfig.SaveConfig(uc);
    }

    public void CopySeedToClipboard(){
        GUIUtility.systemCopyBuffer = chunkManager.SeedGenerator.seed.ToString();
    }

    public void UpdateFOV(Slider slider){
        FOVdisplay.text = slider.value.ToString();
        cam.fieldOfView = slider.value;
        chunkManager.PlayerController.normalFOV = (int)slider.value;
    }
}
