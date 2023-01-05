using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[System.Serializable]
public struct Range
{
    public float Min;
    public float Max;

    public Range(float min, float max){
        this.Min = min;
        this.Max = max;
    }
}
