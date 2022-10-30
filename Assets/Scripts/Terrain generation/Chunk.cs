using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    public float[,] HeightMap;
    public int CurrentLODindex;
    public Vector3 Position;
    public Vector2 BorderVector;
    public MeshData CurrentMeshData;
    public float ChunkSize;
    public int ChunkResolution;

    public Chunk(float[,] heightMap, Vector3 position, float chunkSize, int chunkResolution)
    {
        HeightMap = heightMap;
        Position = position;
        ChunkSize = chunkSize;
        ChunkResolution = chunkResolution;
    }

    public MeshData GetMeshData(int LODindex, Vector2 borderVector)
    {
        BorderVector = borderVector;
        CurrentLODindex = LODindex;

        CurrentMeshData = MeshConstructor.ConstructTerrain(
            HeightMap,
            Position,
            ChunkSize,
            ChunkResolution,
            CurrentLODindex,
            BorderVector
        );

        return CurrentMeshData;
    }
}
