using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshConstructor : MonoBehaviour
{
    private static Vector2Int[] offsets = new Vector2Int[]{
        new Vector2Int(0,0),
        new Vector2Int(1,0),
        new Vector2Int(1,1),
        new Vector2Int(0,1),
    };

    private static int[] triOrder = new int[]{
        0,2,1,
        0,3,2
    };

    // takes in constant size 2D array and creates mesh with variable leavel of detail
    public static MeshData ConstructTerrain(float[,] chunkData, Vector3 position, int LODindex, Vector2 borderWith, ChunkManager chunkManager)
    {
        float[,] chunkDataLocal = (float[,])chunkData.Clone();

        NoiseConverter noiseConverter = new NoiseConverter(
            chunkManager.GlobalNoiseLowest,
            chunkManager.GlobalNoiseHighest,
            chunkManager.TerrainSettings.MinHeight,
            chunkManager.TerrainSettings.MaxHeight,
            chunkManager.TerrainCurve
        );

        int chunkResolution = chunkManager.ChunkSettings.ChunkResolution;

        // fixing border between chunks with different resolution
        if (borderWith != Vector2.zero)
        {
            if (borderWith.x == 1)
            {
                for (int x = 0; x < chunkResolution / LODindex; x++)
                {
                    if (x % 2 != 0)
                    {
                        float v1 = chunkDataLocal[(x - 1) * LODindex, chunkResolution];
                        float v2 = chunkDataLocal[(x + 1) * LODindex, chunkResolution];
                        chunkDataLocal[x * LODindex, chunkResolution] = (v1 + v2) / 2;
                    }
                }
            }
            else if (borderWith.x == -1)
            {
                for (int x = 0; x < chunkResolution / LODindex; x++)
                {
                    if (x % 2 != 0)
                    {
                        float v1 = chunkDataLocal[(x - 1) * LODindex, 0];
                        float v2 = chunkDataLocal[(x + 1) * LODindex, 0];
                        chunkDataLocal[x * LODindex, 0] = (v1 + v2) / 2;
                    }
                }
            }

            if (borderWith.y == 1)
            {

                for (int x = 0; x < chunkResolution / LODindex; x++)
                {
                    if (x % 2 != 0)
                    {
                        float v1 = chunkDataLocal[chunkResolution, (x - 1) * LODindex];
                        float v2 = chunkDataLocal[chunkResolution, (x + 1) * LODindex];
                        chunkDataLocal[chunkResolution, x * LODindex] = (v1 + v2) / 2;
                    }
                }
            }
            else if (borderWith.y == -1)
            {
                for (int x = 0; x < chunkResolution / LODindex; x++)
                {
                    if (x % 2 != 0)
                    {
                        float v1 = chunkDataLocal[0, (x - 1) * LODindex];
                        float v2 = chunkDataLocal[0, (x + 1) * LODindex];
                        chunkDataLocal[0, x * LODindex] = (v1 + v2) / 2;
                    }
                }
            }
        }


        List<Vector3> vertexList = new List<Vector3>();
        List<int> triangleList = new List<int>();
        
        int vertexCout = 0;
        float sampleRate = (chunkManager.ChunkSettings.ChunkSize) / ((float)(chunkResolution) / LODindex);
        
        for (int x = 0; x < chunkResolution / LODindex; x++)
        {
            for (int y = 0; y < chunkResolution / LODindex; y++)
            {
                for (int offsetIndex = 0; offsetIndex < offsets.Length; offsetIndex++)
                {
                    float xPosition = (x + offsets[offsetIndex].x) * sampleRate;
                    float yPosition = (y + offsets[offsetIndex].y) * sampleRate;
                    
                    float height = chunkDataLocal[
                        (x + offsets[offsetIndex].x) * LODindex, 
                        (y + offsets[offsetIndex].y) * LODindex];
                    
                    vertexList.Add(new Vector3(
                        xPosition,
                        noiseConverter.GetRealHeight(height),
                        yPosition
                    ));
                }

                for (int tIndex = 0; tIndex < triOrder.Length; tIndex++)
                {
                    triangleList.Add(vertexCout + triOrder[tIndex]);
                }

                vertexCout += 4;
            }
        }
        
        MeshData meshData = new MeshData(vertexList.ToArray(), triangleList.ToArray(), position,LODindex);
        return meshData;
    }

    
}
public class MeshData
{
    public readonly int LOD;
    public readonly Vector3[] vertexList;
    public readonly int[] triangleList;
    public readonly Vector3 position;

    public MeshData(Vector3[] vertexList, int[] triangleList, Vector3 position,int LOD)
    {
        this.vertexList = vertexList;
        this.triangleList = triangleList;
        this.position = position;
        this.LOD = LOD;
    }
}

public struct MeshUpdate{
    public readonly MeshData MeshData;
    public readonly Chunk CallbackObject;

    public MeshUpdate(MeshData meshData, Chunk callbackObject)
    {
        MeshData = meshData;
        CallbackObject = callbackObject;
    }
}
