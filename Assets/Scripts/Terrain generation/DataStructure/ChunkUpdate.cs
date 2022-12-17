using UnityEngine;
public struct ChunkUpdate
{
    public Vector3 position;
    // public int LODindex;
    public MeshData meshData;
    public ChunkUpdate(Vector3 position, MeshData meshData) //, int LODindex)
    {
        this.position = position;
        this.meshData = meshData;
        // this.LODindex = LODindex;
    }
}