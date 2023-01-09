using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="SimulationSettings",menuName ="SciprableOnjects/SimulationSettings",order = 1)]
public class SimulationSettings : ScriptableObject
{
    public float maxHeight;
    public string seed;
    public int worldSize;

    public override string ToString()
    {
        return string.Format("maxHeight {0}, seed {1}, size {2}",maxHeight,seed,worldSize);
    }
}

[System.Serializable]
public class SimulationSettingsSave{
    public float maxHeight;
    public string seed;
    public int worldSize;

    public SimulationSettingsSave(SimulationSettings simulationSettings)
    {
        this.maxHeight = simulationSettings.maxHeight;
        this.seed = simulationSettings.seed;
        this.worldSize = simulationSettings.worldSize;
    }

    public override string ToString()
    {
        return string.Format("maxHeight {0}, seed {1}, size {2}",maxHeight,seed,worldSize);
    }
}
