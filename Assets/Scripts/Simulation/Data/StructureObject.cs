using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="StructureObject",menuName ="SciprableOnjects/StructureObject",order = 6)]
public class StructureObject : ScriptableObject
{
    public string Name;

    [Header("Location settings")]
    public Range SpawnRange;
    public float SlopeLimit;
    public float Radius;
    public float MinDistanceFromStructures;

    [Header("Rarity settigns")]
    public int Count;

    [Header("Graphics")]
    public GameObject Structure;
}
