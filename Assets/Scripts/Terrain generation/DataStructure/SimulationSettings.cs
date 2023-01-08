using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="SimulationSettings",menuName ="SciprableOnjects/SimulationSettings",order = 1)]
public class SimulationSettings : ScriptableObject
{
    public float maxHeight;
    public string seed;
    public int worldSize;
}
