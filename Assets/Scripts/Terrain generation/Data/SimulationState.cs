using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulationState
{
    public bool IsPaused = false;
    public bool GenerationCompleted = false;
    public Vector3 ViewerPosition = new Vector3(0,0,0);
    public Vector2 ViewerOrientation = new Vector2(0,0);

    
    public SimulationState()
    {

    }
}
