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
    [SerializeField] private Dropdown maxHeightField;
    [SerializeField] private Dropdown worldSize;
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
        simulationSettings.maxHeight = float.Parse(maxHeightField.options[maxHeightField.value].text);
        simulationSettings.seed = seedField.text;
        simulationSettings.worldSize = int.Parse(worldSize.options[worldSize.value].text);

        Debug.Log(simulationSettings.maxHeight);
        Debug.Log(simulationSettings.seed);
        Debug.Log(simulationSettings.worldSize);
        
        if (simulationSettings.maxHeight != float.NaN){
            SceneManager.LoadScene("Simulation",LoadSceneMode.Single);
        }        
    }

    public void QuitApplication(){
        Application.Quit();
    }
}
