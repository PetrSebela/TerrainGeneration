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

    public static MeshBuildData ConstructTerrain(float[,] chunkData, Vector3 position, float chunkSize, int chunkResolution)
    {

        List<Vector3> vertexList = new List<Vector3>();
        List<int> triangleList = new List<int>();
        int vertexCout = 0;

        float sampleRate = chunkSize / chunkResolution;

        for (int x = 0; x < chunkResolution; x++)
        {
            for (int y = 0; y < chunkResolution; y++)
            {
                for (int offsetIndex = 0; offsetIndex < offsets.Length; offsetIndex++)
                {
                    float xPosition = (x + offsets[offsetIndex].x) * sampleRate;
                    float yPosition = (y + offsets[offsetIndex].y) * sampleRate;

                    vertexList.Add(new Vector3(xPosition, chunkData[x + offsets[offsetIndex].x, y + offsets[offsetIndex].y], yPosition));
                }

                for (int tIndex = 0; tIndex < triOrder.Length; tIndex++)
                {
                    triangleList.Add(vertexCout + triOrder[tIndex]);
                }

                vertexCout += 4;
            }
        }

        // combine vertices
        Dictionary<Vector3, int> duplicateMapping = new Dictionary<Vector3, int>();

        int vertexMapIndex = 0;
        foreach (Vector3 item in vertexList)
        {
            if (!duplicateMapping.ContainsKey(item))
            {
                duplicateMapping.Add(item, vertexMapIndex++);
            }
        }

        List<Vector3> constructVertexList = new List<Vector3>();
        List<int> constructTriangleList = new List<int>();
        foreach (int item in triangleList)
        {
            constructTriangleList.Add(duplicateMapping[vertexList[item]]);
        }

        foreach (Vector3 item in duplicateMapping.Keys)
        {
            constructVertexList.Add(item);
        }



        MeshBuildData meshData = new MeshBuildData(constructVertexList.ToArray(), constructTriangleList.ToArray(), position);
        return meshData;
    }
}
public struct MeshBuildData
{
    public readonly Vector3[] vertexList;
    public readonly int[] triangleList;
    public readonly Vector3 position;

    public MeshBuildData(Vector3[] vertexList, int[] triangleList, Vector3 position)
    {
        this.vertexList = vertexList;
        this.triangleList = triangleList;
        this.position = position;
    }
}
