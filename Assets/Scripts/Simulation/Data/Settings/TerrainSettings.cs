using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="TerrainSettings",menuName ="SciprableOnjects/TerrainSettings",order = 2)]
public class TerrainSettings : ScriptableObject
{
    [Header("Height Map")]
    public float Persistence;
    public float Lacunarity;
    public int Octaves;

    [Range(0,1)]
    public float WrinkleMagniture;

    [Header("Terrain")]
    public float MaxHeight;
    public float MinHeight;
}

[System.Serializable]
public class TerrainSettingsSerialized{
    [Header("Height Map")]
    public float Persistence;
    public float Lacunarity;
    public int Octaves;

    public float WrinkleMagniture;

    [Header("Terrain")]
    public float MaxHeight;
    public float MinHeight;
    
    public TerrainSettingsSerialized(TerrainSettings terrainSettings){
        Persistence = terrainSettings.Persistence;
        Lacunarity = terrainSettings.Lacunarity;
        Octaves = terrainSettings.Octaves;
        MaxHeight = terrainSettings.MaxHeight;
        MinHeight = terrainSettings.MinHeight;
        WrinkleMagniture = terrainSettings.WrinkleMagniture;
    }

    public static void SetDataToSGO(TerrainSettingsSerialized serialized, TerrainSettings terrainSettings){
        terrainSettings.Persistence = serialized.Persistence;
        terrainSettings.Lacunarity = serialized.Lacunarity;
        terrainSettings.Octaves = serialized.Octaves;
        terrainSettings.MaxHeight = serialized.MaxHeight;
        terrainSettings.MinHeight = serialized.MinHeight;
        terrainSettings.WrinkleMagniture = serialized.WrinkleMagniture;
    }
}
