using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ChunkUpdateProcessor
{
    ChunkManager ChunkManager;
    public ChunkUpdateProcessor(ChunkManager chunkManager){
        ChunkManager = chunkManager;
    }

    public void UpdateProcessingThread(){
        while (true)
        {        
            if(ChunkManager.MeshRequests.Count > 0)
            {
                MeshRequest meshRequest;
                lock(ChunkManager.MeshRequests)
                {   
                    meshRequest = ChunkManager.MeshRequests.Dequeue();
                }
                if(meshRequest.CallbackObject != null){

                    Vector2 checkPosition =  meshRequest.CallbackObject.position - new Vector2(meshRequest.ViewerPosition.x,meshRequest.ViewerPosition.z); //new Vector2(meshRequest.Position.x,meshRequest.Position.z);

                    int LODindex = 1;

                    if (Math.Abs(checkPosition.x) >= 6 || Math.Abs(checkPosition.y) >= 6)
                        LODindex = 16;
                    else if (Math.Abs(checkPosition.x) >= 5 || Math.Abs(checkPosition.y) >= 5)
                        LODindex = 8;
                    else if (Math.Abs(checkPosition.x) >= 4 || Math.Abs(checkPosition.y) >= 4)
                        LODindex = 4;
                    else if (Math.Abs(checkPosition.x) >= 3 || Math.Abs(checkPosition.y) >= 3)
                        LODindex = 2;
                    else if (Math.Abs(checkPosition.x) >= 2 || Math.Abs(checkPosition.y) >= 2)
                        LODindex = 1;

                    // getting border vector
                    // distance - 1
                    Vector2 borderVector = Vector2.zero;
                    int[] borderNumbers = new int[] { 2, 3, 4, 5 };

                    for (int i = 0; i < borderNumbers.Length; i++)
                    {
                        if (checkPosition.y == borderNumbers[i] && checkPosition.x <= borderNumbers[i] && checkPosition.x >= -borderNumbers[i])
                            borderVector.x = (borderVector.x == 0) ? 1 : borderVector.x;

                        if (checkPosition.y == -borderNumbers[i] && checkPosition.x <= borderNumbers[i] && checkPosition.x >= -borderNumbers[i])
                            borderVector.x = (borderVector.x == 0) ? -1 : borderVector.x;

                        if (checkPosition.x == borderNumbers[i] && checkPosition.y <= borderNumbers[i] && checkPosition.y >= -borderNumbers[i])
                            borderVector.y = (borderVector.y == 0) ? 1 : borderVector.y;

                        if (checkPosition.x == -borderNumbers[i] && checkPosition.y <= borderNumbers[i] && checkPosition.y >= -borderNumbers[i])
                            borderVector.y = (borderVector.y == 0) ? -1 : borderVector.y;
                    }

                    MeshData meshData = MeshConstructor.ConstructTerrain(
                        meshRequest.HeightMap,
                        meshRequest.Position,
                        LODindex,
                        borderVector,
                        ChunkManager
                    );

                    lock(ChunkManager.MeshUpdates)
                    {
                        ChunkManager.MeshUpdates.Enqueue(new MeshUpdate(meshData, meshRequest.CallbackObject));
                    }
                }
            }
        }
    }
}
