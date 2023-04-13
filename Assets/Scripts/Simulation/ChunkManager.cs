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
    [HideInInspector] public Vector3 WorldTopPosition;
    
    //! Progress variables
    
    [HideInInspector] public string ActiveGenerationJob = "";
    [HideInInspector] public int EnviromentProgress = 0;
    [HideInInspector] public int MapProgress = 0;
    [HideInInspector] public float GenerationProgress = 0;
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
    private Thread UpdateProcessingThread;

    private int LastQueueCount = 0;

    public AnimationCurve TerrainCurve;
    public AnimationCurve TerrainFalloffCurve;
    public AnimationCurve TerrainEaseCurve;

    public SimulationState SimulationState = null;
    public PlayerController PlayerController;
    public StructureObject[] StructureObjects;
    public List<ObjectSizeDescriptor> GlobalStructureSizeDescriptorList = new List<ObjectSizeDescriptor>();

    public TreeObject[] TreeObjects;
    public TreeObject[] LowDetailTreeObjects;

    public int DockCount;
    public GameObject DockObject;
    public float ForestSize;
    public UserConfig UserConfig;
    public bool SetViewerPositionFromScript = false;
    public GameObject Cross;
    public PauseMenu PauseMenu;

    // Vector3 -> Vector2
    // z -> y
    // V3(x,y,z) -> V2(x,z)

    //* Setting up simulation  

    void OnApplicationQuit(){
        UpdateProcessingThread.Abort();
    }

    void Start()
    {
        Debug.Log("Loading user config from chunk manager");
        UserConfig = UserConfig.LoadConfig();

        PauseMenu.FOVslider.value = UserConfig.UserFOV;       
        PauseMenu.QualityDropdown.value = UserConfig.LevelDetail;
        PauseMenu.resolutionDropdown.value = PauseMenu.resolutionDropdown.options.FindIndex(option => option.text == UserConfig.WinWidth + "x" + UserConfig.WinHeight);
        PauseMenu.SensitivitySlider.value = UserConfig.MouseSensitivity;
        PlayerController.InputSmoothing = UserConfig.InputSmoothing;

        ImpostorMaterials = new Material[ImpostorTextures.Length];
        
        for (int i = 0; i < ImpostorTextures.Length; i++)
        {
            Material impostorMaterial = new Material(BaseImpostorMaterial);
            impostorMaterial.SetTexture("_BaseMap", ImpostorTextures[i]);
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


        // update processing thread setup 
        ChunkUpdateProcessor updateProcessor = new ChunkUpdateProcessor(this);
        ThreadStart threadStart = delegate
        {
            updateProcessor.UpdateProcessingThread();
        };

        UpdateProcessingThread = new Thread(threadStart);
        UpdateProcessingThread.Start();

        //! Simulation setup
        WorldSize = simulationSettings.WorldSize;        
        GenerateWorld(simulationSettings.Name);
    }

    void GenerateWorld(string name){
        Debug.Log("Raw seed : " + name);

        
        if(name != ""){
            SimulationState = SerializationHandler.DeserializeSimulatinoState(name.ToString());
        }
                
        if (SimulationState == null){
            Debug.Log("Viewer set to default values");
            SimulationState = new SimulationState();
            SetViewerPositionFromScript = true;
        }
        else{
            Debug.Log("Viewer data loaded from memory");
            Debug.Log(SimulationState.ControllerType);
            TrackedObject.position = SimulationState.ViewerPosition + new Vector3(0,0.25f,0);
            PlayerController.CameraRotation = SimulationState.ViewerOrientation;
            PlayerController.RealRotation = SimulationState.ViewerOrientation;
            PlayerController.ControllerType = SimulationState.ControllerType;
            PlayerController.Rigidbody.useGravity = (SimulationState.ControllerType == ControllerType.Ground)? true: false;
            PlayerController.HoldPosition = true;
        }

        int seedInt;
        if(int.TryParse(simulationSettings.Seed, out seedInt)){
            SeedGenerator = new SeedGenerator(seedInt,this);
        }
        else{
            SeedGenerator = new SeedGenerator(name,this);
        }

        PauseMenu.SeedDisplay.text = "Currently on seed: " + SeedGenerator.seed.ToString();
        GenerationManager genenerationManager = new GenerationManager(this);
        StartCoroutine(genenerationManager.GenerationCorutine()); 
    }

    void FixedUpdate(){
        if (!GenerationComplete)
            return;

        UpdateChunks();

        if (MeshUpdates.Count == 0 && LastQueueCount > 0){
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

    public void UpdateChunks(){
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
                    if (IsInWorldBounds(sampler))
                    {
                        ChunkDictionary[sampler].UpdateChunk(new Vector3(PastChunkPosition.x,0,PastChunkPosition.y));
                    }
                }
            }
        }
    }

    public bool IsInWorldBounds(Vector2 position){
        return position.x >= -WorldSize && position.x < WorldSize && position.y >= -WorldSize && position.y < WorldSize;
    }

    public void BatchEnviroment(){
        LowDetailBatches.Clear();
        LowDetailCounter.Clear();

        foreach (TreeObject treeObject in LowDetailTreeObjects)
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
                foreach(TreeObject treeObject in LowDetailTreeObjects)
                {    
                    if(treeObject.IgnoreLOD){
                        continue;
                    }
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
            
            else if (chunk.CurrentLODindex < 2){
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

    void Update()
    {          
        // generating chunk update requests
        if (!GenerationComplete){
            float worldChunkArea = math.pow(WorldSize * 2, 2);
            GenerationProgress = ((float)ChunkDictionary.Count /  worldChunkArea  + 
                        (float)EnviromentProgress / (float)Math.Pow(WorldSize * 2f,2f) + 
                        (float)MapProgress / worldChunkArea) / 3;
            return;
        }    

        // Rendering enviromental details
        foreach (TreeObject treeObject in TreeObjects)
        {
            foreach (List<Matrix4x4> matrix4x4List in DetailBatches[treeObject])
            {
                for (int submeshIndex = 0; submeshIndex < treeObject.Mesh.subMeshCount; submeshIndex++)
                {
                    Graphics.DrawMeshInstanced(treeObject.Mesh, submeshIndex, treeObject.MeshMaterials[submeshIndex], matrix4x4List);
                }            
            }
        }


        foreach (TreeObject treeObject in LowDetailTreeObjects)
        {
            foreach (List<Matrix4x4> matrix4x4List in LowDetailBatches[treeObject])
            {
                for (int impostorMaterialIndex = 0; impostorMaterialIndex < 2; impostorMaterialIndex++)
                {
                    Graphics.DrawMeshInstanced(LowDetailBase, impostorMaterialIndex, treeObject.ImpostorMaterials[impostorMaterialIndex], matrix4x4List);
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