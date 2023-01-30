using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainModifier
{
    public ChunkManager ChunkManager;
    public NoiseConverter NoiseConverter;

    public TerrainModifier(ChunkManager chunkManager, NoiseConverter noiseConverter)
    {
        ChunkManager = chunkManager;
        NoiseConverter = noiseConverter;
    }

    public void LevelTerrain(Vector2Int originChunk,Vector2Int originPosition, Vector2Int size, float desiredHeight){
        int resolution = ChunkManager.ChunkSettings.ChunkResolution;
        float[,] heightMap = ChunkManager.ChunkDictionary[originChunk].HeightMap;

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                float xMod = x / (float) size.x * 2 - 1;
                float yMod = y / (float) size.y * 2 - 1;

                float p = Mathf.Max(Mathf.Abs(xMod),Mathf.Abs(yMod));

                p = ChunkManager.TerrainEaseCurve.Evaluate(p);
                heightMap[x + originPosition.x, y+ originPosition.y] = desiredHeight * (1 - p) + heightMap[x + originPosition.x, y+ originPosition.y] * p;
            }            
        }
    }

    public float CalculateParameter(float from, float to, float value){
        if (value <= from)
            return 0;
        if (value >= to)
            return 1;

        return (1/(to - from)) * value;
    }
}
