using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Chunk
{
    [SerializeField] public float[,] heightMap;
    [SerializeField] public int currentLODindex;
    [SerializeField] public Vector3 position;
    [SerializeField] public Vector2 borderVector;
    [SerializeField] public MeshData currentMeshData;
    [SerializeField] public Dictionary<Spawable,Matrix4x4[]> detailDictionary = new Dictionary<Spawable, Matrix4x4[]>();
    [SerializeField] public float localMaximum;
    [SerializeField] public float localMinimum;

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
            chunkManager.MaxTerrainHeight,
            chunkManager
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