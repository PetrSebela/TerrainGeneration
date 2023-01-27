using UnityEngine;
using System;
using Unity.Mathematics;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;

public class ChunkManager : MonoBehaviour
{
    [Header("Simulation setting")]
    public ChunkSettings ChunkSettings;
    public TerrainSettings TerrainSettings;

    [SerializeField] public int WorldSize;
    [SerializeField] public int LODTreeBorder;
    [SerializeField] private Transform TrackedObject;

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
    public Material WaterMaterial;
    public float waterLevel;

    
    [Header("Exposed variables")]
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

    [SerializeField] public ComputeShader HeightMapShader;
    public SeedGenerator SeedGenerator;
    public int NumOfPeaks;
    public SimulationSettings simulationSettings;
    public float globalNoiseLowest = Mathf.Infinity;
    public float globalNoiseHighest = -Mathf.Infinity;
    public int enviromentProgress = 0;
    public float Progress = 0;
    
    public RawImage MapDisplay;
    public Texture2D MapTexture;


    [Header("LowDetailModels")]
    public Material BaseImpostorMaterial;
    public List<Chunk> LowDetail = new List<Chunk>();
    public Texture2D[] ImpostorTextures;
    public Material[] ImpostorMaterials;    
    public AnimationCurve terrainCurve;


    //! chunk batching
    private Mesh[] combinesMeshes = new Mesh[0];

    private IEnumerator UpdateCorutine;
    int hold = 0;

    private Dictionary<Spawnable,int> LowDetailCounter = new Dictionary<Spawnable, int>();
    private Dictionary<Spawnable,int> DetailCounter = new Dictionary<Spawnable, int>();

    private Dictionary<Spawnable, List<List<Matrix4x4>>> LowDetailBatches = new Dictionary<Spawnable, List<List<Matrix4x4>>>();
    private Dictionary<Spawnable, List<List<Matrix4x4>>> DetailBatches = new Dictionary<Spawnable, List<List<Matrix4x4>>>();
    private int ProcessIndexer = 2;
    

    // Vector3 -> Vector2
    // z -> y
    // V3(x,y,z) -> V2(x,z)

    //* Setting up simulation  
    void Start()
    {
        ImpostorMaterials = new Material[ImpostorTextures.Length];
        
        for (int i = 0; i < ImpostorTextures.Length; i++)
        {
            Material impostorMaterial = new Material(BaseImpostorMaterial);
            impostorMaterial.SetTexture("_BaseMap",ImpostorTextures[i]);
            ImpostorMaterials[i] = impostorMaterial;
        }

        TerrainMaterial.SetTexture("_Texture2D",TextureCreator.GenerateTexture());
        


        UpdateCorutine = BatchMeshes();
        //! Simulation setup
        // MaxTerrainHeight = (simulationSettings.maxHeight == 0)? MaxTerrainHeight : simulationSettings.maxHeight;
        WorldSize = simulationSettings.WorldSize;
        string seed = simulationSettings.Seed;
        GenerateWorld(seed);
    }

    void GenerateWorld(string seed){
        int seedInt;
        if(int.TryParse(simulationSettings.Seed, out seedInt)){
            SeedGenerator = new SeedGenerator(seedInt);
        }
        else{
            SeedGenerator = new SeedGenerator(seed);
        }

        TrackedObject.position = new Vector3(0, simulationSettings.MaxHeight, 0);

        StartCoroutine(GenerationManager.GenerationCorutine(this)); 
    }

    void FixedUpdate(){
        if (!GenerationComplete)
            return;
        
        hold = MeshQueue.Count;
        switch (ProcessIndexer)
        {
            case 2:
                Vector2 currentChunkPosition = new Vector2(
                    Mathf.Round(TrackedObject.position.x / ChunkSettings.ChunkSize), 
                    Mathf.Round(TrackedObject.position.z / ChunkSettings.ChunkSize)
                );
                
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
                    ProcessIndexer = 0;
                }
                break;
    
            case 0:
                for (int f = 0; f < hold; f++)
                {
                    ChunkUpdate updateMeshData;
                    lock (MeshQueue)
                        updateMeshData = MeshQueue.Dequeue();
                    UpdateChunk(updateMeshData.meshData, updateMeshData.LODindex);
                }  
                ProcessIndexer = 1;
                break;
    
            case 1:
                BatchEnviroment();
                StopCoroutine(UpdateCorutine);
                UpdateCorutine = BatchMeshes();
                StartCoroutine(UpdateCorutine);
                ProcessIndexer = 2;
                break;

            default:
                break;
        }
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

        Dictionary<Vector2,Chunk> activeDictionary = (FullRender)? ChunkDictionary : TreeChunkDictionary;

        // Rendering enviromental details
        foreach (Spawnable spawnableType in DetailBatches.Keys)
        {   
            foreach (List<Matrix4x4> envList in DetailBatches[spawnableType])
            {
                switch (spawnableType)
                {
                    case Spawnable.ConiferTree:
                        Graphics.DrawMeshInstanced(TreeMesh2, 1, BarkMaterial, envList);
                        Graphics.DrawMeshInstanced(TreeMesh2, 0, BushMaterial, envList);
                        break;

                    case Spawnable.DeciduousTree:
                        Graphics.DrawMeshInstanced(TreeMesh, 0, BarkMaterial, envList);
                        Graphics.DrawMeshInstanced(TreeMesh, 1, CrownMaterial, envList);
                        break;

                    case Spawnable.Rock:
                        Graphics.DrawMeshInstanced(RockMesh, 0, RockMaterial, envList);
                        break;

                    case Spawnable.Bush:
                        Graphics.DrawMeshInstanced(BushMesh, 0, BushMaterial, envList);
                        Graphics.DrawMeshInstanced(BushMesh, 1, BarkMaterial, envList);
                        break;                           

                    default:
                        break;
                }
            }
        }      
       
        //* SECTION - Rendering
        foreach (Spawnable spawnableType in LowDetailBatches.Keys)
        {   
            foreach (List<Matrix4x4> envList in LowDetailBatches[spawnableType])
            {
                switch (spawnableType)
                {
                    case Spawnable.ConiferTree:
                        Graphics.DrawMeshInstanced(LowDetailBase, 0, ImpostorMaterials[0], envList);                 
                        Graphics.DrawMeshInstanced(LowDetailBase, 1, ImpostorMaterials[1], envList);                 
                        break;

                    case Spawnable.DeciduousTree:
                        Graphics.DrawMeshInstanced(LowDetailBase, 0, ImpostorMaterials[2], envList);                 
                        Graphics.DrawMeshInstanced(LowDetailBase, 1, ImpostorMaterials[3], envList);
                        break;

                    default:
                        break;
                }
            }
        }

        foreach (Mesh mesh in combinesMeshes)
        {
            Graphics.DrawMesh(mesh,Vector3.zero,Quaternion.identity, TerrainMaterial, 0);
        }
    }

    public void BatchEnviroment(){
        float st = Time.realtimeSinceStartup;

        //* -- Low resolution asset -- *
        LowDetailBatches.Clear();
        LowDetailBatches = new Dictionary<Spawnable, List<List<Matrix4x4>>>(){
            {Spawnable.ConiferTree,new List<List<Matrix4x4>>()},
            {Spawnable.DeciduousTree,new List<List<Matrix4x4>>()},
        };
        LowDetailCounter = new Dictionary<Spawnable, int>(){
            {Spawnable.ConiferTree,0},
            {Spawnable.DeciduousTree,0},
        };

        foreach (Chunk chunk in LowDetail)
        {
            foreach(Spawnable type in new Spawnable[]{Spawnable.ConiferTree,Spawnable.DeciduousTree})
            {    
                foreach (Matrix4x4 item in chunk.detailDictionary[type])
                {
                    if(LowDetailCounter[type] % 1023 == 0){
                        LowDetailBatches[type].Add(new List<Matrix4x4>());
                    }

                    LowDetailBatches[type][(int)(LowDetailCounter[type] / 1023)].Add(item);
                    LowDetailCounter[type]++;
                }
            }
        }


        //* -- High resolution asset -- *

        DetailBatches.Clear();
        DetailCounter.Clear();

        var spawnableEnum = Enum.GetValues(typeof(Spawnable));
        foreach (Spawnable spawnable in spawnableEnum)
        {
            DetailBatches.Add(spawnable,new List<List<Matrix4x4>>());
            DetailCounter.Add(spawnable,0);
        }

        foreach (Chunk chunk in TreeChunkDictionary.Values)
        {
            foreach(Spawnable type in spawnableEnum)
            {    
                foreach (Matrix4x4 item in chunk.detailDictionary[type])
                {
                    if(DetailCounter[type] % 1023 == 0){
                        DetailBatches[type].Add(new List<Matrix4x4>());
                    }

                    DetailBatches[type][(int)(DetailCounter[type] / 1023)].Add(item);
                    DetailCounter[type]++;
                }
            }
        }
        Debug.Log("Enviroment batching took : " + (Mathf.Round(((Time.realtimeSinceStartup - st)*1000*100000))/100000) + "ms");
    }

    public IEnumerator BatchMeshes(){
        float st = Time.realtimeSinceStartup;
        List<List<CombineInstance>> mergeList = new List<List<CombineInstance>>(){new List<CombineInstance>()}; 
        int vertCounter = 0;
        int meshIndex = 0;

        for (int x = -WorldSize; x < WorldSize; x++)
        {
            for (int z = -WorldSize; z < WorldSize; z++)
            {
                Mesh mesh = MeshDictionary[new Vector2(x,z)];
                if((long)(vertCounter + mesh.vertexCount) < 4294967295){
                    CombineInstance instance = new CombineInstance();
                    instance.mesh = mesh;
                    instance.transform = Matrix4x4.TRS(new Vector3(x,0,z) * ChunkSettings.ChunkSize,Quaternion.identity,Vector3.one);
                    mergeList[meshIndex].Add(instance);
                }
                else{
                    mergeList.Add( new List<CombineInstance>() );
                    meshIndex++;    
                    vertCounter = 0;                

                    CombineInstance instance = new CombineInstance();
                    instance.mesh = mesh;
                    instance.transform = Matrix4x4.TRS(new Vector3(x,0,z) * ChunkSettings.ChunkSize,Quaternion.identity,Vector3.one);
                    
                    mergeList[meshIndex].Add(instance);
                }

                vertCounter += mesh.vertexCount;
            }
        }

        foreach (Mesh mesh in combinesMeshes)
        {
            DestroyImmediate(mesh);
        }

        combinesMeshes = new Mesh[mergeList.Count];

        for (int i = 0; i < mergeList.Count; i++)
        {
            Mesh mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            CombineInstance[] list = mergeList[i].ToArray();
            mesh.CombineMeshes(list, true, true);
            combinesMeshes[i] = mesh;
        }
        Debug.Log("Mesh batching took : " + (Mathf.Round(((Time.realtimeSinceStartup - st)*1000*100000))/100000) + "ms");
        yield return null;
    }


    void UpdateChunk(MeshData meshData, int LODindex)
    {
        // calculating tree topology
        Vector2 key = new Vector2(meshData.position.x, meshData.position.z);
        if (LODindex <= LODTreeBorder && !TreeChunkDictionary.ContainsKey(key))
        {
            TreeChunkDictionary.Add(key, ChunkDictionary[key]);
            LowDetail.Remove(ChunkDictionary[key]);
        }

        else if (LODindex > LODTreeBorder && TreeChunkDictionary.ContainsKey(key))
        {
            TreeChunkDictionary.Remove(key);
            LowDetail.Add(ChunkDictionary[key]);
        }

        Mesh mesh = MeshDictionary[key];
        mesh.Clear();
        mesh.vertices = meshData.vertexList;
        mesh.triangles = meshData.triangleList;
        mesh.normals = meshData.normals;
        MeshDictionary[key] = mesh;
        
        GameObject chunk = ChunkObjectDictionary[meshData.position];
        if(LODindex == 2)
        {
            if(chunk.GetComponent<MeshCollider>())
            {
                chunk.GetComponent<MeshCollider>().enabled = true;
            }
            else
            {
                MeshCollider collider = chunk.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
            }
        }
        else
        {
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

    public SpawnableSettings(Spawnable type, 
    float minHeight, 
    float maxHeight, 
    float maxSlope, 
    int countInChunk, 
    float minScale, 
    float maxScale)
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