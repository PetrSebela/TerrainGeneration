using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeedGenerator
{
    public Vector2[] noiseLayers = new Vector2[24];
    public int seed;

    public SeedGenerator(int seed)
    {
        Debug.Log("Internal seed int: " + this.seed);
        Random.InitState(seed);
        GenerateValues();
    }

    public SeedGenerator(string seedString){
        if(seedString == ""){
            this.seed = Time.realtimeSinceStartup.GetHashCode();
            Random.InitState(this.seed);

            Debug.Log("Internal seed random: " + this.seed);
            GenerateValues();            
        }
        else{
            this.seed = seedString.GetHashCode();
            Random.InitState(this.seed);
            Debug.Log("Internal seed string : " + seedString.GetHashCode().ToString());
            GenerateValues();
        }
    }


    private void GenerateValues()
    {
        for (int i = 0; i < noiseLayers.Length; i++)
        {
            noiseLayers[i] = new Vector2(Random.value * 1000, Random.value * 1000);
        }
    }
}
