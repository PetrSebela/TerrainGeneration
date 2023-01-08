using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    public float[,] heightMap;
    public int currentLODindex;
    public Vector3 position;
    public Vector2 borderVector;
    public MeshData currentMeshData;
    public Dictionary<Spawable,Matrix4x4[]> detailDictionary = new Dictionary<Spawable, Matrix4x4[]>();
    public float localMaximum;
    public float localMinimum;

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
            chunkSettings.size,
            chunkSettings.maxResolution,
            currentLODindex,
            this.borderVector,
            chunkManager.globalNoiseLowest,
            chunkManager.globalNoiseHighest,
            chunkManager.MaxTerrainHeight
        );
        return currentMeshData;
    }
}

public enum Spawable
{
    ConiferTree,
    DeciduousTree,
    Rock,
    Bush 
}