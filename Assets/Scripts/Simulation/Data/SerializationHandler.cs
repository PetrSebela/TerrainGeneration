using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

public static class SerializationHandler
{
    public static void SaveTerrain( ChunkManager ChunkManager,string saveName){
        ChunkManager.simulationSettings.Name = saveName;

        TerrainSettingsSerialized tSettings = new TerrainSettingsSerialized(ChunkManager.TerrainSettings);
        SimulationSettingsSerialized sSettings = new SimulationSettingsSerialized(ChunkManager.simulationSettings, ChunkManager);
        SimulationState simState = ChunkManager.SimulationState;

        BinaryFormatter formatter = new BinaryFormatter();
        string folderPath =  Application.dataPath + "/worlds/" + saveName + "/";
        DirectoryInfo WorldFolder = Directory.CreateDirectory(folderPath);
    
        string tsj = JsonUtility.ToJson(tSettings);
        File.WriteAllText(folderPath + "TerrainSettings.TerSet",tsj);

        string ssj = JsonUtility.ToJson(sSettings);
        File.WriteAllText(folderPath + "SimulationSettings.SimSet",ssj);

        string simStateJson = JsonUtility.ToJson(simState);
        File.WriteAllText(folderPath + "SimulationState.SimSta",simStateJson);


        Debug.Log("World saved in folder : " + WorldFolder.FullName );
    }

    public static void DeserializeTerrainSettings(string directoryPath, SceneDirector sceneDirector){
        BinaryFormatter formatter = new BinaryFormatter();
        TerrainSettingsSerialized terrainSettings;
        SimulationSettingsSerialized simulationSettings;
        
        foreach (string filePath in Directory.GetFiles(directoryPath)){
            string extenstion = filePath.Split(".")[filePath.Split(".").Length-1];
            
            using(StreamReader reader = new StreamReader(filePath)){
                
                switch(extenstion){
                    case "TerSet":
                        terrainSettings = JsonUtility.FromJson<TerrainSettingsSerialized>(reader.ReadToEnd());
                        TerrainSettingsSerialized.SetDataToSGO(terrainSettings,sceneDirector.TerrainSettings);
                        break;

                    case "SimSet":
                        simulationSettings = JsonUtility.FromJson<SimulationSettingsSerialized>(reader.ReadToEnd());
                        SimulationSettingsSerialized.SetDataToSGO(simulationSettings,sceneDirector.SimulationSettings);
                        break;

                    default:
                        break;
                }
            }
        }
    }

    public static SimulationState DeserializeSimulatinoState(string seed){
        string fullPath = Application.dataPath + "/worlds/" + seed + "/SimulationState.SimSta";
        try
        {
            StreamReader reader = new StreamReader(fullPath);
            SimulationState s =  JsonUtility.FromJson<SimulationState>(reader.ReadToEnd());
            
            Debug.Log("Deserialized position : " + s.ViewerPosition);
            Debug.Log("Deserialized orientation : " + s.ViewerOrientation);

            reader.Close();
            return s;
                        
        }
        catch (System.IO.DirectoryNotFoundException)
        {
            Debug.Log("Folder not found : " + fullPath);
            return null;
        }
    }

    public static DirectoryInfo[] GetSavedTerrains(){
        string folderPath =  Application.dataPath + "/worlds/";
        string[] directoryPaths = Directory.GetDirectories(folderPath);
        DirectoryInfo[] directoryDatas = new DirectoryInfo[directoryPaths.Length];

        for (int i = 0; i < directoryPaths.Length; i++)
        {
            directoryDatas[i] = new DirectoryInfo(directoryPaths[i]);
        }
        return directoryDatas;
    }
}

public enum SupportedFileTypes{
    TerSet,
    SimSet
}