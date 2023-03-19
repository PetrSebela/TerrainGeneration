using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerationManager
{
    private ChunkManager ChunkManager;
    private NoiseConverter noiseConverter;
    private TerrainModifier terrainModifier;
    private Vector3[] dockPosition;
    private Vector3[] dockOrientation;

    public GenerationManager(ChunkManager chunkManager){
        ChunkManager = chunkManager;
    }
    public float SampleNoise(Vector2 position, TerrainSettings terrainSettings, ChunkManager chunkManager)
    {
        float sampleValue = 0;
        float frequency = 0.002f;
        float amplitude = 1;

        for (int i = 0; i < terrainSettings.Octaves; i++)
        {
            Vector2 modPosition = (position + chunkManager.SeedGenerator.NoiseLayers[i]) * frequency;
            sampleValue += Mathf.PerlinNoise(modPosition.x, modPosition.y) * amplitude;

            amplitude *= terrainSettings.Persistence;
            frequency *= terrainSettings.Lacunarity;
        }
        
        return sampleValue / terrainSettings.Octaves;
    }

    public float SampleFBMNoise(Vector2 position){
        float sample1 = SampleNoise(
            position,
            ChunkManager.TerrainSettings,
            ChunkManager
        );

        float sample2 = SampleNoise(
            position + new Vector2(512.4f,752.2f),
            ChunkManager.TerrainSettings,
            ChunkManager
        );

        return SampleNoise(
            position + new Vector2(sample1 * 15023.21f, sample2* 8021.23f) * ChunkManager.TerrainSettings.WrinkleMagniture,
            ChunkManager.TerrainSettings,
            ChunkManager
        );
    }

    public float SampleNoiseWithFalloffMap(Vector2 position){
        float distanceFromIslandOrigin = Vector2.Distance(position,Vector2.zero);
        float normalizedDistance = distanceFromIslandOrigin / (ChunkManager.WorldSize  * ChunkManager.ChunkSettings.ChunkResolution);
        return SampleFBMNoise(position) *  ChunkManager.TerrainFalloffCurve.Evaluate(normalizedDistance);
    }

    public void GenerateDock(float angle,int dockIndex){
        float pastNoiseSample = noiseConverter.GetRealHeight(1);
        Vector2 samplerPositon = Vector2.zero;

        // finding furthest point that dips into water
        while(Vector2.Distance(Vector2.zero, samplerPositon) <= ChunkManager.WorldSize * ChunkManager.ChunkSettings.ChunkSize){
            float currentNoiseSample = SampleNoiseWithFalloffMap(samplerPositon);
            
            currentNoiseSample = noiseConverter.GetRealHeight(currentNoiseSample);

            bool dipUnderWater = pastNoiseSample >= 0 && currentNoiseSample < 0; 

            if (dipUnderWater){
                dockPosition[dockIndex] = new Vector3(samplerPositon.x,0,samplerPositon.y);
                dockOrientation[dockIndex] = new Vector3(0, -angle - 90, 0);
            }                
            
            pastNoiseSample = currentNoiseSample;
            
            samplerPositon += new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );
        }

        if(dockPosition[dockIndex] == null)
            return;

        GameObject dock = GameObject.Instantiate(
            ChunkManager.DockObject,
            dockPosition[dockIndex],
            Quaternion.Euler(dockOrientation[dockIndex]));

        dock.transform.name = angle.ToString();
        dock.transform.parent = ChunkManager.transform;

        ChunkManager.StructureSizeDescriptorList.Add(new ObjectSizeDescriptor(10,dockPosition[dockIndex]));
    }

    public bool ValidateCandidate(StructureObject structureObject, int xInChunk, int yInChunk, int xChunk, int yChunk){
         float[,] heightMap = ChunkManager.ChunkDictionary[new Vector2(xChunk,yChunk)].HeightMap;
                
        Vector3 p1 = new Vector3(xInChunk     , noiseConverter.GetRealHeight(heightMap[xInChunk      , yInChunk    ]), yInChunk);
        Vector3 p2 = new Vector3(xInChunk + 1 , noiseConverter.GetRealHeight(heightMap[xInChunk + 1  , yInChunk    ]), yInChunk);
        Vector3 p3 = new Vector3(xInChunk     , noiseConverter.GetRealHeight(heightMap[xInChunk      , yInChunk + 1]), yInChunk + 1);
        Vector3 normal = Vector3.Cross(p3 - p1, p2 - p1);

        Vector2 globalPosition2D = new Vector2(xChunk,yChunk) * ChunkManager.ChunkSettings.ChunkSize + new Vector2(xInChunk,yInChunk);
        
        float candidateHeight = heightMap[xInChunk,yInChunk];

        return  noiseConverter.GetRealHeight(candidateHeight) > 0 
                && Vector3.Angle(Vector3.up, normal) < structureObject.SlopeLimit
                && DoesntIntersect(structureObject.MinDistanceFromStructures,globalPosition2D,ChunkManager.StructureSizeDescriptorList);
    }

    public bool ValidRegion(float temperature, float humidity, TreeObject treeDescriptor){
        return  treeDescriptor.TemperatureRange.Min     < temperature 
                && treeDescriptor.TemperatureRange.Max  > temperature 
                && treeDescriptor.HumidityRange.Min     < humidity 
                && treeDescriptor.HumidityRange.Max     > humidity;
    }

    public void InstantiateChunk(Vector3 position){
        GameObject chunk = new GameObject();
        chunk.layer = LayerMask.NameToLayer("Ground");
        chunk.isStatic = true;
        chunk.transform.parent = ChunkManager.transform;
        chunk.transform.position =  position * ChunkManager.ChunkSettings.ChunkSize;
        chunk.transform.name = position.ToString();

        Chunk chunkObject = ChunkManager.ChunkDictionary[new Vector2(position.x,position.z)];

        MeshFilter meshFilter = chunk.AddComponent<MeshFilter>();
        MeshCollider meshCollider = chunk.AddComponent<MeshCollider>();
        MeshRenderer meshRenderer = chunk.AddComponent<MeshRenderer>();
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
        meshRenderer.material = ChunkManager.TerrainMaterial;
        
        chunkObject.MeshFilter = meshFilter;
        chunkObject.MeshRenderer = meshRenderer;
        chunkObject.MeshCollider = meshCollider;
    }

    public IEnumerator GenerationCorutine()
    {
        Debug.Log("Starting generation corutine");
        ChunkManager.ActiveGenerationJob = "Generating heightmap";

        List<Vector2> validWaterChunk = new List<Vector2>();

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
                        float noiseSample = SampleNoiseWithFalloffMap(generatedChunkPosition * ChunkManager.ChunkSettings.ChunkSize + new Vector2(i,j));
                        heightMap[i,j] = noiseSample; 

                        lowestValue = (noiseSample < lowestValue)? noiseSample : lowestValue;
                        highestValue = (noiseSample > highestValue)? noiseSample : highestValue;

                        ChunkManager.GlobalNoiseLowest = (lowestValue < ChunkManager.GlobalNoiseLowest)? lowestValue : ChunkManager.GlobalNoiseLowest;
                        if(noiseSample > ChunkManager.GlobalNoiseHighest){
                            ChunkManager.GlobalNoiseHighest = highestValue;
                            ChunkManager.WorldTopPosition = new Vector3(xChunk * ChunkManager.ChunkSettings.ChunkSize + i,noiseSample,yChunk * ChunkManager.ChunkSettings.ChunkSize + j);
                        } 
                    }
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

        Debug.Log("HeightMap Generation Finished");
        ChunkManager.ActiveGenerationJob = "Generating chunks";

        for (int x = -ChunkManager.WorldSize; x < ChunkManager.WorldSize; x++)
        {
            for (int y = -ChunkManager.WorldSize; y < ChunkManager.WorldSize; y++)
            {
                Vector3 position = new Vector3(x,0,y);

                InstantiateChunk(position);
            }
        }


        //*--- Generating enviroment ------------------------------------------------------------------------------------------------
        Debug.Log("Generating enviroment detail");
        ChunkManager.ActiveGenerationJob = "Generating enviroment detail";

        //! Enviromental detail
        noiseConverter = new NoiseConverter(
            ChunkManager.GlobalNoiseLowest,
            ChunkManager.GlobalNoiseHighest,
            ChunkManager.TerrainSettings.MinHeight,
            ChunkManager.TerrainSettings.MaxHeight,
            ChunkManager.TerrainCurve
        );

        terrainModifier = new TerrainModifier(ChunkManager,noiseConverter);
       

        // Generating cross on highest point
        GameObject.Instantiate(ChunkManager.Cross,new Vector3(ChunkManager.WorldTopPosition.x,noiseConverter.GetRealHeight(ChunkManager.WorldTopPosition.y),ChunkManager.WorldTopPosition.z),Quaternion.identity);

        // Generating docks
        dockPosition = new Vector3[ChunkManager.DockCount];
        dockOrientation = new Vector3[ChunkManager.DockCount];

        for (int dockIndex = 0; dockIndex < ChunkManager.DockCount; dockIndex++)
        {
            float angle = (float)(360 / ChunkManager.DockCount) * dockIndex;
            GenerateDock(angle,dockIndex);
        }
        

        if (ChunkManager.SetViewerPositionFromScript)
        {
            int dockIndex = UnityEngine.Random.Range(0,ChunkManager.DockCount-1);
            ChunkManager.TrackedObject.position = dockPosition[dockIndex] + new Vector3(0,5,0);
            ChunkManager.PlayerController.CameraRotation = dockOrientation[dockIndex];
        }


        foreach (StructureObject structureObject in ChunkManager.StructureObjects)
        {
            int objectCount = 0;
            int placementAttempts = 0;

            while (objectCount <= structureObject.Count)
            {                           
                if (placementAttempts++ >= structureObject.Count * 4)
                    break;
                
                int xChunk = Random.Range( -ChunkManager.WorldSize, ChunkManager.WorldSize);
                int yChunk = Random.Range( -ChunkManager.WorldSize, ChunkManager.WorldSize);

                int xInChunk = (int)Random.Range( structureObject.Radius, ChunkManager.ChunkSettings.ChunkResolution - structureObject.Radius);
                int yInChunk = (int)Random.Range( structureObject.Radius, ChunkManager.ChunkSettings.ChunkResolution - structureObject.Radius);
                
                float[,] heightMap = ChunkManager.ChunkDictionary[ new Vector2( xChunk, yChunk )].HeightMap;
                float candidateHeight = heightMap[ xInChunk, yInChunk ];
                Vector2 globalPosition2D = new Vector2( xChunk, yChunk ) * ChunkManager.ChunkSettings.ChunkSize + new Vector2( xInChunk, yInChunk );
                bool validCandidate = ValidateCandidate( structureObject, xInChunk, yInChunk, xChunk, yChunk );

                if ( !validCandidate )
                    continue;

                objectCount++;

                Chunk chunk = ChunkManager.ChunkDictionary[ new Vector2( xChunk, yChunk )];
                
                ChunkManager.StructureSizeDescriptorList.Add( 
                    new ObjectSizeDescriptor( structureObject.MinDistanceFromStructures, globalPosition2D )
                );
                
                chunk.FoliegeSizeDescriptorList.Add( 
                    new ObjectSizeDescriptor( structureObject.Radius, new Vector2( xInChunk, yInChunk ))
                );
                
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
                    xChunk * ChunkManager.ChunkSettings.ChunkSize + xInChunk,
                    noiseConverter.GetRealHeight(candidateHeight),
                    yChunk * ChunkManager.ChunkSettings.ChunkSize + yInChunk);

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
                foreach (TreeObject treeDescriptor in ChunkManager.TreeObjects)
                {
                    List<Matrix4x4> listReference = detailDictionary[treeDescriptor];
                    List<Vector2> nodePositions = new List<Vector2>();

                    int objectCount = 0;
                    int iterations = 0;
                    while (objectCount <= treeDescriptor.Count)
                    {
                        if (iterations++ >= treeDescriptor.Count * 2){
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

                        bool canSpawnObject =   treeDescriptor.SpawnRange.Min * ChunkManager.TerrainSettings.MaxHeight  < height 
                                                && height < treeDescriptor.SpawnRange.Max * ChunkManager.TerrainSettings.MaxHeight  
                                                && Vector3.Angle(Vector3.up, normal) < treeDescriptor.SlopeLimit  
                                                && height > ChunkManager.waterLevel 
                                                && ValidRegion(temperature, humidity, treeDescriptor)
                                                && DoesntIntersect(treeDescriptor.Radius, new Vector2(xTreeCoord,zTreeCoord),chunk.FoliegeSizeDescriptorList);

                        if(canSpawnObject)
                        {
                            chunk.FoliegeSizeDescriptorList.Add(new ObjectSizeDescriptor(treeDescriptor.Radius, new Vector2(xTreeCoord,zTreeCoord)));
                            Vector3 position = new Vector3(
                                (key.x * ChunkManager.ChunkSettings.ChunkSize) + (((float)(xTreeCoord) / ChunkManager.ChunkSettings.ChunkResolution) * ChunkManager.ChunkSettings.ChunkSize),
                                height,
                                (key.y * ChunkManager.ChunkSettings.ChunkSize) + (((float)(zTreeCoord) / ChunkManager.ChunkSettings.ChunkResolution) * ChunkManager.ChunkSettings.ChunkSize));
                            
                            Matrix4x4 matrix4X4 = Matrix4x4.TRS(
                                position, 
                                Quaternion.Euler(new Vector3(0,Random.Range(0,360),0)), 
                                new Vector3(
                                    treeDescriptor.BaseSize.x + Random.Range( 1 - treeDescriptor.SizeVariation.x, 1 + treeDescriptor.SizeVariation.x),
                                    treeDescriptor.BaseSize.y + Random.Range( 1 - treeDescriptor.SizeVariation.y, 1 + treeDescriptor.SizeVariation.y),
                                    treeDescriptor.BaseSize.z + Random.Range( 1 - treeDescriptor.SizeVariation.z, 1 + treeDescriptor.SizeVariation.z)
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
        ChunkManager.ActiveGenerationJob = "Generating ocean";

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

        // Finishing procedure
        Debug.Log("Finishing coruting");
        ChunkManager.BatchEnviroment();
        ChunkManager.TerrainMaterial.SetVector("_HeightRange", new Vector2(ChunkManager.TerrainSettings.MinHeight,ChunkManager.TerrainSettings.MaxHeight));
        ChunkManager.MapDisplay.transform.parent.gameObject.SetActive(true);
        ChunkManager.GenerationComplete = true;
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
