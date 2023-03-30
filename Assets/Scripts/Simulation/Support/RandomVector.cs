using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RandomVector
{
    public static Vector2 RandomVector2Whole(float min, float max){
        return new Vector2(
            (int)Random.Range( min, max),
            (int)Random.Range( min, max)
        );
    }

    public static Vector2Int RandomVector2IntWhole(float min, float max){
        return new Vector2Int(
            (int)Random.Range( min, max),
            (int)Random.Range( min, max)
        );
    }
}
