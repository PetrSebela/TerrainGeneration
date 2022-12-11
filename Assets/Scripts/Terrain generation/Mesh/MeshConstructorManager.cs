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
            Vector2? toGenerateNull = null;

            lock (chunkManager.ChunkUpdateRequestQueue)
            {
                if (chunkManager.ChunkUpdateRequestQueue.Count != 0)
                    toGenerateNull = chunkManager.ChunkUpdateRequestQueue.Dequeue();
            }

            if (toGenerateNull != null)
            {
                Vector2 toGenerate = (Vector2)toGenerateNull;
                toGenerate -= chunkManager.PastChunkPosition;

                int LODindex;
                if (Math.Abs(toGenerate.x) >= 32 || Math.Abs(toGenerate.y) >= 32)
                    LODindex = 32;
                else if (Math.Abs(toGenerate.x) >= 24 || Math.Abs(toGenerate.y) >= 24)
                    LODindex = 16;
                else if (Math.Abs(toGenerate.x) >= 12 || Math.Abs(toGenerate.y) >= 12)
                    LODindex = 8;
                else if (Math.Abs(toGenerate.x) >= 6 || Math.Abs(toGenerate.y) >= 6)
                    LODindex = 4;
                else if (Math.Abs(toGenerate.x) >= 3 || Math.Abs(toGenerate.y) >= 3)
                    LODindex = 2;
                else
                    LODindex = 1;

                // getting border vector
                // distance - 1
                // int[] borderNumbers = new int[] { 2, 3, 4, 5, 8 };
                Vector2 borderVector = Vector2.zero;
                int[] borderNumbers = new int[] { 2, 5, 11, 23, 31 };

                for (int i = 0; i < borderNumbers.Length; i++)
                {
                    if (toGenerate.y == borderNumbers[i] && toGenerate.x <= borderNumbers[i] && toGenerate.x >= -borderNumbers[i])
                        borderVector.x = (borderVector.x == 0) ? 1 : borderVector.x;

                    if (toGenerate.y == -borderNumbers[i] && toGenerate.x <= borderNumbers[i] && toGenerate.x >= -borderNumbers[i])
                        borderVector.x = (borderVector.x == 0) ? -1 : borderVector.x;

                    if (toGenerate.x == borderNumbers[i] && toGenerate.y <= borderNumbers[i] && toGenerate.y >= -borderNumbers[i])
                        borderVector.y = (borderVector.y == 0) ? 1 : borderVector.y;

                    if (toGenerate.x == -borderNumbers[i] && toGenerate.y <= borderNumbers[i] && toGenerate.y >= -borderNumbers[i])
                        borderVector.y = (borderVector.y == 0) ? -1 : borderVector.y;
                }

                toGenerate += chunkManager.PastChunkPosition;

                if (chunkManager.ChunkDictionary[toGenerate].currentLODindex != LODindex || chunkManager.ChunkDictionary[toGenerate].borderVector != borderVector)
                {
                    MeshData meshData = chunkManager.ChunkDictionary[toGenerate].GetMeshData(LODindex, borderVector, chunkManager.ChunkSettings);
                    lock (chunkManager.MeshQueue)
                    {
                        ChunkUpdate chunkUpdate = new ChunkUpdate(new Vector3(toGenerate.x, 0, toGenerate.y), meshData, LODindex);
                        chunkManager.MeshQueue.Enqueue(chunkUpdate);
                    }
                }
            }
        }
    }
}
