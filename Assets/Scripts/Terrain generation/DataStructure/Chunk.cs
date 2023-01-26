using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Chunk
{
    [Header("Rendering")]
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public MeshCollider meshCollider;
    
    public Vector2 position;
    public Vector2 borderVector;
    
    public float[,] heightMap;
    
    public int currentLODindex;

    public MeshData currentMeshData;
    public Dictionary<Spawnable,Matrix4x4[]> detailDictionary = new Dictionary<Spawnable, Matrix4x4[]>();
    public float localMaximum;
    public float localMinimum;

    public Vector3[] vertices;
    public int[] triangles;

    public Chunk(float[,] heightMap, Vector3 position, float localMinimum, float localMaximum)
    {
        this.heightMap = heightMap;
        this.position = position;
        this.localMaximum = localMaximum;
        this.localMinimum = localMinimum;
    }

    public MeshData GetMeshData(int LODindex, Vector2 borderVector, ChunkSettings chunkSettings, ChunkManager chunkManager)
    {
        this.borderVector = borderVector;
        currentLODindex = LODindex;

        currentMeshData = MeshConstructor.ConstructTerrain(
            heightMap,
            position,
            currentLODindex,
            this.borderVector,
            chunkManager
        );

        vertices = currentMeshData.vertexList;
        triangles = currentMeshData.triangleList;

        currentMeshData.normals = CalculateNormals();
        
        return currentMeshData;
    }
    public Vector3[] CalculateNormals(){
        Vector3[] normals = new Vector3[vertices.Length];
        int triangleCount = triangles.Length/3;

        for (int i = 0; i < triangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = triangles[normalTriangleIndex];
            int vertexIndexB = triangles[normalTriangleIndex + 1];
            int vertexIndexC = triangles[normalTriangleIndex + 2];

            Vector3 vertexA = vertices[vertexIndexA];
            Vector3 vertexB = vertices[vertexIndexB];
            Vector3 vertexC = vertices[vertexIndexC];

            Vector3 normal = Vector3.Cross(vertexB-vertexA,vertexC-vertexA);

            normals[vertexIndexA] += normal;
            normals[vertexIndexB] += normal;
            normals[vertexIndexC] += normal;
        }
        
        foreach (Vector3 normal in normals)
        {
            normal.Normalize();
        }

        return normals;
    }
}

public enum Spawnable
{
    ConiferTree,
    DeciduousTree,
    Rock,
    Bush 
}