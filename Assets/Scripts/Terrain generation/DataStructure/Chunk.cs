using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    public float[,] heightMap;
    public int nodeDepth;
    public Vector3 position;
    public Vector2 borderVector;
    public MeshData currentMeshData;
    public Matrix4x4[] treesTransforms;

    public Chunk(float[,] heightMap, Vector3 position, float chunkSize, int chunkResolution, Matrix4x4[] treesTransforms)
    {
        this.heightMap = heightMap;
        this.position = position;
        this.treesTransforms = treesTransforms;
    }

    // public MeshData GetMeshData(ChunkSettings chunkSettings,int nodeDepth) //(int LODindex, Vector2 borderVector, ChunkSettings chunkSettings)
    // {
    //     // this.borderVector = borderVector;
    //     // currentLODindex = LODindex;
    //     this.nodeDepth = nodeDepth;
        
    //     currentMeshData = MeshConstructor.ConstructTerrain(
    //         heightMap,
    //         position,
    //         chunkSettings.size,
    //         chunkSettings.maxResolution,
    //         nodeDepth,
    //         this.borderVector
    //     );
    //     return currentMeshData;
    // }
}