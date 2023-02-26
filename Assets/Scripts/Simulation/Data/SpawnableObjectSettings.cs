using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName ="SapwnableObjectSettings",menuName ="SciprableOnjects/SpawnableObjectSettings",order = 4)]
public class SpawnableObjectSettings : ScriptableObject
{
    [Header("Graphics")]
    public GameObject Model;
    public Mesh Mesh;
    public Material[] MeshMaterials;
    public bool InstantiateMesh;

    [Header("Location")]
    public bool SpawnOnLeveledSurface;
    public Vector2Int LevelArea;
    public Vector2Int CountInChunk;
    public Range MinimumHeight;
    public float MaxAngle;
}