using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeedGenerator
{
    public Vector2[] noiseLayers = new Vector2[24];

    public SeedGenerator(int seed)
    {
        Random.InitState(seed);
        GenerateValues();
    }


    private void GenerateValues()
    {
        for (int i = 0; i < noiseLayers.Length; i++)
        {
            noiseLayers[i] = new Vector2(Random.value, Random.value);
        }
    }
}
