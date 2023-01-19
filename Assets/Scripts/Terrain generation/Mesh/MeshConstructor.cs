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
    public static MeshData ConstructTerrain(float[,] chunkData, Vector3 position, float chunkSize, int chunkResolution, int LODnumber, Vector2 borderWith, float noiseMin, float noiseMax, float maxHeight, ChunkManager chunkManager)
    {
        float[,] _chunkData = (float[,])chunkData.Clone();

        // fixing border between chunks with different resolution
        if (borderWith != Vector2.zero)
        {
            if (borderWith.x == 1)
            {
                for (int x = 0; x < chunkResolution / LODnumber; x++)
                {
                    if (x % 2 != 0)
                    {
                        float v1 = _chunkData[(x - 1) * LODnumber + 1, chunkResolution + 1];
                        float v2 = _chunkData[(x + 1) * LODnumber + 1, chunkResolution + 1];
                        _chunkData[x * LODnumber + 1, chunkResolution + 1] = (v1 + v2) / 2;
                    }
                }
            }
            else if (borderWith.x == -1)
            {
                for (int x = 0; x < chunkResolution / LODnumber; x++)
                {
                    if (x % 2 != 0)
                    {
                        float v1 = _chunkData[(x - 1) * LODnumber + 1, 1];
                        float v2 = _chunkData[(x + 1) * LODnumber + 1, 1];
                        _chunkData[x * LODnumber + 1, 1] = (v1 + v2) / 2;
                    }
                }
            }

            if (borderWith.y == 1)
            {

                for (int x = 0; x < chunkResolution / LODnumber; x++)
                {
                    if (x % 2 != 0)
                    {
                        float v1 = _chunkData[chunkResolution + 1, (x - 1) * LODnumber + 1];
                        float v2 = _chunkData[chunkResolution + 1, (x + 1) * LODnumber + 1];
                        _chunkData[chunkResolution + 1, x * LODnumber + 1] = (v1 + v2) / 2;
                    }
                }
            }
            else if (borderWith.y == -1)
            {
                for (int x = 0; x < chunkResolution / LODnumber; x++)
                {
                    if (x % 2 != 0)
                    {
                        float v1 = _chunkData[1, (x - 1) * LODnumber + 1];
                        float v2 = _chunkData[1, (x + 1) * LODnumber + 1];
                        _chunkData[1, x * LODnumber + 1] = (v1 + v2) / 2;
                    }
                }
            }
        }


        List<Vector3> vertexList = new List<Vector3>();
        List<int> triangleList = new List<int>();
        int vertexCout = 0;
        float sampleRate = (chunkSize) / ((float)(chunkResolution) / LODnumber);

        for (int x = 0; x < chunkResolution / LODnumber; x++)
        {
            for (int y = 0; y < chunkResolution / LODnumber; y++)
            {
                for (int offsetIndex = 0; offsetIndex < offsets.Length; offsetIndex++)
                {

                    float xPosition = (x + offsets[offsetIndex].x) * sampleRate;
                    float yPosition = (y + offsets[offsetIndex].y) * sampleRate;
                    float height = _chunkData[(x + offsets[offsetIndex].x) * LODnumber + 1, (y + offsets[offsetIndex].y) * LODnumber + 1];

                    vertexList.Add(new Vector3(
                        xPosition,
                        -maxHeight/3 + (height - noiseMin) * (maxHeight - -maxHeight/3) / (noiseMax - noiseMin),
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
        // combine duplicate vertices
        //! probably just memory saver if it eats too much cpu time remove it i will have method for calculating smooth normals anyways
        // Dictionary<Vector3, int> duplicateMapping = new Dictionary<Vector3, int>();

        // int vertexMapIndex = 0;
        // foreach (Vector3 item in vertexList)
        // {
        //     if (!duplicateMapping.ContainsKey(item))
        //     {
        //         duplicateMapping.Add(item, vertexMapIndex++);
        //     }
        // }

        // List<Vector3> constructVertexList = new List<Vector3>();
        // List<int> constructTriangleList = new List<int>();
        // foreach (int item in triangleList)
        // {
        //     constructTriangleList.Add(duplicateMapping[vertexList[item]]);
        // }

        // foreach (Vector3 item in duplicateMapping.Keys)
        // {
        //     constructVertexList.Add(item);
        // }

        // MeshData meshData = new MeshData(constructVertexList.ToArray(), constructTriangleList.ToArray(), position);
        
        MeshData meshData = new MeshData(vertexList.ToArray(), triangleList.ToArray(), position);
        return meshData;
    }
}
public class MeshData
{
    public readonly Vector3[] vertexList;
    public Vector3[] normals;
    public readonly int[] triangleList;
    public readonly Vector3 position;

    public MeshData(Vector3[] vertexList, int[] triangleList, Vector3 position)
    {
        this.vertexList = vertexList;
        this.triangleList = triangleList;
        this.position = position;
    }
}
