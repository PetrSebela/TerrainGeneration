using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Linq;

public static class GenerationManager
{
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

                // preparing compute shader
                heightMapBuffer.SetData(heightMap);
                ChunkManager.HeightMapShader.SetVector("offset", generatedChunkPosition);
                ChunkManager.HeightMapShader.SetBuffer(0, "heightMap", heightMapBuffer);
                ChunkManager.HeightMapShader.SetBuffer(0, "layerOffsets", offsets);
                
                ChunkManager.HeightMapShader.SetFloat("persistence",ChunkManager.TerrainSettings.Persistence);
                ChunkManager.HeightMapShader.SetFloat("lacunarity",ChunkManager.TerrainSettings.Lacunarity);
                ChunkManager.HeightMapShader.SetInt("octaves",ChunkManager.TerrainSettings.Octaves);
                
                ChunkManager.HeightMapShader.SetFloat("size",ChunkManager.ChunkSettings.ChunkSize);
                ChunkManager.HeightMapShader.SetInt("resolution",heightMapSide);
                ChunkManager.HeightMapShader.SetInts("worldSize", ChunkManager.WorldSize);
                
                ChunkManager.HeightMapShader.Dispatch(0, heightMapSide / 2, heightMapSide / 2, 1);
                heightMapBuffer.GetData(heightMap);

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
            }
            yield return null;
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
                
                Chunk c = ChunkManager.ChunkDictionary[new Vector2(x,y)];
                lock(ChunkManager.MeshRequests){
                    ChunkManager.MeshRequests.Enqueue(new MeshRequest(c.HeightMap,position,c,Vector3.zero));
                }
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
            ChunkManager.terrainCurve
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
                                    Vector3.Angle(Vector3.up, normal) < 35;
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

                lock(ChunkManager.MeshRequests){
                    ChunkManager.MeshRequests.Enqueue(new MeshRequest(
                        ChunkManager.ChunkDictionary[new Vector2(xChunkHut,yChunkHut)].HeightMap,
                        new Vector3(xChunkHut,0,yChunkHut),
                        ChunkManager.ChunkDictionary[new Vector2(xChunkHut,yChunkHut)],
                        Vector3.zero)
                        );
                }
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


                lock(ChunkManager.MeshRequests){
                    ChunkManager.MeshRequests.Enqueue(new MeshRequest(
                        ChunkManager.ChunkDictionary[new Vector2(x,y)].HeightMap,
                        new Vector3(x,0,y),
                        ChunkManager.ChunkDictionary[new Vector2(x,y)],
                        Vector3.zero)
                        );
                }
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

                Dictionary<Spawnable,List<Matrix4x4>> detailDictionary = new Dictionary<Spawnable, List<Matrix4x4>>(){
                    {Spawnable.ConiferTree,new List<Matrix4x4>()},
                    {Spawnable.DeciduousTree,new List<Matrix4x4>()},
                    {Spawnable.Rock,new List<Matrix4x4>()},
                    {Spawnable.Bush,new List<Matrix4x4>()},
                };

                Dictionary<Spawnable,List<Matrix4x4>> lowDetailDictionary = new Dictionary<Spawnable, List<Matrix4x4>>(){
                    {Spawnable.ConiferTree,new List<Matrix4x4>()},
                    {Spawnable.DeciduousTree,new List<Matrix4x4>()},
                };

                foreach (SpawnableSettings item in ChunkManager.spSettings)
                {
                    List<Matrix4x4> listReference = detailDictionary[item.type];
                    for (int i = 0; i < item.countInChunk; i++)
                    {
                        int xTreeCoord = UnityEngine.Random.Range(0, ChunkManager.ChunkSettings.ChunkResolution - 1);
                        int zTreeCoord = UnityEngine.Random.Range(0, ChunkManager.ChunkSettings.ChunkResolution - 1);
                        float height = noiseConverter.GetRealHeight(chunk.HeightMap[xTreeCoord, zTreeCoord]);

                        // Calculate base normal
                        Vector3 p1 = new Vector3(xTreeCoord     , noiseConverter.GetRealHeight(chunk.HeightMap[xTreeCoord      , zTreeCoord    ]), zTreeCoord);
                        Vector3 p2 = new Vector3(xTreeCoord + 1 , noiseConverter.GetRealHeight(chunk.HeightMap[xTreeCoord + 1  , zTreeCoord    ]), zTreeCoord);
                        Vector3 p3 = new Vector3(xTreeCoord     , noiseConverter.GetRealHeight(chunk.HeightMap[xTreeCoord      , zTreeCoord + 1]), zTreeCoord + 1);
                        Vector3 normal = Vector3.Cross(p3 - p1, p2 - p1);

                        bool canSpawnObject = item.minHeight * ChunkManager.TerrainSettings.MaxHeight  < height && 
                                            height < item.maxHeight * ChunkManager.TerrainSettings.MaxHeight && 
                                            Vector3.Angle(Vector3.up, normal) < item.maxSlope && 
                                            height > ChunkManager.waterLevel;
                        if(canSpawnObject)
                        {
                            Vector3 position = new Vector3(
                                (key.x * ChunkManager.ChunkSettings.ChunkSize) + (((float)(xTreeCoord) / ChunkManager.ChunkSettings.ChunkResolution) * ChunkManager.ChunkSettings.ChunkSize),
                                height,
                                (key.y * ChunkManager.ChunkSettings.ChunkSize) + (((float)(zTreeCoord) / ChunkManager.ChunkSettings.ChunkResolution) * ChunkManager.ChunkSettings.ChunkSize));
                            
                            Matrix4x4 matrix4X4 = Matrix4x4.TRS(
                                position, 
                                Quaternion.Euler(new Vector3(0,Random.Range(0,360),0)), 
                                Vector3.one * Random.Range(item.minScale,item.maxScale));

                            listReference.Add(matrix4X4);
                        }
                    }
                }

                // Converting list to array
                Dictionary<Spawnable,Matrix4x4[]> detailDictionaryArray = new Dictionary<Spawnable, Matrix4x4[]>();
                Dictionary<Spawnable,Matrix4x4[]> lowDetailDictionaryArray = new Dictionary<Spawnable, Matrix4x4[]>();

                foreach (var item in detailDictionary.Keys)
                {
                    switch (item)
                    {
                        case Spawnable.ConiferTree:
                            detailDictionaryArray.Add(item, detailDictionary[item].ToArray());
                            lowDetailDictionaryArray.Add(item, detailDictionary[item].ToArray());
                            break;                                
                        
                        case Spawnable.DeciduousTree:
                            detailDictionaryArray.Add(item, detailDictionary[item].ToArray());
                            lowDetailDictionaryArray.Add(item, detailDictionary[item].ToArray());
                            break;                                
                        
                        case Spawnable.Rock:
                            detailDictionaryArray.Add(item, detailDictionary[item].ToArray());
                            break;         

                        case Spawnable.Bush:
                            detailDictionaryArray.Add(item, detailDictionary[item].ToArray());
                            break;                                
                    }
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


        // Filling Tree Dictionary

        for (int x = -5; x < 5; x++)
        {
            for (int y = -5; y < 5; y++)
            {
                Vector2 key = new Vector2(x,y);
                ChunkManager.TreeChunkDictionary.Add(key, ChunkManager.ChunkDictionary[key]);
                ChunkManager.LowDetail.Remove(ChunkManager.ChunkDictionary[key]);
            }
        }

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

        ChunkManager.ActiveGenerationJob = "Generating cartographical data";
        
        yield return MapTextureGenerator.GenerateMapTexture(
            ChunkManager.ChunkDictionary,
            ChunkManager.WorldSize,
            ChunkManager.ChunkSettings.ChunkResolution,
            ChunkManager);

        ChunkManager.TerrainMaterial.SetVector("_HeightRange", new Vector2(ChunkManager.TerrainSettings.MinHeight,ChunkManager.TerrainSettings.MaxHeight));
        ChunkManager.MapDisplay.transform.parent.gameObject.SetActive(true);
        ChunkManager.GenerationComplete = true;
        Debug.Log("Chunk Prerender Generation Finished");
        Debug.Log(string.Format("Max : {0} | Min : {1}",ChunkManager.GlobalNoiseHighest,ChunkManager.GlobalNoiseLowest));
        Debug.Log("World generation and prerender corutine complete");
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
