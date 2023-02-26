using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct Range
{
    [Range(0,1)]
    public float Min;
    
    [Range(0,1)]
    public float Max;

    public Range(float min, float max)
    {
        Min = min;
        Max = max;
    }
}
