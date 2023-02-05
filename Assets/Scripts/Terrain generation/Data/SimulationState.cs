using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationState
{
    public bool IsPaused = false;
    public bool GenerationCompleted = false;
    public Vector3 ViewerPosition;
    public float LoadProgress = 0;
    
    public SimulationState()
    {

    }
}
