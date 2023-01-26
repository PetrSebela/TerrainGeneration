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

        for (int xChunk = -chunkManager.WorldSize; xChunk < chunkManager.WorldSize; xChunk++)
        {
            for (int yChunk = -chunkManager.WorldSize; yChunk < chunkManager.WorldSize; yChunk++)
            {
                Vector2 toGenerate = new Vector2(xChunk, yChunk);
                float[,] heightMap = new float[64,64];
                float highestValue = -Mathf.Infinity;
                float lowestValue = Mathf.Infinity;

                // preparing compute shader
                heightMapBuffer.SetData(heightMap);
                chunkManager.HeightMapShader.SetVector("offset", toGenerate);
                chunkManager.HeightMapShader.SetBuffer(0, "heightMap", heightMapBuffer);
                chunkManager.HeightMapShader.SetBuffer(0, "layerOffsets", offsets);
                chunkManager.HeightMapShader.SetFloat("size",chunkManager.WorldSize);
                chunkManager.HeightMapShader.SetFloat("persistence",chunkManager.persistence);
                chunkManager.HeightMapShader.SetFloat("lacunarity",chunkManager.lacunarity);
                chunkManager.HeightMapShader.SetInt("octaves",chunkManager.octaves);
                chunkManager.HeightMapShader.Dispatch(0, 4, 4, 1);
                heightMapBuffer.GetData(heightMap);


                // Finding lowest and highest point
                for (int y = 0; y < 64; y++)
                {
                    for (int x = 0; x < 64; x++)
                    {
                        if(heightMap[x,y] < lowestValue){
                            lowestValue = heightMap[x,y]; 
                        }
                       
                        float checkedValue = heightMap[x,y];
                        if( checkedValue > highestValue){
                            highestValue = checkedValue;
                        }
                    }
                }

                if(lowestValue < chunkManager.globalNoiseLowest){
                    chunkManager.globalNoiseLowest = lowestValue;
                }
                if(highestValue > chunkManager.globalNoiseHighest){
                    chunkManager.globalNoiseHighest = highestValue;
                }     

                Chunk chunk = new Chunk(heightMap, new Vector3(toGenerate.x, 0, toGenerate.y), lowestValue, highestValue);
                chunkManager.ChunkDictionary.Add(toGenerate, chunk);
            }
            yield return null;
        }
        heightMapBuffer.Dispose();
        offsets.Dispose();
        Debug.Log("HeightMap Generation Finished");


        //! Enviromental detail
        NoiseConverter nosieConverter = new NoiseConverter(
            chunkManager.globalNoiseLowest,
            chunkManager.globalNoiseHighest,
            -chunkManager.MaxTerrainHeight/3,
            chunkManager.MaxTerrainHeight,
            chunkManager.terrainCurve
        );
        
        Dictionary<Spawnable,int> spawnableCounter = new Dictionary<Spawnable, int>(){
            {Spawnable.ConiferTree,0},
            {Spawnable.DeciduousTree,0},
            {Spawnable.Rock,0},
            {Spawnable.Bush,0}
        };

        for (int xChunk = -chunkManager.WorldSize; xChunk < chunkManager.WorldSize; xChunk++)
        {
            for (int yChunk = -chunkManager.WorldSize; yChunk < chunkManager.WorldSize; yChunk++)
            {
                Vector2 key = new Vector2(xChunk,yChunk);
                Chunk chunk = chunkManager.ChunkDictionary[key];
                //! Water chunks
                if(nosieConverter.GetRealHeight(chunk.localMinimum) < chunkManager.waterLevel){
                    validWaterChunk.Add(key * chunkManager.ChunkSettings.size);
                }

                //! Enviromental detail
                // this solution is only temporary 
                // I will probably refactor fucking everything
                Dictionary<Spawnable,List<Matrix4x4>> enviromentalDetail = new Dictionary<Spawnable, List<Matrix4x4>>(){
                    {Spawnable.ConiferTree,new List<Matrix4x4>()},
                    {Spawnable.DeciduousTree,new List<Matrix4x4>()},
                    {Spawnable.Rock,new List<Matrix4x4>()},
                    {Spawnable.Bush,new List<Matrix4x4>()},
                };

                foreach (SpawnableSettings item in chunkManager.spSettings)
                {
                    
                    List<Matrix4x4> listReference = enviromentalDetail[item.type];
                    for (int i = 0; i < item.countInChunk; i++)
                    {
                        int xTreeCoord = UnityEngine.Random.Range(0, 63);
                        int zTreeCoord = UnityEngine.Random.Range(0, 63);
                        float height = nosieConverter.GetRealHeight(chunk.heightMap[xTreeCoord, zTreeCoord]);

                        // Calculate tree base normal
                        Vector3 p1 = new Vector3(xTreeCoord     , nosieConverter.GetRealHeight(chunk.heightMap[xTreeCoord      , zTreeCoord    ]), zTreeCoord);
                        Vector3 p2 = new Vector3(xTreeCoord + 1 , nosieConverter.GetRealHeight(chunk.heightMap[xTreeCoord + 1  , zTreeCoord    ]), zTreeCoord);
                        Vector3 p3 = new Vector3(xTreeCoord     , nosieConverter.GetRealHeight(chunk.heightMap[xTreeCoord      , zTreeCoord + 1]), zTreeCoord + 1);
                        Vector3 normal = Vector3.Cross(p3 - p1, p2 - p1);

                        if(item.minHeight * chunkManager.MaxTerrainHeight  < height && height < item.maxHeight * chunkManager.MaxTerrainHeight && Vector3.Angle(Vector3.up, normal) < item.maxSlope && height > chunkManager.waterLevel + 3)
                        {
                            Vector3 position = new Vector3(
                                (key.x * chunkManager.ChunkSettings.size) + (((float)(xTreeCoord - 1) / chunkManager.ChunkSettings.resolution) * chunkManager.ChunkSettings.size),
                                height,
                                (key.y * chunkManager.ChunkSettings.size) + (((float)(zTreeCoord - 1) / chunkManager.ChunkSettings.resolution) * chunkManager.ChunkSettings.size));
                            
                            Matrix4x4 matrix4X4 = Matrix4x4.TRS(
                                position, 
                                Quaternion.Euler(new Vector3(0,Random.Range(0,360),0)), 
                                Vector3.one * Random.Range(item.minScale,item.maxScale));

                            listReference.Add(matrix4X4);
                        }
                    }
                }
                // Converting list to array
                Dictionary<Spawnable,Matrix4x4[]> enviromentalDetailArray = new Dictionary<Spawnable, Matrix4x4[]>();

                foreach (var item in enviromentalDetail.Keys)
                {
                    enviromentalDetailArray.Add(item, enviromentalDetail[item].ToArray());
                } 
                chunk.detailDictionary = enviromentalDetailArray;

                chunkManager.LowDetail.Add(chunk);

                //! Monuments 
                float highestValue = -Mathf.Infinity;
                Vector3 highestPoint = Vector3.zero;
                for (int y = 1; y < 63; y++)
                {
                    for (int x = 1; x < 63; x++)
                    {                    
                        float checkedValue = nosieConverter.GetRealHeight(chunk.heightMap[x,y]);
                        bool isPeak = checkedValue > highestValue &&
                            checkedValue > chunk.heightMap[x,y + 1] &&
                            checkedValue > chunk.heightMap[x,y - 1] &&
                            checkedValue > chunk.heightMap[x + 1,y] &&
                            checkedValue > chunk.heightMap[x - 1,y];
                        
                        if(isPeak) {
                            highestValue = checkedValue;
                            highestPoint = new Vector3(x,checkedValue,y);
                        }
                    }
                }

                if (chunk.localMaximum != 0){                    
                    // finding POI
                    Vector2 comparativePosition = new Vector2(
                        (key.x * chunkManager.ChunkSettings.size) + (((float)(highestPoint.x - 1) / chunkManager.ChunkSettings.resolution) * chunkManager.ChunkSettings.size),
                        (key.y * chunkManager.ChunkSettings.size) + (((float)(highestPoint.z - 1) / chunkManager.ChunkSettings.resolution) * chunkManager.ChunkSettings.size)
                    );

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

                    if(highestPoint.y > chunkManager.Peaks[closestIndex].y){
                        Vector3 inWorldPosition = new Vector3(
                            (key.x * chunkManager.ChunkSettings.size) + (((float)(highestPoint.x - 1) / chunkManager.ChunkSettings.resolution) * chunkManager.ChunkSettings.size),
                            highestPoint.y,
                            (key.y * chunkManager.ChunkSettings.size) + (((float)(highestPoint.z - 1) / chunkManager.ChunkSettings.resolution) * chunkManager.ChunkSettings.size)
                        );

                        chunkManager.Peaks[closestIndex] = inWorldPosition;
                    }
                }
            }
            chunkManager.enviromentProgress++;
            yield return null;
        }
        Debug.Log("Enviroment Generation Finished");

        //! Constructing chunks

        for (int x = -chunkManager.WorldSize; x < chunkManager.WorldSize; x++)
        {
            for (int y = -chunkManager.WorldSize; y < chunkManager.WorldSize; y++)
            {
                Vector2 sampler = new Vector2(x, y);
                lock (chunkManager.ChunkUpdateRequestQueue)
                    chunkManager.ChunkUpdateRequestQueue.Enqueue(sampler);
            }
        }

        ThreadStart meshConstructorThread = delegate{
            MeshConstructorManager.ConstructChunkMesh_T(chunkManager);
        };


        for (int i = 0; i < 1; i++)
        {
            Thread thread = new Thread(meshConstructorThread);
            thread.Start();
        }


        while (chunkManager.ChunkUpdateRequestQueue.Count > 0)
        {
            yield return null;
        }

        while (chunkManager.MeshQueue.Count > 0)
        {
            ChunkUpdate update = chunkManager.MeshQueue.Dequeue();
            GameObject chunk = new GameObject();
            chunk.layer = LayerMask.NameToLayer("Ground");
            chunk.isStatic = true;
            chunk.transform.parent = chunkManager.transform;
            chunk.transform.position = update.meshData.position * chunkManager.ChunkSettings.size;
            chunk.transform.name = update.meshData.position.ToString();

            MeshFilter meshFilter = chunk.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = chunk.AddComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;

            Mesh mesh = new Mesh();
            mesh.vertices = update.meshData.vertexList;
            mesh.triangles = update.meshData.triangleList;
            mesh.RecalculateNormals();

            chunkManager.MeshDictionary.Add(new Vector2(update.position.x,update.position.z),mesh);

            if(update.LODindex == 1){
                MeshCollider meshCollider = chunk.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = mesh;
            }
            
            chunkManager.ChunkObjectDictionary.Add(update.position,chunk);
        }

        // Filling Tree Dictionary
        for (int x = -5; x < 5; x++)
        {
            for (int y = -5; y < 5; y++)
            {
                Vector2 key = new Vector2(x,y);
                chunkManager.TreeChunkDictionary.Add(key, chunkManager.ChunkDictionary[key]);
                chunkManager.LowDetail.Remove(chunkManager.ChunkDictionary[key]);
            }
        }

        // generating water
        if (chunkManager.UseWater)
        {
            float size = chunkManager.ChunkSettings.size;
            float worldSize = chunkManager.WorldSize * size;
            foreach (Vector2 chunk in validWaterChunk)
            {
                GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                plane.transform.position = new Vector3(chunk.x,0,chunk.y) + new Vector3(size / 2,chunkManager.waterLevel, + size / 2);
                plane.transform.localScale = Vector3.one / 10 * size;
                plane.GetComponent<MeshRenderer>().material = chunkManager.WaterMaterial;
            }

            Vector3[] positions = new Vector3[]{
                new Vector3(1,0,0),
                new Vector3(0,0,1),
                new Vector3(-1,0,0),
                new Vector3(0,0,-1),


                new Vector3(1,0,1),
                new Vector3(-1,0,1),
                new Vector3(1,0,-1),
                new Vector3(-1,0,-1),
            };

            foreach (var item in positions)
            {
                GameObject w1 = GameObject.CreatePrimitive(PrimitiveType.Plane);
                w1.transform.position = new Vector3(
                    item.x * worldSize * 2,
                    chunkManager.waterLevel,
                    item.z * worldSize * 2
                );
                w1.transform.localScale = Vector3.one / 10 * worldSize*2;
                w1.GetComponent<MeshRenderer>().material = chunkManager.WaterMaterial;                
            }


            // far water chunkg
        }

        foreach (Vector3 pos in chunkManager.Peaks)
        {
            float angle = Mathf.Rad2Deg*Mathf.Atan(pos.x/pos.z) - 90;
            Transform.Instantiate(chunkManager.HighestPointMonument,pos,Quaternion.Euler(0,angle - 90,0));
        }

        
        chunkManager.MapTexture = MapTextureGenerator.GenerateMapTexture(chunkManager.ChunkDictionary,Vector2.zero,Vector2.one,Vector2Int.zero,chunkManager.WorldSize,chunkManager.ChunkSettings.resolution,chunkManager);
        chunkManager.MapDisplay.texture = chunkManager.MapTexture;

        chunkManager.TerrainMaterial.SetVector("_HeightRange", new Vector2(-chunkManager.MaxTerrainHeight/3,chunkManager.MaxTerrainHeight));
        chunkManager.BatchEnviroment();  
        yield return chunkManager.BatchMeshes();

        chunkManager.GenerationComplete = true;
        Debug.Log("Chunk Prerender Generation Finished");
        Debug.Log(string.Format("Max : {0} | Min : {1}",chunkManager.globalNoiseHighest,chunkManager.globalNoiseLowest));
        Debug.Log("World generation and prerender corutine complete");
    }


}

class NoiseConverter{
    float min1;
    float high1;
    float min2;
    float high2;
    AnimationCurve terrainCurve;
    public NoiseConverter(float min1,float high1, float min2, float high2, AnimationCurve terrainCurve){
        this.min1 = min1;
        this.high1 = high1;
        this.min2 = min2;
        this.high2 = high2;
        this.terrainCurve = terrainCurve;
    }

    public float GetRealHeight(float value){
        float range01 = 0 + (value - min1) * (1 - 0) / (high1 - min1);
        float modHeight = terrainCurve.Evaluate(range01);
        float realHeight = min2 + (modHeight - 0) * (high2 - min2) / (1 - 0);
        return realHeight;
    }
}
