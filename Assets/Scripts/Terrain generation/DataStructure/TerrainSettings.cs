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

    [Header("Terrain")]
    public float MaxHeight;
    public float MinHeight;
}
