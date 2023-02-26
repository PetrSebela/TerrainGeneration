using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="SimulationSettings",menuName ="SciprableOnjects/SimulationSettings",order = 1)]
public class SimulationSettings : ScriptableObject
{
    public string Seed;
    public int WorldSize;
    public string Name;
}

public class SimulationSettingsSerialized{
    public string Seed;
    public int WorldSize; 
    public string Name;


    public SimulationSettingsSerialized(SimulationSettings simulationSettings,ChunkManager chunkManager){
        Seed = chunkManager.SeedGenerator.seed.ToString();
        WorldSize = simulationSettings.WorldSize;
        Name = simulationSettings.Name;
    }

    public static void SetDataToSGO(SimulationSettingsSerialized serialize, SimulationSettings settings){
        settings.Seed = serialize.Seed;
        settings.WorldSize = serialize.WorldSize;
        settings.Name = serialize.Name;
    }
}