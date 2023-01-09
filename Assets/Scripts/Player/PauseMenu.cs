using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.IO;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public SimulationSettings simulationSettings;
    public ChunkManager chunkManager;
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
}
