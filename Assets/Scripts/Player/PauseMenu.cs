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
    public Dropdown resolutionDropdown;
    private Dictionary<string,Vector2Int> resolutionMap = new Dictionary<string, Vector2Int>(){
        {"2560x1440",new Vector2Int(2560,1440)},
        {"1920x1080",new Vector2Int(1920,1080)},
        {"1366x768",new Vector2Int(1366,768)},
        {"1280x720",new Vector2Int(1280,720)},
    };
    public SimulationSettings simulationSettings;
    public ChunkManager chunkManager;
    public TMP_Text SeedDisplay;

    public void Start(){
        // Currently on seed :
        SeedDisplay.text = "Currently on seed : " + chunkManager.SeedGenerator.seed;  
        // this.transform.parent.
    }

    public void QuitSimulation(){
        Debug.Log("Quitting");
        Application.Quit();
    }

    public void SaveWorld(){
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream fileStream = new FileStream("SavedWorlds/SavedWorld.world",FileMode.Create);
        
        formatter.Serialize(fileStream, chunkManager.ChunkDictionary[Vector2.zero]);
        Debug.Log(simulationSettings);    
        fileStream.Close();
    }

    public void LoadWorld(){
        try{
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream("SavedWorlds/SavedWorld.world", FileMode.Open);
            Chunk data = formatter.Deserialize(stream) as Chunk;
            Debug.Log(data.position);    
            stream.Close();
        }
        catch{
            Debug.Log("file error");
        }
    }

    public void BackToMenu(){
        SceneManager.LoadScene("MainMenu");
    }

    public void ChangeResolution(){
        Debug.Log("Resolution change");
        Vector2Int res = resolutionMap[resolutionDropdown.options[resolutionDropdown.value].text];
        Screen.SetResolution(res.x, res.y, FullScreenMode.FullScreenWindow);
    }
}
