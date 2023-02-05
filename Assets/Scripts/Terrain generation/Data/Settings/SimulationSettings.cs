using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="SimulationSettings",menuName ="SciprableOnjects/SimulationSettings",order = 1)]
public class SimulationSettings : ScriptableObject
{
    public string Seed;
    public int WorldSize;
}

public class SimulationSettingsSerialized{
    public string Seed;
    public int WorldSize; 

    public SimulationSettingsSerialized(SimulationSettings simulationSettings,ChunkManager chunkManager){
        Seed = chunkManager.SeedGenerator.seed.ToString();
        WorldSize = simulationSettings.WorldSize;
    }

    public static void SetDataToSGO(SimulationSettingsSerialized serialize, SimulationSettings settings){
        settings.Seed = serialize.Seed;
        settings.WorldSize = serialize.WorldSize;
    }
}