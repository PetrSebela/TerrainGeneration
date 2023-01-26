using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapTextureGenerator : MonoBehaviour
{
    public static Texture2D GenerateMapTexture(Dictionary<Vector2, Chunk> chunkMap, int worldSize, int chunkResolution, ChunkManager chunkManager)
        {
        
        Texture2D mapTexture = new Texture2D(
            worldSize*chunkResolution*2, 
            worldSize*chunkResolution*2
        );
        
        
        NoiseConverter converter = new NoiseConverter(
            chunkManager.globalNoiseLowest,
            chunkManager.globalNoiseHighest,
            0,
            1,
            chunkManager.terrainCurve);
        
        for (int x = -worldSize; x < worldSize; x++)
        {
            for (int y = -worldSize; y < worldSize; y++)
            {
                Vector2 key =  new Vector2(x,y);
                float[,] heightMap = chunkMap[key].heightMap;        
                for (int xChunk = 0; xChunk < chunkResolution; xChunk++)
                {
                    for (int yChunk = 0; yChunk < chunkResolution; yChunk++)
                    {
                        float value = heightMap[xChunk,yChunk];
                        float color = Mathf.Abs(converter.GetRealHeight(value));
                        mapTexture.SetPixel(
                            x * chunkResolution + xChunk + ( worldSize * chunkResolution ),
                            y * chunkResolution + yChunk + ( worldSize * chunkResolution ),
                            new Color(color,color,color));
                    }
                }
            }
        }

        mapTexture.Apply();
        return mapTexture;
    }
}
