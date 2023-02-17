using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Linq;

public static class GenerationManager
{

    public static float SampleNoise(Vector2 position,TerrainSettings terrainSettings,ChunkManager chunkManager)
    {
        float value = 0;
        float frequency = 0.002f;
        float amplitude = 1;
        for (int i = 0; i < terrainSettings.Octaves; i++)
        {
            Vector2 modPosition = (position + chunkManager.SeedGenerator.noiseLayers[i]) * frequency;
            value += Mathf.PerlinNoise(modPosition.x,modPosition.y) * amplitude;

            amplitude *= terrainSettings.Persistence;
            frequency *= terrainSettings.Lacunarity;
        }
        return value / terrainSettings.Octaves;
    }
    public static IEnumerator GenerationCorutine(ChunkManager ChunkManager)
    {
        ComputeBuffer heightMapBuffer = new ComputeBuffer((int)Mathf.Pow(64 + 4, 2), sizeof(float));
        ComputeBuffer offsets = new ComputeBuffer(24, sizeof(float) * 2);

        List<Vector2> validWaterChunk = new List<Vector2>();

        offsets.SetData(ChunkManager.SeedGenerator.noiseLayers);

        ChunkManager.ActiveGenerationJob = "Generating noise map";
        Debug.Log(ChunkManager.WorldSize);

        for (int xChunk = -ChunkManager.WorldSize; xChunk < ChunkManager.WorldSize; xChunk++)
        {
            for (int yChunk = -ChunkManager.WorldSize; yChunk < ChunkManager.WorldSize; yChunk++)
            {
                Vector2 generatedChunkPosition = new Vector2(xChunk, yChunk);
                int heightMapSide = ChunkManager.ChunkSettings.ChunkResolution + 2;
                float[,] heightMap = new float[heightMapSide, heightMapSide ];
                float highestValue = -Mathf.Infinity;
                float lowestValue = Mathf.Infinity;

                for (int i = 0; i < heightMapSide; i++)
                {
                    for (int j = 0; j < heightMapSide; j++)
                    {
                        heightMap[i,j] = SampleNoise(
                            new Vector2(
                                xChunk * ChunkManager.ChunkSettings.ChunkSize + i,
                                yChunk * ChunkManager.ChunkSettings.ChunkSize + j
                            ),
                            ChunkManager.TerrainSettings,
                            ChunkManager
                        );

                        float distance = Mathf.Sqrt(
                        Mathf.Pow(xChunk * ChunkManager.ChunkSettings.ChunkSize + i,2) + 
                        Mathf.Pow(yChunk * ChunkManager.ChunkSettings.ChunkSize + j,2));
                        distance /= ChunkManager.WorldSize  * ChunkManager.ChunkSettings.ChunkResolution;
                        heightMap[i,j] *= ChunkManager.TerrainFalloffCurve.Evaluate(distance);
                    }
                }


                // Finding lowest and highest point
                for (int y = 0; y < heightMapSide; y++)
                {
                    for (int x = 0; x < heightMapSide; x++)
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

                if(lowestValue < ChunkManager.GlobalNoiseLowest){
                    ChunkManager.GlobalNoiseLowest = lowestValue;
                }

                if(highestValue > ChunkManager.GlobalNoiseHighest){
                    ChunkManager.GlobalNoiseHighest = highestValue;
                }     
                
                Chunk chunk = new Chunk(
                    heightMap, 
                    generatedChunkPosition, 
                    lowestValue, 
                    highestValue,
                    ChunkManager
                );

                ChunkManager.ChunkDictionary.Add(generatedChunkPosition, chunk);
                yield return null;
            }
        }

        heightMapBuffer.Dispose();
        offsets.Dispose();
        Debug.Log("HeightMap Generation Finished");

        ChunkManager.ActiveGenerationJob = "Generating chunks";
        for (int x = -ChunkManager.WorldSize; x < ChunkManager.WorldSize; x++)
        {
            for (int y = -ChunkManager.WorldSize; y < ChunkManager.WorldSize; y++)
            {
                GameObject chunk = new GameObject();
                chunk.layer = LayerMask.NameToLayer("Ground");
                chunk.isStatic = true;
                chunk.transform.parent = ChunkManager.transform;

                Vector3 position = new Vector3(x,0,y);
                chunk.transform.position =  position * ChunkManager.ChunkSettings.ChunkSize;
                chunk.transform.name = position.ToString();

                MeshFilter meshFilter = chunk.AddComponent<MeshFilter>();
                ChunkManager.ChunkDictionary[new Vector2(position.x,position.z)].MeshFilter = meshFilter;

                MeshRenderer meshRenderer = chunk.AddComponent<MeshRenderer>();
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
                meshRenderer.material = ChunkManager.TerrainMaterial;
                ChunkManager.ChunkDictionary[new Vector2(position.x,position.z)].MeshRenderer = meshRenderer;


                MeshCollider meshCollider = chunk.AddComponent<MeshCollider>();
                ChunkManager.ChunkDictionary[new Vector2(position.x,position.z)].MeshCollider = meshCollider;
                
                // Chunk c = ChunkManager.ChunkDictionary[new Vector2(x,y)];
                // lock(ChunkManager.MeshRequests){
                //     ChunkManager.MeshRequests.Enqueue(new MeshRequest(c.HeightMap,position,c,Vector3.zero));
                // }
            }
        }


        //*--- Generating enviroment ------------------------------------------------------------------------------------------------
        ChunkManager.ActiveGenerationJob = "Generating enviroment detail";

        //! Enviromental detail
        NoiseConverter noiseConverter = new NoiseConverter(
            ChunkManager.GlobalNoiseLowest,
            ChunkManager.GlobalNoiseHighest,
            ChunkManager.TerrainSettings.MinHeight,
            ChunkManager.TerrainSettings.MaxHeight,
            ChunkManager.TerrainCurve
        );
        TerrainModifier  terrainModifier = new TerrainModifier(ChunkManager,noiseConverter);
        
        int belltowerCount = 64;
        for (int i = 0; i < belltowerCount; i++)
        {
            int xChunkHut = Random.Range(-ChunkManager.WorldSize,ChunkManager.WorldSize);
            int yChunkHut = Random.Range(-ChunkManager.WorldSize,ChunkManager.WorldSize);

            int xInChunk = Random.Range(11,ChunkManager.ChunkSettings.ChunkResolution - 11);
            int yInChunk = Random.Range(11,ChunkManager.ChunkSettings.ChunkResolution - 11);

            float[,] heightMap = ChunkManager.ChunkDictionary[new Vector2(xChunkHut,yChunkHut)].HeightMap;

            float desiredHeight = heightMap[xInChunk,yInChunk];

            Vector3 p1 = new Vector3(xInChunk     , noiseConverter.GetRealHeight(heightMap[xInChunk      , yInChunk    ]), yInChunk);
            Vector3 p2 = new Vector3(xInChunk + 1 , noiseConverter.GetRealHeight(heightMap[xInChunk + 1  , yInChunk    ]), yInChunk);
            Vector3 p3 = new Vector3(xInChunk     , noiseConverter.GetRealHeight(heightMap[xInChunk      , yInChunk + 1]), yInChunk + 1);
            Vector3 normal = Vector3.Cross(p3 - p1, p2 - p1);

            bool canSpawnObject = desiredHeight > ChunkManager.waterLevel && 
                                    Vector3.Angle(Vector3.up, normal) < 35 ;
            if(canSpawnObject)
            {
                terrainModifier.LevelTerrain(
                    new Vector2Int(
                        xChunkHut,
                        yChunkHut
                    ),
                    new Vector2Int(
                        xInChunk-5,
                        yInChunk-5
                    ),
                    new Vector2Int(
                        10,
                        10
                    ),
                    desiredHeight
                );

                Vector3 generatePosition = new Vector3(
                    xChunkHut* ChunkManager.ChunkSettings.ChunkSize + xInChunk,
                    noiseConverter.GetRealHeight(desiredHeight),
                    yChunkHut* ChunkManager.ChunkSettings.ChunkSize + yInChunk);

                GameObject obj = GameObject.Instantiate(
                    ChunkManager.BelltowerObject,
                    generatePosition,
                    Quaternion.identity);

                obj.transform.parent = ChunkManager.ChunkDictionary[new Vector2(xChunkHut,yChunkHut)].MeshRenderer.transform; 

                // lock(ChunkManager.MeshRequests){
                //     ChunkManager.MeshRequests.Enqueue(new MeshRequest(
                //         ChunkManager.ChunkDictionary[new Vector2(xChunkHut,yChunkHut)].HeightMap,
                //         new Vector3(xChunkHut,0,yChunkHut),
                //         ChunkManager.ChunkDictionary[new Vector2(xChunkHut,yChunkHut)],
                //         Vector3.zero)
                //         );
                // }
            }
        }

        int signpost = 512;
        for (int i = 0; i < signpost; i++)
        {
            int x = Random.Range(-ChunkManager.WorldSize+5,ChunkManager.WorldSize-5);
            int y = Random.Range(-ChunkManager.WorldSize+5,ChunkManager.WorldSize-5);

            float[,] heightMap = ChunkManager.ChunkDictionary[new Vector2(x,y)].HeightMap;

            int xInChunk = Random.Range(11,ChunkManager.ChunkSettings.ChunkResolution - 11);
            int yInChunk = Random.Range(11,ChunkManager.ChunkSettings.ChunkResolution - 11);

            float desiredHeight = heightMap[xInChunk, yInChunk];


            Vector3 p1 = new Vector3(xInChunk     , noiseConverter.GetRealHeight(heightMap[xInChunk      , yInChunk    ]), yInChunk);
            Vector3 p2 = new Vector3(xInChunk + 1 , noiseConverter.GetRealHeight(heightMap[xInChunk + 1  , yInChunk    ]), yInChunk);
            Vector3 p3 = new Vector3(xInChunk     , noiseConverter.GetRealHeight(heightMap[xInChunk      , yInChunk + 1]), yInChunk + 1);
            Vector3 normal = Vector3.Cross(p3 - p1, p2 - p1);

            bool canSpawnObject = desiredHeight > ChunkManager.waterLevel && 
                                    Vector3.Angle(Vector3.up, normal) < 35;
            if(canSpawnObject)
            {
                terrainModifier.LevelTerrain(
                    new Vector2Int(
                        x,
                        y
                    ),
                    new Vector2Int(
                        xInChunk-5,
                        yInChunk-5
                    ),
                    new Vector2Int(
                        10,
                        10
                    ),
                    desiredHeight
                );

                Vector3 generatePosition = new Vector3(
                    x* ChunkManager.ChunkSettings.ChunkSize + xInChunk,
                    noiseConverter.GetRealHeight(desiredHeight),
                    y* ChunkManager.ChunkSettings.ChunkSize + yInChunk);

                GameObject obj = GameObject.Instantiate(
                    ChunkManager.Signpost,
                    generatePosition,
                    Quaternion.identity);

                obj.transform.parent = ChunkManager.ChunkDictionary[new Vector2(x,y)].MeshRenderer.transform; 


                // lock(ChunkManager.MeshRequests){
                //     ChunkManager.MeshRequests.Enqueue(new MeshRequest(
                //         ChunkManager.ChunkDictionary[new Vector2(x,y)].HeightMap,
                //         new Vector3(x,0,y),
                //         ChunkManager.ChunkDictionary[new Vector2(x,y)],
                //         Vector3.zero)
                //         );
                // }
            }
        }
        
        
        
        for (int xChunk = -ChunkManager.WorldSize; xChunk < ChunkManager.WorldSize; xChunk++)
        {
            for (int yChunk = -ChunkManager.WorldSize; yChunk < ChunkManager.WorldSize; yChunk++)
            {
                Vector2 key = new Vector2(xChunk, yChunk);
                Chunk chunk = ChunkManager.ChunkDictionary[key];
                
                if(noiseConverter.GetRealHeight(chunk.LocalMinimum) < ChunkManager.waterLevel){
                    validWaterChunk.Add(key * ChunkManager.ChunkSettings.ChunkSize);
                }

                Dictionary<TreeObject,List<Matrix4x4>> detailDictionary = new Dictionary<TreeObject, List<Matrix4x4>>();
                
                foreach (TreeObject treeObject in ChunkManager.TreeObjects)
                {
                    detailDictionary.Add(treeObject,new List<Matrix4x4>());                    
                }

                foreach (TreeObject item in ChunkManager.TreeObjects)
                {
                    List<Matrix4x4> listReference = detailDictionary[item];
                    List<Vector2> nodePositions = new List<Vector2>();

                    int objectCount = 0;
                    int iterations = 0;
                    while (objectCount <= item.Count)
                    {
                        if (iterations++ >= item.Count * 2){
                            break;
                        }
                        
                    // }
                    // for (int i = 0; i < item.Count; i++)
                    // {
                        int xTreeCoord = (int)UnityEngine.Random.Range(item.Radius, ChunkManager.ChunkSettings.ChunkResolution - item.Radius);
                        int zTreeCoord = (int)UnityEngine.Random.Range(item.Radius, ChunkManager.ChunkSettings.ChunkResolution - item.Radius);
                        float height = noiseConverter.GetRealHeight(chunk.HeightMap[xTreeCoord, zTreeCoord]);

                        // Calculate base normal
                        Vector3 p1 = new Vector3(xTreeCoord     , noiseConverter.GetRealHeight(chunk.HeightMap[xTreeCoord      , zTreeCoord    ]), zTreeCoord);
                        Vector3 p2 = new Vector3(xTreeCoord + 1 , noiseConverter.GetRealHeight(chunk.HeightMap[xTreeCoord + 1  , zTreeCoord    ]), zTreeCoord);
                        Vector3 p3 = new Vector3(xTreeCoord     , noiseConverter.GetRealHeight(chunk.HeightMap[xTreeCoord      , zTreeCoord + 1]), zTreeCoord + 1);
                        Vector3 normal = Vector3.Cross(p3 - p1, p2 - p1);

                        float temperature = Mathf.PerlinNoise(
                            (xChunk * ChunkManager.ChunkSettings.ChunkSize + xTreeCoord + ChunkManager.SeedGenerator.noiseLayers[0].x) * 0.0075f,                
                            (yChunk * ChunkManager.ChunkSettings.ChunkSize + zTreeCoord + ChunkManager.SeedGenerator.noiseLayers[0].y) * 0.0075f                
                        );

                        float humidity = Mathf.PerlinNoise(
                            (xChunk * ChunkManager.ChunkSettings.ChunkSize + xTreeCoord + ChunkManager.SeedGenerator.noiseLayers[1].x) * 0.0075f,                
                            (yChunk * ChunkManager.ChunkSettings.ChunkSize + zTreeCoord + ChunkManager.SeedGenerator.noiseLayers[1].y) * 0.0075f                
                        );


                        bool canSpawnObject = item.SpawnRange.Min * ChunkManager.TerrainSettings.MaxHeight  < height && 
                                            height < item.SpawnRange.Max * ChunkManager.TerrainSettings.MaxHeight && 
                                            Vector3.Angle(Vector3.up, normal) < item.SlopeLimit && 
                                            height > ChunkManager.waterLevel &&
                                            item.TemperatureRange.Min < temperature && item.TemperatureRange.Max > temperature &&
                                            item.HumidityRange.Min < humidity && item.HumidityRange.Max > humidity &&
                                            DoesntIntersect(item.Radius, new Vector2(xTreeCoord,zTreeCoord),chunk.SizeDescriptorList);
                                            // item.HumidityRange.Min < humidity && item.HumidityRange.Max > humidity;

                        if(canSpawnObject)
                        {
                            chunk.SizeDescriptorList.Add(new ObjectSizeDescriptor(item.Radius, new Vector2(xTreeCoord,zTreeCoord)));
                            Vector3 position = new Vector3(
                                (key.x * ChunkManager.ChunkSettings.ChunkSize) + (((float)(xTreeCoord) / ChunkManager.ChunkSettings.ChunkResolution) * ChunkManager.ChunkSettings.ChunkSize),
                                height,
                                (key.y * ChunkManager.ChunkSettings.ChunkSize) + (((float)(zTreeCoord) / ChunkManager.ChunkSettings.ChunkResolution) * ChunkManager.ChunkSettings.ChunkSize));
                            
                            Matrix4x4 matrix4X4 = Matrix4x4.TRS(
                                position, 
                                Quaternion.Euler(new Vector3(0,Random.Range(0,360),0)), 
                                new Vector3(
                                    item.BaseSize.x + Random.Range( 1 - item.SizeVariation.x, 1 + item.SizeVariation.x),
                                    item.BaseSize.y + Random.Range( 1 - item.SizeVariation.y, 1 + item.SizeVariation.y),
                                    item.BaseSize.z + Random.Range( 1 - item.SizeVariation.z, 1 + item.SizeVariation.z)
                                )    
                            );
                          

                            listReference.Add(matrix4X4);
                            objectCount++;
                        }
                    }
                }

                // Converting list to array
                Dictionary<TreeObject,Matrix4x4[]> detailDictionaryArray = new Dictionary<TreeObject, Matrix4x4[]>();
                Dictionary<TreeObject,Matrix4x4[]> lowDetailDictionaryArray = new Dictionary<TreeObject, Matrix4x4[]>();

                foreach (TreeObject treeObject in ChunkManager.TreeObjects)
                {
                    detailDictionaryArray.Add(treeObject, detailDictionary[treeObject].ToArray());                    
                }

                foreach (TreeObject treeObject in ChunkManager.LowTreeObjects)
                {
                    lowDetailDictionaryArray.Add(treeObject, detailDictionary[treeObject].ToArray());                    
                }

                chunk.DetailDictionary = detailDictionaryArray;
                chunk.LowDetailDictionary = lowDetailDictionaryArray;
            }
            ChunkManager.EnviromentProgress++;
            yield return null;
        }
        Debug.Log("Enviroment Generation Finished");

        //! Constructing chunks


        //*---------------------------------------------------------------------------------------------------

        // Spawn huts



        ChunkManager.ActiveGenerationJob = "Generating oceans";
        //* Water generation inside reachable world
        float size = ChunkManager.ChunkSettings.ChunkSize;
        float worldSize = ChunkManager.WorldSize * size;

        foreach (Vector2 chunk in validWaterChunk)
        {
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.transform.position = new Vector3(chunk.x,0,chunk.y) + new Vector3(size / 2,ChunkManager.waterLevel, + size / 2);
            plane.transform.localScale = Vector3.one / 10 * size;
            plane.GetComponent<MeshRenderer>().material = ChunkManager.WaterMaterial;
        }

        // bordering water
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
                ChunkManager.waterLevel,
                item.z * worldSize * 2
            );
            w1.transform.localScale = Vector3.one / 10 * worldSize*2;
            w1.GetComponent<MeshRenderer>().material = ChunkManager.WaterMaterial;                
        }

        for (int x = -ChunkManager.simulationSettings.WorldSize; x < ChunkManager.simulationSettings.WorldSize; x++)
        {
            for (int y = -ChunkManager.simulationSettings.WorldSize; y < ChunkManager.simulationSettings.WorldSize; y++)
            {
                Vector2 key = new Vector2(x,y);
                ChunkManager.ChunkDictionary[key].UpdateChunk(new Vector3(
                    Mathf.Round(ChunkManager.TrackedObject.position.x / ChunkManager.simulationSettings.WorldSize),
                    0,
                    Mathf.Round(ChunkManager.TrackedObject.position.z / ChunkManager.simulationSettings.WorldSize)));
            }
        }

        ChunkManager.ActiveGenerationJob = "Generating cartographical data";
        
        yield return MapTextureGenerator.GenerateMapTexture(
            ChunkManager.ChunkDictionary,
            ChunkManager.WorldSize,
            ChunkManager.ChunkSettings.ChunkResolution,
            ChunkManager);

        ChunkManager.BatchEnviroment();

        ChunkManager.TerrainMaterial.SetVector("_HeightRange", new Vector2(ChunkManager.TerrainSettings.MinHeight,ChunkManager.TerrainSettings.MaxHeight));
        ChunkManager.MapDisplay.transform.parent.gameObject.SetActive(true);
        ChunkManager.GenerationComplete = true;
        Debug.Log("Chunk Prerender Generation Finished");
        Debug.Log(string.Format("Max : {0} | Min : {1}",ChunkManager.GlobalNoiseHighest,ChunkManager.GlobalNoiseLowest));
        Debug.Log("World generation and prerender corutine complete");
    }


    public static bool DoesntIntersect(float radius, Vector2 position, List<ObjectSizeDescriptor> positions)
    {
        foreach (ObjectSizeDescriptor checkedPosiiton in positions)
        {
            float comparedRadius = (checkedPosiiton.Radius > radius)?checkedPosiiton.Radius:radius;
            if (Vector2.Distance(position,checkedPosiiton.Position) < comparedRadius)
                return false;
        }

        return true;
    }
}

public class NoiseConverter{
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

    public float GetNormalized(float rawValue){
        float range01 = 0 + (rawValue - min1) * (1 - 0) / (high1 - min1);
        return terrainCurve.Evaluate(range01);
        
    }

    public float GetRealHeight(float value){
        float range01 = 0 + (value - min1) * (1 - 0) / (high1 - min1);
        float modHeight = terrainCurve.Evaluate(range01);
        float realHeight = min2 + (modHeight - 0) * (high2 - min2) / (1 - 0);
        return realHeight;
    }
}
