using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public static class MeshConstructorManager
{
    public static void ConstructChunkMesh_T(ChunkManager chunkManager)
    {
        while (true)
        {
            // Pulling data from queue
            TreeNode toGenerateNull = null;
            lock (chunkManager.ChunkUpdateRequestQueue)
            {
                if (chunkManager.ChunkUpdateRequestQueue.Count != 0)
                    toGenerateNull = chunkManager.ChunkUpdateRequestQueue.Dequeue();
            }

            if (toGenerateNull != null)
            {
                TreeNode toGenerate = (TreeNode)toGenerateNull;
                
                int nodeDepth = toGenerate.NodeDepth();
                (Vector2 position, int depth) key = new (toGenerate.Position,nodeDepth);
                // Constructing mesh
                // MeshData meshData = chunkManager.ChunkDictionary[key].GetMeshData(chunkManager.ChunkSettings, nodeDepth);
                MeshData meshData = chunkManager.meshBuilder.ConstructTerrain(toGenerate);
                // Displaying mesh
                lock (chunkManager.MeshQueue)
                {
                    ChunkUpdate chunkUpdate = new ChunkUpdate(new Vector3(toGenerate.Position.x, 0, toGenerate.Position.y), meshData);//, LODindex);
                    chunkManager.MeshQueue.Enqueue(chunkUpdate);
                }
            }
        }
    }
}