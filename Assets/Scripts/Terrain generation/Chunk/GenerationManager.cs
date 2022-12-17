using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public static class GenerationManager
{
    public static IEnumerator GenerationCorutine(ChunkManager chunkManager)
    {
        ComputeBuffer heightMapBuffer = new ComputeBuffer((int)Mathf.Pow(64 + 4, 2), sizeof(float));
        ComputeBuffer offsets = new ComputeBuffer(24, sizeof(float) * 2);

        offsets.SetData(chunkManager.SeedGenerator.noiseLayers);

        while (chunkManager.HeightmapGenQueue.Count > 0)
        {
            Vector2 toGenerate;
            lock (chunkManager.HeightmapGenQueue)
                toGenerate = chunkManager.HeightmapGenQueue.Dequeue();

            // create height map chunk where it overlaps another chunks by 1 on each axis in negative direction
            float[,] heightMap = new float[68, 68];
            heightMapBuffer.SetData(heightMap);

            // preparing compute shader
            chunkManager.HeightMapShader.SetVector("offset", toGenerate);
            chunkManager.HeightMapShader.SetBuffer(0, "heightMap", heightMapBuffer);
            chunkManager.HeightMapShader.SetBuffer(0, "layerOffsets", offsets);

            chunkManager.HeightMapShader.Dispatch(0, 17, 17, 1);
            heightMapBuffer.GetData(heightMap);

            lock (chunkManager.HeightMapDict)
                chunkManager.HeightMapDict.Add(toGenerate, heightMap);

            // generating trees
            // this solution is only temporary 
            // I will probably refactor fucking everything
            Vector3[] trees = new Vector3[chunkManager.ChunkSettings.treesPerChunk];
            for (int i = 0; i < chunkManager.ChunkSettings.treesPerChunk; i++)
            {
                int xTreeCoord = UnityEngine.Random.Range(1, 64);
                int zTreeCoord = UnityEngine.Random.Range(1, 64);

                // for some reason i am offseting by (1;1) in mesh constructino process so here is the compensation
                // THIS CODE SO FUCKED UP
                trees[i] = new Vector3(
                    (toGenerate.x * chunkManager.ChunkSettings.size) + (((float)(xTreeCoord - 1) / chunkManager.ChunkSettings.maxResolution) * chunkManager.ChunkSettings.size),
                    heightMap[xTreeCoord, zTreeCoord],
                    (toGenerate.y * chunkManager.ChunkSettings.size) + (((float)(zTreeCoord - 1) / chunkManager.ChunkSettings.maxResolution) * chunkManager.ChunkSettings.size));
                // heightMap[xTreeCoord, zTreeCoord] += 5;
            }

            Matrix4x4[] treesTranformMatrix = new Matrix4x4[chunkManager.ChunkSettings.treesPerChunk];
            for (int i = 0; i < chunkManager.ChunkSettings.treesPerChunk; i++)
            {
                Matrix4x4 matrix4X4 = Matrix4x4.TRS(trees[i], Quaternion.identity, Vector3.one * 2);
                treesTranformMatrix[i] = matrix4X4;
            }

            Chunk chunk = new Chunk(heightMap, new Vector3(toGenerate.x, 0, toGenerate.y), chunkManager.ChunkSettings.size, chunkManager.ChunkSettings.maxResolution, treesTranformMatrix);
            // lock (chunkManager.ChunkDictionary)
            // chunkManager.ChunkDictionary.Add(toGenerate, chunk);
            chunkManager.LeafDict[toGenerate].Data = chunk;

            // releases corutine execution in order to run other stuff
            if (chunkManager.HeightmapGenQueue.Count % 32 == 0)
                yield return null;
        }

        heightMapBuffer.Dispose();
        offsets.Dispose();

        ThreadStart meshConstructorThread = delegate
        {
            MeshConstructorManager.ConstructChunkMesh_T(chunkManager);
        };

        //these threads will be running forever
        for (int i = 0; i < 1; i++)
        {
            Thread thread = new Thread(meshConstructorThread);
            thread.Start();
        }

        // for (int x = -chunkManager.ChunkRenderDistance; x < chunkManager.ChunkRenderDistance; x++)
        // {
        //     for (int y = -chunkManager.ChunkRenderDistance; y < chunkManager.ChunkRenderDistance; y++)
        //     {
        //         Vector2 sampler = new Vector2(x, y);
        //         lock (chunkManager.ChunkUpdateRequestQueue)
        //             chunkManager.ChunkUpdateRequestQueue.Enqueue(sampler);
        //     }
        // }

        while (chunkManager.ChunkUpdateRequestQueue.Count > 0)
        {
            yield return null;
        }

        chunkManager.GenerationComplete = true;
        Debug.Log("World generation and prerender corutine complete");
    }
}
