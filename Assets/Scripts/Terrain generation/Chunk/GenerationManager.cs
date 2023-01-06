using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Linq;

public static class GenerationManager
{
    public static IEnumerator GenerationCorutine(ChunkManager chunkManager)
    {
        ComputeBuffer heightMapBuffer = new ComputeBuffer((int)Mathf.Pow(64 + 4, 2), sizeof(float));
        ComputeBuffer offsets = new ComputeBuffer(24, sizeof(float) * 2);

        List<Vector2> validWaterChunk = new List<Vector2>();

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
            chunkManager.HeightMapShader.SetFloat("height",chunkManager.MaxTerrainHeight);

            chunkManager.HeightMapShader.Dispatch(0, 17, 17, 1);
            heightMapBuffer.GetData(heightMap);

            lock (chunkManager.HeightMapDict)
                chunkManager.HeightMapDict.Add(toGenerate, heightMap);

            // Scanning for valid peaks on heightmap
            float Max = -Mathf.Infinity;
            Vector3 HighestPoint = Vector3.zero;
            

            float lowestPoint = Mathf.Infinity;
            for (int y = 1; y < 64; y++)
            {
                for (int x = 1; x < 64; x++)
                {
                    if(heightMap[x,y] < lowestPoint){
                        lowestPoint = heightMap[x,y]; 
                    }
                }
            }
            if (lowestPoint <= 0){
                validWaterChunk.Add(toGenerate * chunkManager.ChunkSettings.size);
            }

            for (int y = 1; y < 64; y++)
            {
                for (int x = 1; x < 64; x++)
                {
                    float checkedValue = heightMap[x,y];
                    if( checkedValue > Max &&
                        checkedValue > heightMap[x,y + 1] &&
                        checkedValue > heightMap[x,y - 1] &&
                        checkedValue > heightMap[x + 1,y] &&
                        checkedValue > heightMap[x - 1,y]){
                        Max = checkedValue;
                        HighestPoint = new Vector3(x,checkedValue,y);
                    }
                }
            }
            

            if (HighestPoint != Vector3.zero){
                Vector3 inWorldPosition = new Vector3(
                    (toGenerate.x * chunkManager.ChunkSettings.size) + (((float)(HighestPoint.x - 1) / chunkManager.ChunkSettings.maxResolution) * chunkManager.ChunkSettings.size),
                    HighestPoint.y,
                    (toGenerate.y * chunkManager.ChunkSettings.size) + (((float)(HighestPoint.z - 1) / chunkManager.ChunkSettings.maxResolution) * chunkManager.ChunkSettings.size));
                
                // finding POI
                Vector2 comparativePosition = new Vector2(
                    (toGenerate.x * chunkManager.ChunkSettings.size) + (((float)(HighestPoint.x - 1) / chunkManager.ChunkSettings.maxResolution) * chunkManager.ChunkSettings.size),
                    (toGenerate.y * chunkManager.ChunkSettings.size) + (((float)(HighestPoint.z - 1) / chunkManager.ChunkSettings.maxResolution) * chunkManager.ChunkSettings.size));
 
                
                int closestIndex = 0;
                float closestDistance = Mathf.Infinity;

                for (int i = 0; i < chunkManager.PeaksPOI.Length; i++)
                {
                    float comparedDistance = Vector2.Distance(chunkManager.PeaksPOI[i], comparativePosition);
                    if (comparedDistance < closestDistance)
                    {
                        closestDistance = comparedDistance;
                        closestIndex = i; 
                    }
                }
                if(HighestPoint.y > chunkManager.Peaks[closestIndex].y){
                    chunkManager.Peaks[closestIndex] = inWorldPosition;
                }
            }


            //! TREE GENERATION
            // this solution is only temporary 
            // I will probably refactor fucking everything
            // Vector3[] trees = new Vector3[chunkManager.ChunkSettings.treesPerChunk];
            Dictionary<Spawable,List<Matrix4x4>> enviromentalDetail = new Dictionary<Spawable, List<Matrix4x4>>(){
                {Spawable.ConiferTree,new List<Matrix4x4>()},
                {Spawable.DeciduousTree,new List<Matrix4x4>()},
                {Spawable.Rock,new List<Matrix4x4>()},
                {Spawable.Bush,new List<Matrix4x4>()},
            };

            foreach (SpawnableSettings item in chunkManager.spSettings)
            {
                for (int i = 0; i < item.countInChunk; i++)
                {
                    int xTreeCoord = UnityEngine.Random.Range(1, 64);
                    int zTreeCoord = UnityEngine.Random.Range(1, 64);
                    float height = heightMap[xTreeCoord, zTreeCoord];

                    // Calculate tree base normal
                    Vector3 p1 = new Vector3(xTreeCoord     , heightMap[xTreeCoord      , zTreeCoord    ], zTreeCoord);
                    Vector3 p2 = new Vector3(xTreeCoord + 1 , heightMap[xTreeCoord + 1  , zTreeCoord    ], zTreeCoord);
                    Vector3 p3 = new Vector3(xTreeCoord     , heightMap[xTreeCoord      , zTreeCoord + 1], zTreeCoord + 1);
                    Vector3 normal = Vector3.Cross(p3 - p1, p2 - p1);

                    if(item.minHeight < height && height < item.maxHeight && Vector3.Angle(Vector3.up, normal) < item.maxSlope && height > chunkManager.waterLevel)
                    {
                        Vector3 position = new Vector3(
                            (toGenerate.x * chunkManager.ChunkSettings.size) + (((float)(xTreeCoord - 1) / chunkManager.ChunkSettings.maxResolution) * chunkManager.ChunkSettings.size),
                            height,
                            (toGenerate.y * chunkManager.ChunkSettings.size) + (((float)(zTreeCoord - 1) / chunkManager.ChunkSettings.maxResolution) * chunkManager.ChunkSettings.size));
                        
                        Matrix4x4 matrix4X4 = Matrix4x4.TRS(
                            position, 
                            Quaternion.Euler(new Vector3(0,Random.Range(0,360),0)), 
                            Vector3.one * Random.Range(2,4));

                        enviromentalDetail[item.type].Add(matrix4X4);
                    }
                }
            }


            // Converting list to array
            Dictionary<Spawable,Matrix4x4[]> enviromentalDetailArray = new Dictionary<Spawable, Matrix4x4[]>();

            foreach (var item in enviromentalDetail.Keys)
            {
                enviromentalDetailArray.Add(item, enviromentalDetail[item].ToArray());
            } 

            Chunk chunk = new Chunk(heightMap, new Vector3(toGenerate.x, 0, toGenerate.y), chunkManager.ChunkSettings.size, chunkManager.ChunkSettings.maxResolution);
            chunk.treesDictionary = enviromentalDetailArray;
            chunkManager.ChunkDictionary.Add(toGenerate, chunk);

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

        for (int x = -chunkManager.ChunkRenderDistance; x < chunkManager.ChunkRenderDistance; x++)
        {
            for (int y = -chunkManager.ChunkRenderDistance; y < chunkManager.ChunkRenderDistance; y++)
            {
                Vector2 sampler = new Vector2(x, y);
                lock (chunkManager.ChunkUpdateRequestQueue)
                    chunkManager.ChunkUpdateRequestQueue.Enqueue(sampler);
            }
        }

        while (chunkManager.ChunkUpdateRequestQueue.Count > 0)
        {
            yield return null;
        }


        // generating water
        if (chunkManager.UseWater)
        {
            float size = chunkManager.ChunkSettings.size;
            float worldSize = chunkManager.ChunkRenderDistance * size;
            foreach (Vector2 chunk in validWaterChunk)
            {
                GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                plane.transform.position = new Vector3(chunk.x,0,chunk.y) + new Vector3(size / 2,chunkManager.waterLevel, + size / 2);
                plane.transform.localScale = Vector3.one / 10 * size;
                plane.GetComponent<MeshRenderer>().material = chunkManager.WaterMaterial;
                
            }
        }

        chunkManager.GenerationComplete = true;
        Debug.Log("World generation and prerender corutine complete");
    }
}
