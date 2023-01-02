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

    public SeedGenerator(string seed){
        Random.InitState(seed.GetHashCode());
        Debug.Log("Internal seed : " + seed.GetHashCode().ToString());
        GenerateValues();
    }


    private void GenerateValues()
    {
        for (int i = 0; i < noiseLayers.Length; i++)
        {
            noiseLayers[i] = new Vector2(Random.value * 1000, Random.value * 1000);
        }
    }
}
