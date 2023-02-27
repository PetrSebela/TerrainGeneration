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
    public Slider FOVslider;
    public GameObject PauseMenuObject;
    public GameObject CanvasParent;
    public GameObject SavePrompt;
    public TMP_Dropdown QualityDropdown;

    public void Start(){
        // SeedDisplay.text = "Currently on seed : " + chunkManager.SeedGenerator.seed;  
        // resolutionDropdown.value = resolutionDropdown.options.FindIndex(option => option.text == Screen.width + "x" + Screen.height);
    }

    public void QuitSimulation(){
        Debug.Log("Quitting");
        Application.Quit();
    }

    public void SaveWorld(){
        GameObject go = Instantiate(SavePrompt);

        go.GetComponent<Prompt>().ChunkManager = chunkManager;
        go.GetComponent<Prompt>().PauseMenu = PauseMenuObject;
        PauseMenuObject.SetActive(false);
        go.transform.parent = CanvasParent.transform;
        go.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f,0.5f);
        go.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f,0.5f);
        go.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;
        
        // SerializationHandler.SaveTerrain(chunkManager);
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
        chunkManager.UserConfig.WinWidth = res.x;
        chunkManager.UserConfig.WinHeight = res.y;
        UserConfig.SaveConfig(chunkManager.UserConfig);
    }

    public void CopySeedToClipboard(){
        GUIUtility.systemCopyBuffer = chunkManager.SeedGenerator.seed.ToString();
    }

    public void UpdateFOV(Slider slider){
        Debug.Log("fov change");
        FOVdisplay.text = slider.value.ToString();
        cam.fieldOfView = (int)slider.value;
        chunkManager.PlayerController.normalFOV = (int)slider.value;
        chunkManager.UserConfig.UserFOV = (int)slider.value;
        UserConfig.SaveConfig(chunkManager.UserConfig);
    }

    public void UpdateQuality(){
        Debug.Log(QualityDropdown.value);
        QualitySettings.SetQualityLevel(QualityDropdown.value);
        chunkManager.UserConfig.LevelDetail = QualityDropdown.value;
        UserConfig.SaveConfig(chunkManager.UserConfig);
    }
}
