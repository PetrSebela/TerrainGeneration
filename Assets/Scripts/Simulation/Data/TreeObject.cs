using UnityEngine;

[CreateAssetMenu(fileName ="TreeObject",menuName ="SciprableOnjects/TreeObject",order = 5)]
public class TreeObject : ScriptableObject
{
    public string Name;

    [Header("Location settings")]
    public Range SpawnRange;
    public float SlopeLimit;
    public float Radius;

    [Header("Rarity settigns")]
    public int Count;
    public Range TemperatureRange;
    public Range HumidityRange;

    [Header("Variation settings")]
    public Vector3 BaseSize = Vector3.one;
    public Vector3 SizeVariation = Vector3.zero;
    
    [Header("Graphics")]
    public Mesh Mesh;
    public Material[] MeshMaterials;


    [Header("LOD settings")]
    public Texture2D[] ImpostorTextures;
    public Material BaseImpostorMaterial;
    public Material[] ImpostorMaterials;
}
