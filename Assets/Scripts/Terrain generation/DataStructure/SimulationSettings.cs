using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="SimulationSettings",menuName ="SciprableOnjects/SimulationSettings",order = 1)]
public class SimulationSettings : ScriptableObject
{
    public float MaxHeight;
    public string Seed;
    public int WorldSize;

    public override string ToString()
    {
        return string.Format("maxHeight {0}, seed {1}, size {2}",MaxHeight,Seed,WorldSize);
    }
}
