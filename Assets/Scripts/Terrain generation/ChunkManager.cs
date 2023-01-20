using UnityEngine;
using System;
using Unity.Mathematics;
using System.Collections.Generic;
using UnityEngine.UI;

public class ChunkManager : MonoBehaviour
{
    [Header("Simulation setting")]
    public ChunkSettings ChunkSettings;
    [SerializeField] public int WorldSize;
    [SerializeField] public int LODtreeBorder;
    [SerializeField] private Transform Tracker;
    public float MaxTerrainHeight;

    [Header("Enviroment")]
    [SerializeField] public List<SpawnableSettings> spSettings = new List<SpawnableSettings>();
    [SerializeField] private Mesh TreeMesh;
    [SerializeField] private Mesh TreeMesh2;
    [SerializeField] private Mesh RockMesh;
    [SerializeField] private Mesh BushMesh;
    [SerializeField] private Mesh LowDetailBase;

    
    [Header("Materials")]
    [SerializeField] private Material CrownMaterial;
    [SerializeField] private Material BarkMaterial;

    [SerializeField] private Material RockMaterial;

    [SerializeField] private Material BushMaterial;
    [SerializeField] public Material TerrainMaterial;

    [SerializeField] private Material LowDetailMaterialBase;


    [Header("Water settings")]
    public bool UseWater = true;
    public Material WaterMaterial;
    public float waterLevel;

    
    [Header("Exposed variables")]
    // public Dictionary<Vector2, float[,]> HeightMapDict = new Dictionary<Vector2, float[,]>();
    public Dictionary<Vector2, Chunk> ChunkDictionary = new Dictionary<Vector2, Chunk>();
    public Dictionary<Vector2, Mesh> MeshDictionary = new Dictionary<Vector2, Mesh>();
    public Dictionary<Vector3, GameObject> ChunkObjectDictionary = new Dictionary<Vector3, GameObject>();
    public Dictionary<Vector2, Chunk> TreeChunkDictionary = new Dictionary<Vector2, Chunk>();
    // Queues
    public Queue<Vector2> ChunkUpdateRequestQueue = new Queue<Vector2>();
    public Queue<ChunkUpdate> MeshQueue = new Queue<ChunkUpdate>();


    public Vector2 PastChunkPosition = Vector2.zero;
    public bool GenerationComplete = false;
    public bool FullRender = false;
    public bool RenderEnviroment = true;
    public bool DrawChunkBorders = false;

    [SerializeField] public ComputeShader HeightMapShader;
    public SeedGenerator SeedGenerator;
    public int NumOfPeaks;
    public Vector3[] Peaks;
    public Vector2[] PeaksPOI;
    public GameObject HighestPointMonument;
    public GameObject Monument;
    public SimulationSettings simulationSettings;
    public float globalNoiseLowest = Mathf.Infinity;
    public float globalNoiseHighest = -Mathf.Infinity;
    public int enviromentProgress = 0;
    public float Progress = 0;

    public Dictionary<Spawnable,List<List<Matrix4x4>>> FullTreeList = new Dictionary<Spawnable,List<List<Matrix4x4>>>(){
        {Spawnable.ConiferTree,new List<List<Matrix4x4>>()},
        {Spawnable.DeciduousTree,new List<List<Matrix4x4>>()},
        {Spawnable.Rock,new List<List<Matrix4x4>>()},
        {Spawnable.Bush,new List<List<Matrix4x4>>()},
    };


    [Header("LowDetailModels")]
    public Material baseMaterial;
    public List<Chunk> LowDetail = new List<Chunk>();
    public Texture2D[] Texture2DList;
    public Material[] materials;    

    public Texture2D terrainTexture;
    public float hardness;
    [Range(0,1)]
    public float offset;

    [Range(0,1)]
    public float gradStart;
    
    [Range(0,1)]
    public float gradEnd;
    public AnimationCurve terrainCurve;


    //! chunk batching
    Mesh[] combinesMeshes = new Mesh[0];

    // Vector3 -> Vector2
    // z -> y
    // V3(x,y,z) -> V2(x,z)

    void Start()
    {
        //! Mapping low res textures to models
        materials = new Material[Texture2DList.Length];
        for (int i = 0; i < Texture2DList.Length; i++)
        {
            Material mat = new Material(baseMaterial);
            mat.SetTexture("_BaseMap",Texture2DList[i]);
            materials[i] = mat;
        }

        TerrainMaterial.SetTexture("_Texture2D",TextureCreator.GenerateTexture());

        //! Simulation setup
        MaxTerrainHeight = (simulationSettings.maxHeight == 0)? MaxTerrainHeight : simulationSettings.maxHeight;
        WorldSize = simulationSettings.worldSize;
        string seed = simulationSettings.seed;
        GenerateWorld(seed);
    }

    void GenerateWorld(string seed){
        int seedInt;
        if(int.TryParse(simulationSettings.seed, out seedInt)){
            SeedGenerator = new SeedGenerator(seedInt);
        }
        else{
            SeedGenerator = new SeedGenerator(seed);
        }

        Tracker.position = new Vector3(0, MaxTerrainHeight, 0);
        
        Peaks = new Vector3[NumOfPeaks];
        PeaksPOI = new Vector2[NumOfPeaks];

        for (int i = 0; i < NumOfPeaks; i++)
        {
            float angle = 360 / NumOfPeaks * i;
            float x = math.cos(angle) * (WorldSize / 2 * ChunkSettings.size);
            float y = math.sin(angle) * (WorldSize / 2 * ChunkSettings.size);
            PeaksPOI[i] = new Vector2(x,y);
        }
        StartCoroutine(GenerationManager.GenerationCorutine(this)); 
    }

    void Update()
    {          
        // generating chunk update requests
        if (!GenerationComplete){
            float worldChunkArea = math.pow(WorldSize * 2, 2);
            Progress = ((float)ChunkDictionary.Count /  worldChunkArea  + 
                        (float)enviromentProgress / (WorldSize * 2)) / 2;
            return;
        }
        
        // Selecting which chunks to update
        Vector2 currentChunkPosition = new Vector2(Mathf.Round(Tracker.position.x / ChunkSettings.size), Mathf.Round(Tracker.position.z / ChunkSettings.size));
        if (currentChunkPosition != PastChunkPosition)
        {
            PastChunkPosition = currentChunkPosition;
            for (int x = -34; x <= 34; x++)
            {
                for (int y = -34; y <= 34; y++)
                {
                    Vector2 sampler = currentChunkPosition + new Vector2(x, y);
                    if (sampler.x >= -WorldSize && sampler.x < WorldSize && sampler.y >= -WorldSize && sampler.y < WorldSize)
                        lock (ChunkUpdateRequestQueue)
                            ChunkUpdateRequestQueue.Enqueue(sampler);
                }
            }
        }


        // Updating and redrawing chunks
        int hold = MeshQueue.Count;
        for (int f = 0; f < hold; f++)
        {
            ChunkUpdate updateMeshData;
            lock (MeshQueue)
                updateMeshData = MeshQueue.Dequeue();
            UpdateChunk(updateMeshData.meshData, updateMeshData.LODindex);
        }      

        Dictionary<Vector2,Chunk> activeDictionary = (FullRender)? ChunkDictionary : TreeChunkDictionary;

        // Rendering enviromental details
        // foreach (Chunk chunkInstance in activeDictionary.Values)
        // {   
        //     foreach (var spawnableType in chunkInstance.detailDictionary.Keys)
        //     {
        //         Matrix4x4[] detailArray = chunkInstance.detailDictionary[spawnableType];

        //         switch (spawnableType)
        //         {
        //             case Spawnable.ConiferTree:
        //                 Graphics.DrawMeshInstanced(TreeMesh2, 1, BarkMaterial, detailArray);
        //                 Graphics.DrawMeshInstanced(TreeMesh2, 0, BushMaterial, detailArray);
        //                 break;

        //             case Spawnable.DeciduousTree:
        //                 Graphics.DrawMeshInstanced(TreeMesh, 0, BarkMaterial, detailArray);
        //                 Graphics.DrawMeshInstanced(TreeMesh, 1, CrownMaterial, detailArray);
        //                 break;

        //             case Spawnable.Rock:
        //                 Graphics.DrawMeshInstanced(RockMesh, 0, RockMaterial, detailArray);
        //                 break;

        //             case Spawnable.Bush:
        //                 Graphics.DrawMeshInstanced(BushMesh, 0, BushMaterial, detailArray);
        //                 Graphics.DrawMeshInstanced(BushMesh, 1, BarkMaterial, detailArray);
        //                 break;                           

        //             default:
        //                 break;
        //         }
        //     }
        // }

        // //! Updating enviroment batches
        // Dictionary<Spawnable,List<List<Matrix4x4>>> batches = new Dictionary<Spawnable, List<List<Matrix4x4>>>(){
        //     {Spawnable.ConiferTree,new List<List<Matrix4x4>>()},
        //     {Spawnable.DeciduousTree,new List<List<Matrix4x4>>()},
        // };

        // Dictionary<Spawnable,int> batchCounter = new Dictionary<Spawnable, int>(){
        //     {Spawnable.ConiferTree,0},
        //     {Spawnable.DeciduousTree,0},
        // };

        // foreach (Chunk chunk in LowDetail)
        // {
        //     foreach(Spawnable type in new Spawnable[]{Spawnable.ConiferTree,Spawnable.DeciduousTree}){
                
        //         foreach (Matrix4x4 item in chunk.detailDictionary[type])
        //         {
        //             if(batchCounter[type] % 1023 == 0){
        //                 batches[type].Add(new List<Matrix4x4>());
        //             }

        //             batches[type][(int)(batchCounter[type] / 1023)].Add(item);
        //             batchCounter[type]++;
        //         }
        //     }
        // }
        
        // foreach (Spawnable spawnableType in batches.Keys)
        // {   
        //     foreach (List<Matrix4x4> envList in batches[spawnableType])
        //     {
        //         switch (spawnableType)
        //         {
        //             case Spawnable.ConiferTree:
        //                 Graphics.DrawMeshInstanced(LowDetailBase, 0, materials[0], envList);                 
        //                 Graphics.DrawMeshInstanced(LowDetailBase, 1, materials[1], envList);                 
        //                 break;

        //             case Spawnable.DeciduousTree:
        //                 Graphics.DrawMeshInstanced(LowDetailBase, 0, materials[2], envList);                 
        //                 Graphics.DrawMeshInstanced(LowDetailBase, 1, materials[3], envList);
        //                 break;

        //             default:
        //                 break;
        //         }
        //     }
        // }

        foreach (Mesh mesh in combinesMeshes)
        {
            Graphics.DrawMesh(mesh,Vector3.zero,Quaternion.identity, TerrainMaterial, 0);
        }

        if(hold == 0)
            return;        
        BatchMeshes();
    }

    public void BatchMeshes(){
        List<List<CombineInstance>> mergeList = new List<List<CombineInstance>>(){new List<CombineInstance>()}; 
        int vertCounter = 0;
        int meshIndex = 0;

        for (int x = -WorldSize; x < WorldSize; x++)
        {
            for (int z = -WorldSize; z < WorldSize; z++)
            {
                Mesh mesh = MeshDictionary[new Vector2(x,z)];
                // Debug.Log((new Vector2(x,z) * ChunkSettings.size).ToString() + " - " + mesh.vertexCount);

                if(vertCounter + mesh.vertexCount < 10000){
                    CombineInstance instance = new CombineInstance();
                    instance.mesh = mesh;
                    instance.transform = Matrix4x4.TRS(new Vector3(x,0,z) * ChunkSettings.size,Quaternion.identity,Vector3.one);
                    mergeList[meshIndex].Add(instance);
                }
                else{
                    mergeList.Add( new List<CombineInstance>() );
                    meshIndex++;    
                    vertCounter = 0;                

                    CombineInstance instance = new CombineInstance();
                    instance.mesh = mesh;
                    instance.transform = Matrix4x4.TRS(new Vector3(x,0,z) * ChunkSettings.size,Quaternion.identity,Vector3.one);
                    
                    mergeList[meshIndex].Add(instance);
                }

                vertCounter += mesh.vertexCount;
            }
        }

        combinesMeshes = new Mesh[mergeList.Count];

        for (int i = 0; i < mergeList.Count; i++)
        {
            Mesh mesh = new Mesh();
            mesh.CombineMeshes(mergeList[i].ToArray(),true,true);
            combinesMeshes[i] = mesh;
            // Debug.Log(combinesMeshes[i].vertexCount);
        }  
    }

    void UpdateChunk(MeshData meshData, int LODindex)
    {
        // calculating tree topology
        Vector2 key = new Vector2(meshData.position.x, meshData.position.z);
        if (LODindex <= LODtreeBorder && !TreeChunkDictionary.ContainsKey(key))
        {
            TreeChunkDictionary.Add(key, ChunkDictionary[key]);
            LowDetail.Remove(ChunkDictionary[key]);
        }
        else if (LODindex > LODtreeBorder && TreeChunkDictionary.ContainsKey(key))
        {
            TreeChunkDictionary.Remove(key);
            LowDetail.Add(ChunkDictionary[key]);
        }


        GameObject chunk = ChunkObjectDictionary[meshData.position];
        // Mesh mesh = chunk.GetComponent<MeshFilter>().mesh;
        // mesh.Clear();

        Mesh mesh = new Mesh();
        mesh.vertices = meshData.vertexList;
        mesh.triangles = meshData.triangleList;
        mesh.normals = meshData.normals;
        MeshDictionary[key] = mesh;
        
        // Mesh constructed = new Mesh();
        // constructed.vertices = meshData.vertexList;
        // constructed.triangles = meshData.triangleList;
        // constructed.normals = meshData.normals;
        
        // chunk.SetActive(false);

        // collider manipulation
        if(LODindex == 1){
            if(chunk.GetComponent<MeshCollider>()){
                chunk.GetComponent<MeshCollider>().enabled = true;
            }
            else{
                MeshCollider collider = chunk.AddComponent<MeshCollider>();
                collider.sharedMesh = chunk.GetComponent<MeshFilter>().mesh;
            }
        }
        else{
            
            MeshCollider collider = chunk.GetComponent<MeshCollider>();
            if(collider != null){
                collider.enabled = false;
            }
        }
    }
}

[Serializable]
public struct SpawnableSettings{
    public Spawnable type;
    [Range(0,1)]
    public float minHeight;
    
    [Range(0,1)]
    public float maxHeight;
    public float minScale;
    public float maxScale;

    public float maxSlope;
    public int countInChunk;

    public SpawnableSettings(Spawnable type, float minHeight, float maxHeight, float maxSlope, int countInChunk, float minScale, float maxScale)
    {
        if(minHeight > maxHeight){
            maxHeight = minHeight;
        }

        if ( maxHeight < minHeight){
            maxHeight = minHeight;
        }
        
        this.type = type;
        this.minHeight = minHeight;
        this.maxHeight = maxHeight;
        this.maxSlope = maxSlope;
        this.countInChunk = countInChunk;
        this.minScale = minScale;
        this.maxScale = maxScale;
    }
}

public struct ChunkUpdate
{
    public Vector3 position;
    public int LODindex;
    public MeshData meshData;
    public ChunkUpdate(Vector3 position, MeshData meshData, int LODindex)
    {
        this.position = position;
        this.meshData = meshData;
        this.LODindex = LODindex;
    }
}