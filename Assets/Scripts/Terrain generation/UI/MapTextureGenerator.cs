using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapTextureGenerator : MonoBehaviour
{
    public static IEnumerator GenerateMapTexture(Dictionary<Vector2, Chunk> chunkMap, int worldSize, int chunkResolution, ChunkManager chunkManager)
        {
        
        Texture2D mapTexture = new Texture2D(
            worldSize*chunkResolution*2, 
            worldSize*chunkResolution*2
        );
        
        
        NoiseConverter converter = new NoiseConverter(
            chunkManager.GlobalNoiseLowest,
            chunkManager.GlobalNoiseHighest,
            0,
            1,
            chunkManager.terrainCurve);


        Texture2D tex = TextureCreator.GenerateTexture();

        for (int x = -worldSize; x < worldSize; x++)
        {
            for (int y = -worldSize; y < worldSize; y++)
            {
                Vector2 key =  new Vector2(x,y);
                float[,] heightMap = chunkMap[key].HeightMap;        
                for (int xInChunk = 0; xInChunk < chunkResolution; xInChunk++)
                {
                    for (int yInChunk = 0; yInChunk < chunkResolution; yInChunk++)
                    {
                        float value = heightMap[xInChunk,yInChunk];
                        float color = Mathf.Abs(converter.GetRealHeight(value));
                        Vector3 p1 = new Vector3(xInChunk     , converter.GetRealHeight(heightMap[xInChunk      , yInChunk    ]), yInChunk);
                        Vector3 p2 = new Vector3(xInChunk + 1 , converter.GetRealHeight(heightMap[xInChunk + 1  , yInChunk    ]), yInChunk);
                        Vector3 p3 = new Vector3(xInChunk     , converter.GetRealHeight(heightMap[xInChunk      , yInChunk + 1]), yInChunk + 1);
                        Vector3 normal = Vector3.Cross(p3 - p1, p2 - p1);   

                        float uvy = Vector3.Angle(normal,Vector3.up) / 90;
                        
                        Color col = tex.GetPixel(
                            (int)(uvy * 512),
                            (int)(converter.GetNormalized(value) * 512)
                        );
                        col.a = 1;

                        if(converter.GetRealHeight(value) <= chunkManager.waterLevel){
                            col = new Color(65/255, 200/255, 255/255);
                        }
                        
                        mapTexture.SetPixel(
                            x * chunkResolution + xInChunk + ( worldSize * chunkResolution ),
                            y * chunkResolution + yInChunk + ( worldSize * chunkResolution ),
                            col);
                    }
                }
                chunkManager.MapProgress++;
            }
            yield return null;
        }

        mapTexture.Apply();
        chunkManager.MapDisplay.texture = mapTexture;
    }
}
