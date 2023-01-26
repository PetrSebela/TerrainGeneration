using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using System.IO;

public class SceneDirector : MonoBehaviour
{
    [SerializeField] private SimulationSettings simulationSettings;
    [SerializeField] private TMP_Dropdown maxHeightField;
    [SerializeField] private TMP_Dropdown worldSize;
    [SerializeField] private TMP_InputField seedField;

    public void Start(){
        Screen.SetResolution(1920, 1080, FullScreenMode.FullScreenWindow);
        Debug.Log("loading files");
        DirectoryInfo dir = new DirectoryInfo(Application.dataPath + "/../SavedWorlds/");
        FileInfo[] info = dir.GetFiles("*.world");

        foreach (FileInfo file in info)
        {
            Debug.Log(file);
        }
    }

    public void BeginSimulation(){
        simulationSettings.MaxHeight = float.Parse(maxHeightField.options[maxHeightField.value].text);
        simulationSettings.Seed = seedField.text;
        simulationSettings.WorldSize = int.Parse(worldSize.options[worldSize.value].text);

        Debug.Log(simulationSettings.MaxHeight);
        Debug.Log(simulationSettings.Seed);
        Debug.Log(simulationSettings.WorldSize);
        
        if (simulationSettings.MaxHeight != float.NaN){
            SceneManager.LoadScene("Simulation",LoadSceneMode.Single);
        }        
    }

    public void QuitApplication(){
        Application.Quit();
    }
}
