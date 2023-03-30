using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class NoiseConverter{
    float min1;
    float high1;
    float min2;
    float high2;
    AnimationCurve terrainCurve;
    public NoiseConverter(float min1,float high1, float min2, float high2, AnimationCurve terrainCurve){
        this.min1 = min1;
        this.high1 = high1;
        this.min2 = min2;
        this.high2 = high2;
        this.terrainCurve = terrainCurve;
    }

    public float GetNormalized(float rawValue){
        float range01 = 0 + (rawValue - min1) * (1 - 0) / (high1 - min1);
        return terrainCurve.Evaluate(range01);
        
    }

    public float GetRealHeight(float value){
        float range01 = 0 + (value - min1) * (1 - 0) / (high1 - min1);
        float modHeight = terrainCurve.Evaluate(range01);
        float realHeight = min2 + (modHeight - 0) * (high2 - min2) / (1 - 0);
        return realHeight;
    }
}