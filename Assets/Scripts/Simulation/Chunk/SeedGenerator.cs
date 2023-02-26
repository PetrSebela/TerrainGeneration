using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeedGenerator
{
    public Vector2[] NoiseLayers = new Vector2[24];
    public Vector2[] EntityTemperatureMapOffsets;
    public Vector2[] EntityHumidityMapOffsets;

    public int seed;
    public ChunkManager ChunkManager;

    public SeedGenerator(int seed, ChunkManager chunkManager)
    {
        this.seed = seed;
        ChunkManager = chunkManager;
        Debug.Log("Internal seed int: " + this.seed);
        Random.InitState(seed);
        GenerateValues();
        GenerateEntityOffsets();
    }

    public SeedGenerator(string seedString, ChunkManager chunkManager){
        ChunkManager = chunkManager;
        if(seedString == ""){
            this.seed = Time.realtimeSinceStartup.GetHashCode();
            Random.InitState(this.seed);
            Debug.Log("Internal seed random: " + this.seed);
            GenerateValues();        
            GenerateEntityOffsets();    
        }
        else{
            Debug.Log(seedString);
            this.seed = seedString.GetHashCode();
            Random.InitState(this.seed);
            Debug.Log("Internal seed string : " + seedString.GetHashCode().ToString());
            GenerateValues();
            GenerateEntityOffsets();    
        }
    }


    private void GenerateValues()
    {
        for (int i = 0; i < NoiseLayers.Length; i++)
        {
            NoiseLayers[i] = new Vector2(Random.value * 100000, Random.value * 100000);
        }
    }

    private void GenerateEntityOffsets(){
        Debug.Log(ChunkManager.TreeObjects);
        EntityTemperatureMapOffsets = new Vector2[ChunkManager.TreeObjects.Length];
        EntityHumidityMapOffsets = new Vector2[ChunkManager.TreeObjects.Length];

        for (int i = 0; i < ChunkManager.TreeObjects.Length; i++)
        {
            EntityTemperatureMapOffsets[i] = new Vector2(Random.value * 100000, Random.value * 100000);
            EntityHumidityMapOffsets[i] = new Vector2(Random.value * 100000, Random.value * 100000);
        }
    }
}
