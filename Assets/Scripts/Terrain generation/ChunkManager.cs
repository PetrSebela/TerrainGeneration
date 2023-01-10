using UnityEngine;
using System;
using Unity.Mathematics;
using System.Collections.Generic;

public class ChunkManager : MonoBehaviour
{
    [Header("Chunk setting")]
    public ChunkSettings ChunkSettings;
    [SerializeField] public Material TerrainMaterial;

    [Header("World setting")]
    [SerializeField] public int WorldSize;
    [SerializeField] public int LODtreeBorder;

    [Header("Terrain")]
    public float MaxTerrainHeight;
    [SerializeField] private Transform Tracker;

    [Header("Tree")]
    [SerializeField] public List<SpawnableSettings> spSettings = new List<SpawnableSettings>();
    [SerializeField] private Mesh TreeMesh;
    [SerializeField] private Mesh TreeMesh2;
    [SerializeField] private Mesh RockMesh;
    [SerializeField] private Mesh BushMesh;


    [SerializeField] private Material CrownMaterial;
    [SerializeField] private Material BarkMaterial;

    [SerializeField] private Material RockMaterial;

    [SerializeField] private Material BushMaterial;

    [Header("Water")]
    public bool UseWater = true;
    public Material WaterMaterial;
    public float waterLevel;

    public bool DrawChunkBorders = false;
    public float Progress = 0;
    
    // Dictionaries 
    //! Replacing dictionary for tree structure
    public Dictionary<Vector2, float[,]> HeightMapDict = new Dictionary<Vector2, float[,]>();

    public Dictionary<Vector2, Chunk> ChunkDictionary = new Dictionary<Vector2, Chunk>();
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

    public Vector3[] Peaks;
    public int NumOfPeaks;
    public Vector2[] PeaksPOI;
    public GameObject HighestPointMonument;

    public GameObject Monument;

    public SimulationSettings simulationSettings;

    public float globalNoiseLowest = Mathf.Infinity;
    public float globalNoiseHighest = -Mathf.Infinity;

    public int enviromentProgress = 0;
    
    public Dictionary<Spawnable,List<List<Matrix4x4>>> FullTreeList = new Dictionary<Spawnable,List<List<Matrix4x4>>>(){
        {Spawnable.ConiferTree,new List<List<Matrix4x4>>()},
        {Spawnable.DeciduousTree,new List<List<Matrix4x4>>()},
        {Spawnable.Rock,new List<List<Matrix4x4>>()},
        {Spawnable.Bush,new List<List<Matrix4x4>>()},
    };


    // Vector3 -> Vector2
    // z -> y
    // V3(x,y,z) -> V2(x,z)
    void Start()
    {
        MaxTerrainHeight = (simulationSettings.maxHeight == 0)? MaxTerrainHeight : simulationSettings.maxHeight;
        WorldSize = simulationSettings.worldSize;
        int seed = 0;
        if(int.TryParse(simulationSettings.seed, out seed)){
            Debug.Log(seed);
            GenerateWorld(seed);
        }
        else{
            Debug.Log(simulationSettings.seed.GetHashCode());
            GenerateWorld(simulationSettings.seed.GetHashCode());
        }
    }

    void GenerateWorld(int seed){
        SeedGenerator = new SeedGenerator(seed);
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
        if (Input.GetKeyDown(KeyCode.G))
            DrawChunkBorders = !DrawChunkBorders;

        // generating chunk update requests
        if (GenerationComplete)
        {         
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

            if(!FullRender)
            // Rendering enviromental details
                foreach (Chunk chunkInstance in activeDictionary.Values)
                {
                    
                    foreach (var spawnableType in chunkInstance.detailDictionary.Keys)
                    {
                        if (chunkInstance.detailDictionary[spawnableType].Length > 0)
                        {
                            Matrix4x4[] detailArray = chunkInstance.detailDictionary[spawnableType];
                            switch (spawnableType)
                            {
                                case Spawnable.ConiferTree:
                                    Graphics.DrawMeshInstanced(TreeMesh2, 1, BarkMaterial, detailArray);
                                    Graphics.DrawMeshInstanced(TreeMesh2, 0, BushMaterial, detailArray);
                                    break;

                                case Spawnable.DeciduousTree:
                                    Graphics.DrawMeshInstanced(TreeMesh, 0, BarkMaterial, detailArray);
                                    Graphics.DrawMeshInstanced(TreeMesh, 1, CrownMaterial, detailArray);
                                    break;

                                case Spawnable.Rock:
                                    Graphics.DrawMeshInstanced(RockMesh, 0, RockMaterial, detailArray);
                                    break;

                                case Spawnable.Bush:
                                    Graphics.DrawMeshInstanced(BushMesh, 0, BushMaterial, detailArray);
                                    Graphics.DrawMeshInstanced(BushMesh, 1, BarkMaterial, detailArray);

                                    break;                           

                                default:
                                    break;
                            }                        
                        }
                    }
                }
            else{
                foreach(Spawnable spawnableType in FullTreeList.Keys){
                    foreach (List<Matrix4x4> array in FullTreeList[spawnableType])
                    {
                    Matrix4x4[] detailArray = array.ToArray();
                    switch (spawnableType)
                        {
                            case Spawnable.ConiferTree:
                                Graphics.DrawMeshInstanced(TreeMesh2, 1, BarkMaterial, detailArray);
                                Graphics.DrawMeshInstanced(TreeMesh2, 0, BushMaterial, detailArray);
                                break;

                            case Spawnable.DeciduousTree:
                                Graphics.DrawMeshInstanced(TreeMesh, 0, BarkMaterial, detailArray);
                                Graphics.DrawMeshInstanced(TreeMesh, 1, CrownMaterial, detailArray);
                                break;

                            case Spawnable.Rock:
                                Graphics.DrawMeshInstanced(RockMesh, 0, RockMaterial, detailArray);
                                break;

                            case Spawnable.Bush:
                                Graphics.DrawMeshInstanced(BushMesh, 0, BushMaterial, detailArray);
                                Graphics.DrawMeshInstanced(BushMesh, 1, BarkMaterial, detailArray);

                                break;                           

                            default:
                                break;
                        } 
                    } 
                }
            }
        }
        else
            Progress = ((float) ChunkDictionary.Count / math.pow(WorldSize * 2, 2) * 0.5f) + (((float)enviromentProgress / (WorldSize * 2)) * 0.5f);
    }

    GameObject UpdateChunk(MeshData meshData, int LODindex)
    {
        // calculating tree topology
        Vector2 key = new Vector2(meshData.position.x, meshData.position.z);
        if (LODindex <= LODtreeBorder && !TreeChunkDictionary.ContainsKey(key))
        {
            TreeChunkDictionary.Add(key, ChunkDictionary[key]);
        }
        else if (LODindex > LODtreeBorder && TreeChunkDictionary.ContainsKey(key))
        {
            TreeChunkDictionary.Remove(key);
        }

        GameObject chunk = ChunkObjectDictionary[meshData.position];
        Mesh mesh = chunk.GetComponent<MeshFilter>().mesh;
        mesh.Clear();
        mesh.vertices = meshData.vertexList;
        mesh.triangles = meshData.triangleList;
        mesh.RecalculateNormals();

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
        return chunk;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (DrawChunkBorders && GenerationComplete){
            foreach (Vector3 peak in Peaks)
            {
                Vector3 beaconPositionBottom = new Vector3(peak.x,-1000,peak.z);
                Vector3 beaconPositionTop = new Vector3(peak.x,2500,peak.z);

                Gizmos.DrawLine(beaconPositionBottom,beaconPositionTop);
            }
        }
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(new Vector3(0,-1000,0),new Vector3(0,2500,0));
    }
}

[Serializable]
public struct SpawnableSettings{
    public Spawnable type;
    public float minHeight;
    public float maxHeight;

    public float minScale;
    public float maxScale;

    public float maxSlope;
    public int countInChunk;

    public SpawnableSettings(Spawnable type, float minHeight, float maxHeight, float maxSlope, int countInChunk, float minScale, float maxScale)
    {
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