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
    public Matrix4x4[] treesTransforms;

    public Dictionary<Spawable,Matrix4x4[]> treesDictionary = new Dictionary<Spawable, Matrix4x4[]>();

    public Chunk(float[,] heightMap, Vector3 position, float chunkSize, int chunkResolution)
    {
        this.heightMap = heightMap;
        this.position = position;
    }

    public MeshData GetMeshData(int LODindex, Vector2 borderVector, ChunkSettings chunkSettings)
    {
        this.borderVector = borderVector;
        currentLODindex = LODindex;

        currentMeshData = MeshConstructor.ConstructTerrain(
            heightMap,
            position,
            chunkSettings.size,
            chunkSettings.maxResolution,
            currentLODindex,
            this.borderVector
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