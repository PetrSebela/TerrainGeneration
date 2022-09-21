using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator : MonoBehaviour
{
    public static MeshBuildData ConstructChunkMesh(float[,,] chunkData, Vector3 position, float surfaceLevel, float chunkSize)
    {

        List<Vector3> vertexList = new List<Vector3>();
        List<int> triangleList = new List<int>();
        int vertexCout = 0;
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    Vector3[] vertices = new Vector3[]{
                        new Vector3(x    ,y,z    ),
                        new Vector3(x    ,y,z + 1),
                        new Vector3(x + 1,y,z + 1),
                        new Vector3(x + 1,y,z    ),

                        new Vector3(x    ,y + 1,z    ),
                        new Vector3(x    ,y + 1,z + 1),
                        new Vector3(x + 1,y + 1,z + 1),
                        new Vector3(x + 1,y + 1,z    ),
                    };

                    int cubeIndex = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        if (chunkData[(int)vertices[i].x, (int)vertices[i].y, (int)vertices[i].z] < surfaceLevel)
                        {
                            cubeIndex += (int)Mathf.Pow(2, i);
                        }
                    }

                    int[] tri = TriangulationFile.triTable[cubeIndex];

                    for (int i = 0; i < 12; i++)
                    {
                        int indexA = TriangulationFile.edgeToVertex[i][0];
                        int indexB = TriangulationFile.edgeToVertex[i][1];
                        float a = chunkData[(int)vertices[indexA].x, (int)vertices[indexA].y, (int)vertices[indexA].z];
                        float b = chunkData[(int)vertices[indexB].x, (int)vertices[indexB].y, (int)vertices[indexB].z];

                        float interValue = (surfaceLevel - a) / (b - a);

                        Vector3 vertexPosition = Vector3.Lerp(vertices[indexA], vertices[indexB], interValue);

                        vertexList.Add(vertexPosition);
                    }

                    foreach (int edgeIndex in tri)
                    {
                        triangleList.Add(edgeIndex + vertexCout);
                    }
                    vertexCout += 12;
                }
            }
        }

        // mesh.vertices = vertexList.ToArray();
        // mesh.triangles = triangleList.ToArray();
        // mesh.RecalculateNormals();
        MeshBuildData meshData = new MeshBuildData(vertexList.ToArray(), triangleList.ToArray(), position);
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
