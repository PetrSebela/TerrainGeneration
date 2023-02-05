using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

public static class SerializationHandler
{
    public static void SaveTerrain( ChunkManager ChunkManager){
        BinaryFormatter formatter = new BinaryFormatter();
        TerrainSettingsSerialized tSettings = new TerrainSettingsSerialized(ChunkManager.TerrainSettings);
        SimulationSettingsSerialized sSettings = new SimulationSettingsSerialized(ChunkManager.simulationSettings, ChunkManager);

        string subFolder = (sSettings.Seed == "") ? ChunkManager.SeedGenerator.seed.ToString() : sSettings.Seed;
        string folderPath =  Application.dataPath + "/worlds/" + subFolder + "/";

        DirectoryInfo WorldFolder = Directory.CreateDirectory(folderPath);

        string tsj = JsonUtility.ToJson(tSettings);
        File.WriteAllText(folderPath + "TerrainSettings.TerSet",tsj);

        string ssj = JsonUtility.ToJson(sSettings);
        File.WriteAllText(folderPath + "SimulationSettings.SimSet",ssj);

        Debug.Log("World saved in folder : " + WorldFolder.FullName );
    }

    public static void DeserializeTerrain(string directoryPath, SceneDirector sceneDirector){
        BinaryFormatter formatter = new BinaryFormatter();
        TerrainSettingsSerialized terrainSettings;
        SimulationSettingsSerialized simulationSettings;
        
        foreach (string filePath in Directory.GetFiles(directoryPath)){
            Debug.Log(filePath);
            string extenstion = filePath.Split(".")[filePath.Split(".").Length-1];
            
            using(StreamReader reader = new StreamReader(filePath)){
                
                switch(extenstion){
                    case "TerSet":
                        terrainSettings = JsonUtility.FromJson<TerrainSettingsSerialized>(reader.ReadToEnd());
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

    public static DirectoryInfo[] GetSavedTerrains(){
        string folderPath =  Application.dataPath + "/worlds/";
        string[] dPaths = Directory.GetDirectories(folderPath);
        DirectoryInfo[] dInfos = new DirectoryInfo[dPaths.Length];

        for (int i = 0; i < dPaths.Length; i++)
        {
            Debug.Log(dPaths[i]);
            dInfos[i] = new DirectoryInfo(dPaths[i]);
        }
        return dInfos;
    }
}

public enum SupportedFileTypes{
    TerSet,
    SimSet
}