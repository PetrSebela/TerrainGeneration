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

    [HideInInspector] public int WorldSize;
    public Transform TrackedObject;

    [Header("Enviroment")]
    [SerializeField] private Mesh LowDetailBase;

    
    [Header("Materials")]
    [SerializeField] public Material TerrainMaterial;



    [Header("Water settings")]
    public Material WaterMaterial;
    public float waterLevel;

    
    [Header("Exposed variables")]
    public Dictionary<Vector2, Chunk> ChunkDictionary = new Dictionary<Vector2, Chunk>();
    [HideInInspector] public Vector2 PastChunkPosition = Vector2.zero;

    //! Terrain variables
    [HideInInspector] public SeedGenerator SeedGenerator;
    public SimulationSettings simulationSettings;
    
    //! Terrain variables
    [HideInInspector] public float GlobalNoiseLowest = Mathf.Infinity;
    [HideInInspector] public float GlobalNoiseHighest = -Mathf.Infinity;
    
    //! Progress variables
    
    [HideInInspector] public string ActiveGenerationJob = "";
    [HideInInspector] public int EnviromentProgress = 0;
    [HideInInspector] public int MapProgress = 0;
    [HideInInspector] public float Progress = 0;
    [HideInInspector] public bool GenerationComplete = false;
    
    public RawImage MapDisplay;


    [Header("LowDetailModels")]
    public Material BaseImpostorMaterial;
    public Texture2D[] ImpostorTextures;
    public Material[] ImpostorMaterials;    

    [Header("Colors")]
    public Color Sand;
    public Color Stone;
    public Color StoneLighter;
    public Color StoneDarker;
    public Color Grass;
    public Color GrassDarker;

    private Dictionary<TreeObject,int> LowDetailCounter = new Dictionary<TreeObject, int>();
    private Dictionary<TreeObject,int> DetailCounter = new Dictionary<TreeObject, int>();

    public Dictionary<TreeObject, List<List<Matrix4x4>>> LowDetailBatches = new Dictionary<TreeObject, List<List<Matrix4x4>>>();
    public Dictionary<TreeObject, List<List<Matrix4x4>>> DetailBatches = new Dictionary<TreeObject, List<List<Matrix4x4>>>();

    public Queue<MeshRequest> MeshRequests = new Queue<MeshRequest>();
    public Queue<MeshUpdate> MeshUpdates = new Queue<MeshUpdate>();
    private Thread ProcessingThread;

    private int LastQueueCount = 0;

    public AnimationCurve TerrainCurve;
    public AnimationCurve TerrainFalloffCurve;
    public AnimationCurve TerrainEaseCurve;

    public SimulationState simulationState;
    public PlayerController PlayerController;

    public StructureObject[] StructureObjects;
    public List<ObjectSizeDescriptor> StructureSizeDescriptorList = new List<ObjectSizeDescriptor>();

    public TreeObject[] TreeObjects;
    public TreeObject[] LowTreeObjects;

    public int DockCount;
    public GameObject DockObject;

    public bool SetViewerPositionFromScript = false;

    // Vector3 -> Vector2
    // z -> y
    // V3(x,y,z) -> V2(x,z)

    //* Setting up simulation  

    void OnApplicationQuit(){
        ProcessingThread.Abort();
    }

    void Start()
    {
        Debug.Log(TreeObjects.Length);
        ImpostorMaterials = new Material[ImpostorTextures.Length];
        
        for (int i = 0; i < ImpostorTextures.Length; i++)
        {
            Material impostorMaterial = new Material(BaseImpostorMaterial);
            impostorMaterial.SetTexture("_BaseMap",ImpostorTextures[i]);
            ImpostorMaterials[i] = impostorMaterial;
        }
        
        // Automatic impostor material setup
        foreach (TreeObject treeObject in TreeObjects)
        {
            treeObject.ImpostorMaterials = new Material[treeObject.ImpostorTextures.Length];
            for (int i = 0; i < treeObject.ImpostorTextures.Length; i++)
            {
                Material impostorMaterial = new Material(treeObject.BaseImpostorMaterial);
                impostorMaterial.SetTexture("_BaseMap",treeObject.ImpostorTextures[i]);
                treeObject.ImpostorMaterials[i] = impostorMaterial;                
            }
        }

        // Generating heightmap texture
        TerrainMaterial.SetTexture("_Texture2D",TextureCreator.GenerateTexture(this));
        
        ChunkUpdateProcessor updateProcessor = new ChunkUpdateProcessor(this);

        ThreadStart threadStart = delegate
        {
            updateProcessor.UpdateProcessingThread();
        };

        ProcessingThread = new Thread(threadStart);
        ProcessingThread.Start();

        //! Simulation setup
        WorldSize = simulationSettings.WorldSize;        
        GenerateWorld(simulationSettings.Seed);
    }

    void GenerateWorld(string seed){
        Debug.Log("Raw seed : " + seed);
        
        int seedInt;
        if(int.TryParse(simulationSettings.Seed, out seedInt)){
            SeedGenerator = new SeedGenerator(seedInt,this);
        }
        else{
            SeedGenerator = new SeedGenerator(seed,this);
        }

        simulationState = SerializationHandler.DeserializeSimulatinoState(SeedGenerator.seed.ToString());
        
        if (simulationState == null){
            Debug.Log("Viewer set to default values");
            TrackedObject.position = new Vector3(0, TerrainSettings.MaxHeight, 0);
            simulationState = new SimulationState();
            SetViewerPositionFromScript = true;
        }
        else{
            Debug.Log("Viewer data loaded from memory");
            TrackedObject.position = simulationState.ViewerPosition;
            PlayerController.cameraRotation = simulationState.ViewerOrientation;
            PlayerController.ControllerType = simulationState.ControllerType;
        }

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
                        ChunkDictionary[sampler].UpdateChunk(new Vector3(PastChunkPosition.x,0,PastChunkPosition.y));
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
                        (float)EnviromentProgress / (WorldSize * 2) + 
                        (float)MapProgress / worldChunkArea) / 3;
            return;
        }    

        // Rendering enviromental details
        foreach (TreeObject treeObject in TreeObjects)
        {
            foreach (List<Matrix4x4> envList in DetailBatches[treeObject])
            {
                for (int submeshIndex = 0; submeshIndex < treeObject.Mesh.subMeshCount; submeshIndex++)
                {
                    Graphics.DrawMeshInstanced(treeObject.Mesh, submeshIndex, treeObject.MeshMaterials[submeshIndex], envList);
                }            
            }
        }
        foreach (TreeObject treeObject in LowTreeObjects)
        {
            foreach (List<Matrix4x4> envList in LowDetailBatches[treeObject])
            {
                for (int impostorMaterialIndex = 0; impostorMaterialIndex < 2; impostorMaterialIndex++)
                {
                    Graphics.DrawMeshInstanced(LowDetailBase, impostorMaterialIndex, treeObject.ImpostorMaterials[impostorMaterialIndex], envList);
                }            
            }
        }
    }

    public void BatchEnviroment(){
        float st = Time.realtimeSinceStartup;

        //* -- Low resolution assets -- *
        LowDetailBatches.Clear();
        LowDetailCounter.Clear();
        LowDetailBatches = new Dictionary<TreeObject, List<List<Matrix4x4>>>();        
        LowDetailCounter = new Dictionary<TreeObject, int>();

        foreach (TreeObject treeObject in LowTreeObjects)
        {
            LowDetailBatches.Add(treeObject,new List<List<Matrix4x4>>());
            LowDetailCounter.Add(treeObject,0);
        }

        DetailBatches.Clear();
        DetailCounter.Clear();

        foreach (TreeObject treeObject in TreeObjects)
        {
            DetailBatches.Add(treeObject,new List<List<Matrix4x4>>());
            DetailCounter.Add(treeObject,0);
        }


        foreach (Chunk chunk in ChunkDictionary.Values)
        {
            if(chunk.CurrentLODindex >= 2  && chunk.CurrentLODindex <= 16){
                foreach(TreeObject treeObject in LowTreeObjects)
                {    
                    foreach (Matrix4x4 item in chunk.LowDetailDictionary[treeObject])
                    {
                        if(LowDetailCounter[treeObject] % 1023 == 0){
                            LowDetailBatches[treeObject].Add(new List<Matrix4x4>());
                        }

                        LowDetailBatches[treeObject][(int)(LowDetailCounter[treeObject] / 1023)].Add(item);
                        LowDetailCounter[treeObject]++;
                    }
                }
            }
            else if (chunk.CurrentLODindex < 4){
                foreach(TreeObject treeObject in TreeObjects)
                {    
                    foreach (Matrix4x4 item in chunk.DetailDictionary[treeObject])
                    {
                        if(DetailCounter[treeObject] % 1023 == 0){
                            DetailBatches[treeObject].Add(new List<Matrix4x4>());
                        }

                        DetailBatches[treeObject][(int)(DetailCounter[treeObject] / 1023)].Add(item);
                        DetailCounter[treeObject]++;
                    }
                }
            }
        }
    }

    // void OnDrawGizmos()
    // {
    //     NoiseConverter noiseConverter = new NoiseConverter(
    //         GlobalNoiseLowest,
    //         GlobalNoiseHighest,
    //         TerrainSettings.MinHeight,
    //         TerrainSettings.MaxHeight,
    //         TerrainCurve
    //     );

    //     for (int dockIndex = 0; dockIndex < DockCount; dockIndex++)
    //     {
    //         float angle = (float)(360 / DockCount) * dockIndex;
    //         float sampleDistance = 1f;
    //         float pastSample = noiseConverter.GetRealHeight(1);
    //         Vector2 samplerPositon = Vector2.zero;

    //         while(Vector2.Distance(Vector2.zero, samplerPositon) <= WorldSize * ChunkSettings.ChunkSize){
    //             Gizmos.color = Color.red;
    //             float s1 = GenerationManager.SampleNoise(
    //                         samplerPositon,
    //                         TerrainSettings,
    //                         this
    //                     );

    //             float s2 = GenerationManager.SampleNoise(
    //                 samplerPositon + new Vector2(512.4f,752.4f),
    //                 TerrainSettings,
    //                 this
    //             );

    //             float distance =  Vector2.Distance(Vector2.zero, samplerPositon) / (WorldSize  * ChunkSettings.ChunkResolution);
    //             float currentSample =  GenerationManager.SampleNoise(
    //                 new Vector2(
    //                     samplerPositon.x + s1 * 15000.1f * TerrainSettings.WrinkleMagniture,
    //                     samplerPositon.y + s2 * 8020.1f * TerrainSettings.WrinkleMagniture
    //                 ), 
    //                 TerrainSettings, 
    //                 this);
    //             currentSample *= TerrainFalloffCurve.Evaluate(distance);
    //             currentSample = noiseConverter.GetRealHeight(currentSample);


    //             if (pastSample >= 0 && currentSample < 0){
    //                 Gizmos.color = Color.green;
    //             }


    //             Gizmos.DrawSphere(new Vector3(samplerPositon.x,currentSample,samplerPositon.y),0.1f);
                
    //             pastSample = currentSample;
                
    //             samplerPositon += new Vector2(
    //                 Mathf.Cos(angle * Mathf.Deg2Rad) * sampleDistance,
    //                 Mathf.Sin(angle * Mathf.Deg2Rad) * sampleDistance
    //             );
    //         }
    //     }

        // Gizmos.color = new Color(1, 0, 0, 0.5f);
        // foreach (Chunk chunk in ChunkDictionary.Values)
        // {
        //     foreach (ObjectSizeDescriptor obj in chunk.SizeDescriptorList)
        //     {
        //         Vector3 position
        //         Gizmos.DrawCube(, new Vector3(1, 1, 1));
        //     }
        // }
    // }
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