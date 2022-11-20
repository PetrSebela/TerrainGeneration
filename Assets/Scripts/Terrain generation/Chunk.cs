using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    public float[,] heightMap;
    public int CurrentLODindex;
    public Vector3 position;
    public Vector2 borderVector;
    public MeshData currentMeshData;
    public float chunkSize;
    public int chunkResolution;

    public Chunk(float[,] heightMap, Vector3 position, float chunkSize, int chunkResolution)
    {
        this.heightMap = heightMap;
        this.position = position;
        this.chunkSize = chunkSize;
        this.chunkResolution = chunkResolution;
    }

    public MeshData GetMeshData(int LODindex, Vector2 borderVector)
    {
        this.borderVector = borderVector;
        CurrentLODindex = LODindex;

        currentMeshData = MeshConstructor.ConstructTerrain(
            heightMap,
            position,
            chunkSize,
            chunkResolution,
            CurrentLODindex,
            this.borderVector
        );

        return currentMeshData;
    }
}
