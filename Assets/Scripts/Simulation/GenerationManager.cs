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
            Vector2 modPosition = (position + chunkManager.SeedGenerator.NoiseLayers[i]) * frequency;
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

        offsets.SetData(ChunkManager.SeedGenerator.NoiseLayers);

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

                        float s1 = SampleNoise(
                            new Vector2(
                                xChunk * ChunkManager.ChunkSettings.ChunkSize + i,
                                yChunk * ChunkManager.ChunkSettings.ChunkSize + j
                            ),
                            ChunkManager.TerrainSettings,
                            ChunkManager
                        );
                        // float s1 = GetFBMSamples(
                        //     new Vector2(
                        //         xChunk * ChunkManager.ChunkSettings.ChunkSize + i,
                        //         yChunk * ChunkManager.ChunkSettings.ChunkSize + j
                        //     )
                        //     ,
                        //     ChunkManager.WrinkleSize
                        // );

                        float s2 = SampleNoise(
                            new Vector2(
                                xChunk * ChunkManager.ChunkSettings.ChunkSize + i + 512.4f,
                                yChunk * ChunkManager.ChunkSettings.ChunkSize + j + 752.4f
                            ),
                            ChunkManager.TerrainSettings,
                            ChunkManager
                        );

                        // float s2 = GetFBMSamples(
                        //     new Vector2(
                        //         xChunk * ChunkManager.ChunkSettings.ChunkSize + i + 512.4f,
                        //         yChunk * ChunkManager.ChunkSettings.ChunkSize + j + 752.4f
                        //     ),
                        //     ChunkManager.WrinkleSize
                        // );

                        heightMap[i,j] = SampleNoise(
                            new Vector2(
                                xChunk * ChunkManager.ChunkSettings.ChunkSize + i + s1 * 15000.1f * ChunkManager.TerrainSettings.WrinkleMagniture,
                                yChunk * ChunkManager.ChunkSettings.ChunkSize + j + s2 * 8020.1f * ChunkManager.TerrainSettings.WrinkleMagniture
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

                        if(checkedValue > ChunkManager.GlobalNoiseHighest){
                            ChunkManager.GlobalNoiseHighest = highestValue;
                            ChunkManager.WorldTopPosition = new Vector3(xChunk * ChunkManager.ChunkSettings.ChunkSize + x,checkedValue,yChunk * ChunkManager.ChunkSettings.ChunkSize + y);
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
        
        Vector3[] dockPosition = new Vector3[ChunkManager.DockCount];
        Vector3[] dockOrientation = new Vector3[ChunkManager.DockCount];

        GameObject g = GameObject.Instantiate(ChunkManager.Cross,new Vector3(ChunkManager.WorldTopPosition.x,noiseConverter.GetRealHeight(ChunkManager.WorldTopPosition.y),ChunkManager.WorldTopPosition.z),Quaternion.identity);

        for (int dockIndex = 0; dockIndex < ChunkManager.DockCount; dockIndex++)
        {
            float angle = (float)(360 / ChunkManager.DockCount) * dockIndex;
            float sampleDistance = 1;
            float pastSample = noiseConverter.GetRealHeight(1);
            Vector2 samplerPositon = Vector2.zero;

            while(Vector2.Distance(Vector2.zero, samplerPositon) <= ChunkManager.WorldSize * ChunkManager.ChunkSettings.ChunkSize){
                   float s1 = GenerationManager.SampleNoise(
                            samplerPositon,
                            ChunkManager.TerrainSettings,
                            ChunkManager
                        );

                float s2 = GenerationManager.SampleNoise(
                    samplerPositon + new Vector2(512.4f,752.4f),
                    ChunkManager.TerrainSettings,
                    ChunkManager
                );

                float distance =  Vector2.Distance(Vector2.zero, samplerPositon) / (ChunkManager.WorldSize  * ChunkManager.ChunkSettings.ChunkResolution);
                float currentSample =  GenerationManager.SampleNoise(
                    new Vector2(
                        samplerPositon.x + s1 * 15000.1f * ChunkManager.TerrainSettings.WrinkleMagniture,
                        samplerPositon.y + s2 * 8020.1f * ChunkManager.TerrainSettings.WrinkleMagniture
                    ), 
                    ChunkManager.TerrainSettings, 
                    ChunkManager);
                
                
                currentSample *= ChunkManager.TerrainFalloffCurve.Evaluate(distance);
                currentSample = noiseConverter.GetRealHeight(currentSample);

                if (pastSample >= 0 && currentSample < 0){
                    dockPosition[dockIndex] = new Vector3(samplerPositon.x,0,samplerPositon.y);
                    dockOrientation[dockIndex] = new Vector3(0, -angle - 90, 0);
                }
                
                
                pastSample = currentSample;
                
                samplerPositon += new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * sampleDistance,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * sampleDistance
                );
            }

            if(dockPosition[dockIndex] == null)
                continue;

            GameObject dock = GameObject.Instantiate(
                ChunkManager.DockObject,
                dockPosition[dockIndex],
                Quaternion.Euler(dockOrientation[dockIndex]));
            dock.transform.name = angle.ToString();
            
            ChunkManager.StructureSizeDescriptorList.Add(new ObjectSizeDescriptor(10,dockPosition[dockIndex]));
        }
        

        if (ChunkManager.SetViewerPositionFromScript)
        {
            int dockIndex = UnityEngine.Random.Range(0,ChunkManager.DockCount-1);
            ChunkManager.TrackedObject.position = dockPosition[dockIndex] + new Vector3(0,5,0);
            ChunkManager.PlayerController.cameraRotation = dockOrientation[dockIndex];
        }


        foreach (StructureObject structureObject in ChunkManager.StructureObjects)
        {
            int objectCount = 0;
            int iterations = 0;

            while (objectCount <= structureObject.Count)
            {                           
                if (iterations++ >= structureObject.Count * 2){
                    break;
                }
                
                int xChunk = Random.Range(-ChunkManager.WorldSize,ChunkManager.WorldSize);
                int yChunk = Random.Range(-ChunkManager.WorldSize,ChunkManager.WorldSize);

                int xInChunk = (int)Random.Range(structureObject.Radius,ChunkManager.ChunkSettings.ChunkResolution - structureObject.Radius);
                int yInChunk = (int)Random.Range(structureObject.Radius,ChunkManager.ChunkSettings.ChunkResolution - structureObject.Radius);
                float[,] heightMap = ChunkManager.ChunkDictionary[new Vector2(xChunk,yChunk)].HeightMap;
                
                Vector3 p1 = new Vector3(xInChunk     , noiseConverter.GetRealHeight(heightMap[xInChunk      , yInChunk    ]), yInChunk);
                Vector3 p2 = new Vector3(xInChunk + 1 , noiseConverter.GetRealHeight(heightMap[xInChunk + 1  , yInChunk    ]), yInChunk);
                Vector3 p3 = new Vector3(xInChunk     , noiseConverter.GetRealHeight(heightMap[xInChunk      , yInChunk + 1]), yInChunk + 1);
                Vector3 normal = Vector3.Cross(p3 - p1, p2 - p1);

                Vector2 globalPosition2D = new Vector2(xChunk,yChunk) * ChunkManager.ChunkSettings.ChunkSize + new Vector2(xInChunk,yInChunk);
                
                float candidateHeight = heightMap[xInChunk,yInChunk];

                bool canSpawnCandidate =    noiseConverter.GetRealHeight(candidateHeight) > 0 && 
                                            Vector3.Angle(Vector3.up, normal) < structureObject.SlopeLimit &&
                                            DoesntIntersect(structureObject.MinDistanceFromStructures,globalPosition2D,ChunkManager.StructureSizeDescriptorList);

                if (!canSpawnCandidate)
                {
                    // Debug.Log("ditching candidate");
                    continue;
                }

                objectCount++;
                Chunk chunk = ChunkManager.ChunkDictionary[new Vector2(xChunk,yChunk)];
                ChunkManager.StructureSizeDescriptorList.Add(new ObjectSizeDescriptor(structureObject.MinDistanceFromStructures,globalPosition2D));
                chunk.FoliegeSizeDescriptorList.Add(new ObjectSizeDescriptor(structureObject.Radius,new Vector2(xInChunk,yInChunk)));
                
                terrainModifier.LevelTerrain(
                    new Vector2Int(
                        xChunk,
                        yChunk
                    ),
                    new Vector2Int(
                        xInChunk - (int)structureObject.Radius,
                        yInChunk - (int)structureObject.Radius
                    ),
                    new Vector2Int(
                        (int)structureObject.Radius*2,
                        (int)structureObject.Radius*2
                    ),
                    candidateHeight
                );

                Vector3 generatedPosition = new Vector3(
                    xChunk* ChunkManager.ChunkSettings.ChunkSize + xInChunk,
                    noiseConverter.GetRealHeight(candidateHeight),
                    yChunk* ChunkManager.ChunkSettings.ChunkSize + yInChunk);

                GameObject candidateInstance = GameObject.Instantiate(
                    structureObject.Structure,
                    generatedPosition,
                    Quaternion.identity);

                candidateInstance.transform.parent = chunk.MeshRenderer.transform; 
                chunk.ChildStructures.Add(candidateInstance);
            }
        }


        //! Spawning trees
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

                int objectIndex = 0;
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
                        
                        int xTreeCoord = (int)UnityEngine.Random.Range(0, ChunkManager.ChunkSettings.ChunkResolution);
                        int zTreeCoord = (int)UnityEngine.Random.Range(0, ChunkManager.ChunkSettings.ChunkResolution);
                        float height = noiseConverter.GetRealHeight(chunk.HeightMap[xTreeCoord, zTreeCoord]);

                        // Calculate base normal
                        Vector3 p1 = new Vector3(xTreeCoord     , noiseConverter.GetRealHeight(chunk.HeightMap[xTreeCoord      , zTreeCoord    ]), zTreeCoord);
                        Vector3 p2 = new Vector3(xTreeCoord + 1 , noiseConverter.GetRealHeight(chunk.HeightMap[xTreeCoord + 1  , zTreeCoord    ]), zTreeCoord);
                        Vector3 p3 = new Vector3(xTreeCoord     , noiseConverter.GetRealHeight(chunk.HeightMap[xTreeCoord      , zTreeCoord + 1]), zTreeCoord + 1);
                        Vector3 normal = Vector3.Cross(p3 - p1, p2 - p1);

                        float temperature = Mathf.PerlinNoise(
                            (xChunk * ChunkManager.ChunkSettings.ChunkSize + xTreeCoord + ChunkManager.SeedGenerator.EntityTemperatureMapOffsets[objectIndex].x) * 0.0045f * ChunkManager.ForestSize,                
                            (yChunk * ChunkManager.ChunkSettings.ChunkSize + zTreeCoord + ChunkManager.SeedGenerator.EntityTemperatureMapOffsets[objectIndex].y) * 0.0045f * ChunkManager.ForestSize                
                        );

                        float humidity = Mathf.PerlinNoise(
                            (xChunk * ChunkManager.ChunkSettings.ChunkSize + xTreeCoord + ChunkManager.SeedGenerator.EntityHumidityMapOffsets[objectIndex].x) * 0.0045f *ChunkManager.ForestSize,                
                            (yChunk * ChunkManager.ChunkSettings.ChunkSize + zTreeCoord + ChunkManager.SeedGenerator.EntityHumidityMapOffsets[objectIndex].y) * 0.0045f * ChunkManager.ForestSize               
                        );


                        float floatProbabilityModifier = (temperature + humidity) / 2f;

                        float randomSaple = (float)Random.Range(0,100)/100f;

                        bool f = floatProbabilityModifier > randomSaple;

                        bool canSpawnObject = item.SpawnRange.Min * ChunkManager.TerrainSettings.MaxHeight  < height && 
                                            height < item.SpawnRange.Max * ChunkManager.TerrainSettings.MaxHeight && 
                                            Vector3.Angle(Vector3.up, normal) < item.SlopeLimit && 
                                            height > ChunkManager.waterLevel &&
                                            item.TemperatureRange.Min < temperature && item.TemperatureRange.Max > temperature &&
                                            item.HumidityRange.Min < humidity && item.HumidityRange.Max > humidity &&
                                            DoesntIntersect(item.Radius, new Vector2(xTreeCoord,zTreeCoord),chunk.FoliegeSizeDescriptorList) && f;

                        if(canSpawnObject)
                        {
                            chunk.FoliegeSizeDescriptorList.Add(new ObjectSizeDescriptor(item.Radius, new Vector2(xTreeCoord,zTreeCoord)));
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
                    objectIndex++;
                }

                // Converting list to array

                Dictionary<TreeObject,Matrix4x4[]> detailDictionaryArray = new Dictionary<TreeObject, Matrix4x4[]>();
                foreach (TreeObject treeObject in ChunkManager.TreeObjects)
                    detailDictionaryArray.Add(treeObject, detailDictionary[treeObject].ToArray());                    

                Dictionary<TreeObject,Matrix4x4[]> lowDetailDictionaryArray = new Dictionary<TreeObject, Matrix4x4[]>();
                foreach (TreeObject treeObject in ChunkManager.LowTreeObjects)
                    lowDetailDictionaryArray.Add(treeObject, detailDictionary[treeObject].ToArray());                    

                chunk.DetailDictionary = detailDictionaryArray;
                chunk.LowDetailDictionary = lowDetailDictionaryArray;
            }
            ChunkManager.EnviromentProgress++;
            yield return null;
        }


        Debug.Log("Enviroment Generation Finished");

        //! Constructing chunks


        //*---------------------------------------------------------------------------------------------------


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
    public double GetRealHeight(double value){
        double range01 = 0 + (value - min1) * (1 - 0) / (high1 - min1);
        double modHeight = terrainCurve.Evaluate((float)range01);
        double realHeight = min2 + (modHeight - 0) * (high2 - min2) / (1 - 0);
        return realHeight;
    }
}
