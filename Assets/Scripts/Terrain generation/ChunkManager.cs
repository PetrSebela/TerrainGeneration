using UnityEngine;
using System;
using Unity.Mathematics;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Collections;
using System.Threading;

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
    public Dictionary<Vector2, Chunk> TreeChunkDictionary = new Dictionary<Vector2, Chunk>();
    // Queues

    public Vector2 PastChunkPosition = Vector2.zero;
    public bool GenerationComplete = false;
    public bool FullRender = false;

    [SerializeField] public ComputeShader HeightMapShader;
    public SeedGenerator SeedGenerator;
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


    private Dictionary<Spawnable,int> LowDetailCounter = new Dictionary<Spawnable, int>();
    private Dictionary<Spawnable,int> DetailCounter = new Dictionary<Spawnable, int>();

    private Dictionary<Spawnable, List<List<Matrix4x4>>> LowDetailBatches = new Dictionary<Spawnable, List<List<Matrix4x4>>>();
    private Dictionary<Spawnable, List<List<Matrix4x4>>> DetailBatches = new Dictionary<Spawnable, List<List<Matrix4x4>>>();

    public Queue<MeshRequest> MeshRequests = new Queue<MeshRequest>();
    public Queue<MeshUpdate> MeshUpdates = new Queue<MeshUpdate>();
    private Thread ProcessingThread;

    public int LastQueueCount = 0;

    public GameObject BelltowerObject;
    public GameObject Signpost;

    public AnimationCurve TerrainEaseCurve;

    // Vector3 -> Vector2
    // z -> y
    // V3(x,y,z) -> V2(x,z)

    //* Setting up simulation  

    void OnApplicationQuit(){
        ProcessingThread.Abort();
    }
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
        
        ChunkUpdateProcessor updateProcessor = new ChunkUpdateProcessor(this);

        ThreadStart threadStart = delegate
        {
            updateProcessor.UpdateProcessingThread();
        };

        ProcessingThread = new Thread(threadStart);
        ProcessingThread.Start();

        //! Simulation setup
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

        Vector2 currentChunkPosition = new Vector2(
            Mathf.Round(TrackedObject.position.x / ChunkSettings.ChunkSize), 
            Mathf.Round(TrackedObject.position.z / ChunkSettings.ChunkSize)
        );

        if (currentChunkPosition != PastChunkPosition)
        {
            PastChunkPosition = currentChunkPosition;
            for (int x = -7; x <= 7; x++)
            {
                for (int y = -7; y <= 7; y++)
                {
                    Vector2 sampler = currentChunkPosition + new Vector2(x, y);
                    if (sampler.x >= -WorldSize && sampler.x < WorldSize && sampler.y >= -WorldSize && sampler.y < WorldSize)
                    {
                        ChunkDictionary[sampler].UpdateChunk(1,Vector2.zero,new Vector3(PastChunkPosition.x,0,PastChunkPosition.y));
                    }
                }
            }
        }

        if (MeshUpdates.Count == 0 && LastQueueCount>0){
            BatchEnviroment();
        }
        LastQueueCount = MeshUpdates.Count;


        if(MeshUpdates.Count > 0){
            int hold = MeshUpdates.Count;

            for (int i = 0; i < MeshUpdates.Count; i++)
            {
                MeshUpdate meshUpdate;
                lock(MeshUpdates){
                    meshUpdate = MeshUpdates.Dequeue();
                }
                meshUpdate.CallbackObject.OnMeshRecieved(meshUpdate.MeshData);   
            }
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
       
        // //* SECTION - Rendering
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

        DetailBatches.Clear();
        DetailCounter.Clear();

        foreach (Spawnable spawnable in Enum.GetValues(typeof(Spawnable)))
        {
            DetailBatches.Add(spawnable,new List<List<Matrix4x4>>());
            DetailCounter.Add(spawnable,0);
        }


        foreach (Chunk chunk in ChunkDictionary.Values)
        {
            if(chunk.CurrentLODindex >= 8){
                foreach(Spawnable type in new Spawnable[]{Spawnable.ConiferTree,Spawnable.DeciduousTree})
                {    
                    foreach (Matrix4x4 item in chunk.LowDetailDictionary[type])
                    {
                        if(LowDetailCounter[type] % 1023 == 0){
                            LowDetailBatches[type].Add(new List<Matrix4x4>());
                        }

                        LowDetailBatches[type][(int)(LowDetailCounter[type] / 1023)].Add(item);
                        LowDetailCounter[type]++;
                    }
                }
            }
            else{
                foreach(Spawnable type in Enum.GetValues(typeof(Spawnable)))
                {    
                    foreach (Matrix4x4 item in chunk.DetailDictionary[type])
                    {
                        if(DetailCounter[type] % 1023 == 0){
                            DetailBatches[type].Add(new List<Matrix4x4>());
                        }

                        DetailBatches[type][(int)(DetailCounter[type] / 1023)].Add(item);
                        DetailCounter[type]++;
                    }
                }
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