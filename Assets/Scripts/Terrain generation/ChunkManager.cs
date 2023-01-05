using UnityEngine;
using System;
using Unity.Mathematics;
using System.Collections.Generic;

public class ChunkManager : MonoBehaviour
{
    [Header("Chunk setting")]
    public ChunkSettings ChunkSettings;
    [SerializeField] private Material DefaultMaterial;

    [Header("World setting")]
    [SerializeField] public int ChunkRenderDistance;
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
    private Dictionary<Vector3, GameObject> ChunkObjectDictionary = new Dictionary<Vector3, GameObject>();
    private Dictionary<Vector2, Chunk> TreeChunkDictionary = new Dictionary<Vector2, Chunk>();
    // Queues
    public Queue<Vector2> HeightmapGenQueue = new Queue<Vector2>();
    public Queue<Vector2> ChunkUpdateRequestQueue = new Queue<Vector2>();
    public Queue<ChunkUpdate> MeshQueue = new Queue<ChunkUpdate>();


    public Vector2 PastChunkPosition = Vector2.zero;
    public bool GenerationComplete = false;

    public bool FullRender = false;
    


    [SerializeField] public ComputeShader HeightMapShader;
    public SeedGenerator SeedGenerator;

    private Vector2 CurrentCell;

    public Vector3 HighestPoint;

    public Vector3[] Peaks;
    public int NumOfPeaks;
    public Vector2[] PeaksPOI;
    public GameObject HighestPointMonument;

    public GameObject Monument;
    private bool PastGenerationComplete = false;

    // Vector3 -> Vector2
    // z -> y
    // V3(x,y,z) -> V2(x,z)
    void Start()
    {
        SeedGenerator = new SeedGenerator(123);
        SeedGenerator = new SeedGenerator("ahoj");
        Tracker.position = new Vector3(0, MaxTerrainHeight, 0);
        
        Peaks = new Vector3[NumOfPeaks];
        PeaksPOI = new Vector2[NumOfPeaks];

        for (int i = 0; i < NumOfPeaks; i++)
        {
            float angle = 360 / NumOfPeaks * i;
            float x = math.cos(angle) * (ChunkRenderDistance / 2 * ChunkSettings.size);
            float y = math.sin(angle) * (ChunkRenderDistance / 2 * ChunkSettings.size);
            PeaksPOI[i] = new Vector2(x,y);
        }
        // sampling terrain chunks
        for (int x = -ChunkRenderDistance; x < ChunkRenderDistance; x++)
        {
            for (int y = -ChunkRenderDistance; y < ChunkRenderDistance; y++)
            {
                Vector2 sampler = new Vector2(x, y);
                HeightmapGenQueue.Enqueue(sampler);
            }
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
            // execute only once
            if (PastGenerationComplete == false){
                foreach (Vector3 pos in Peaks)
                {
                    float angle = Mathf.Rad2Deg*Mathf.Atan(pos.x/pos.z) - 90;
                    Instantiate(HighestPointMonument,pos,Quaternion.Euler(0,angle - 90,0));
                }

                float height = ChunkDictionary[Vector2.zero].heightMap[1,1];
                if(height < waterLevel)
                    height = waterLevel;
                GameObject monu = Instantiate(Monument,new Vector3(0,height,0),Quaternion.Euler(0,0,0));
                monu.transform.localScale = Vector3.one * 3.25f;
                PastGenerationComplete = true;
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
                        if (Math.Abs(sampler.x) < ChunkRenderDistance && Math.Abs(sampler.y) < ChunkRenderDistance)
                            lock (ChunkUpdateRequestQueue)
                                ChunkUpdateRequestQueue.Enqueue(sampler);
                    }
                }
            }


            // Rendering chunks
            int hold = MeshQueue.Count;
            for (int f = 0; f < hold; f++)
            {
                ChunkUpdate updateMeshData;
                lock (MeshQueue)
                    updateMeshData = MeshQueue.Dequeue();
                UpdateChunk(updateMeshData.meshData, updateMeshData.LODindex);
            }


           Dictionary<Vector2,Chunk> activeDictionary = TreeChunkDictionary;

            if(FullRender){
                activeDictionary = ChunkDictionary;
            }

            // Rendering trees trees
            foreach (Chunk chunkInstance in activeDictionary.Values)
            {
                foreach (var spawnableType in chunkInstance.treesDictionary.Keys)
                {
                    if (chunkInstance.treesDictionary[spawnableType].Length > 0)
                    {
                        switch (spawnableType)
                        {
                            case Spawable.ConiferTree:
                                Graphics.DrawMeshInstanced(TreeMesh2, 1, BarkMaterial, chunkInstance.treesDictionary[spawnableType]);
                                Graphics.DrawMeshInstanced(TreeMesh2, 0, CrownMaterial, chunkInstance.treesDictionary[spawnableType]);
                                break;

                            case Spawable.DeciduousTree:
                                Graphics.DrawMeshInstanced(TreeMesh, 0, BarkMaterial, chunkInstance.treesDictionary[spawnableType]);
                                Graphics.DrawMeshInstanced(TreeMesh, 1, CrownMaterial, chunkInstance.treesDictionary[spawnableType]);
                                break;

                            case Spawable.Rock:
                                Graphics.DrawMeshInstanced(RockMesh, 0, RockMaterial, chunkInstance.treesDictionary[spawnableType]);
                                break;

                            case Spawable.Bush:
                                Graphics.DrawMeshInstanced(BushMesh, 0, BushMaterial, chunkInstance.treesDictionary[spawnableType]);
                                Graphics.DrawMeshInstanced(BushMesh, 1, BarkMaterial, chunkInstance.treesDictionary[spawnableType]);

                                break;                           

                            default:
                                break;
                        }                        
                    }
                }
            }
        }
        else
            Progress = (float)HeightMapDict.Count / math.pow(ChunkRenderDistance * 2, 2);
    }

    GameObject UpdateChunk(MeshData meshData, int LODindex)
    {


        Vector2 key = new Vector2(meshData.position.x, meshData.position.z);
        if (LODindex <= LODtreeBorder && !TreeChunkDictionary.ContainsKey(key))
        {
            TreeChunkDictionary.Add(key, ChunkDictionary[key]);
        }
        else if (LODindex > LODtreeBorder && TreeChunkDictionary.ContainsKey(key))
        {
            TreeChunkDictionary.Remove(key);
        }

        GameObject chunk;
        if (ChunkObjectDictionary.ContainsKey(meshData.position))
        {
            chunk = ChunkObjectDictionary[meshData.position];
            Mesh mesh = chunk.GetComponent<MeshFilter>().mesh;
            mesh.Clear();
            mesh.vertices = meshData.vertexList;
            mesh.triangles = meshData.triangleList;
            mesh.RecalculateNormals();
        }

        else
        {
            chunk = new GameObject();
            chunk.layer = LayerMask.NameToLayer("Ground");
            chunk.isStatic = true;
            chunk.transform.parent = this.transform;
            chunk.transform.position = meshData.position * ChunkSettings.size;
            chunk.transform.name = meshData.position.ToString();

            MeshFilter meshFilter = chunk.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = chunk.AddComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;

            Mesh mesh = new Mesh();
            mesh.vertices = meshData.vertexList;
            mesh.triangles = meshData.triangleList;
            mesh.RecalculateNormals();

            meshFilter.mesh = mesh;

            ChunkObjectDictionary.Add(meshData.position, chunk);
        }



        chunk.GetComponent<MeshRenderer>().material = DefaultMaterial;
        if(LODindex == 1){
            MeshCollider collider = chunk.AddComponent<MeshCollider>();
            collider.sharedMesh = chunk.GetComponent<MeshFilter>().mesh;
        }
        else{
            MeshCollider collider = chunk.GetComponent<MeshCollider>();
            if(collider != null){
                Destroy(collider);
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

        // if (DrawChunkBorders && GenerationComplete)
        // {
        //     foreach (var item in HeightMapTree.Children)
        //     {
        //         Vector2 flatPosition = item.Value.Position;
        //         Vector3 spacePosition = new Vector3(flatPosition.x, 0, flatPosition.y) * ChunkSettings.size;
        //         spacePosition += new Vector3(HighestChunkGrouping * ChunkSettings.size / 2, 0, HighestChunkGrouping * ChunkSettings.size / 2);

        //         if (flatPosition == CurrentCell)
        //         {
        //             Gizmos.color = Color.yellow;
        //             Gizmos.DrawWireCube(
        //                 spacePosition,
        //                 new Vector3(HighestChunkGrouping * ChunkSettings.size, MaxTerrainHeight, HighestChunkGrouping * ChunkSettings.size)
        //             );

        //             Gizmos.color *= new Color(1, 1, 1, 0.25f);
        //             Gizmos.DrawCube(
        //                 spacePosition,
        //                 new Vector3(HighestChunkGrouping * ChunkSettings.size, MaxTerrainHeight, HighestChunkGrouping * ChunkSettings.size)
        //             );
        //         }
        //         else
        //         {
        //             Gizmos.color = Color.black;
        //             Gizmos.DrawWireCube(
        //                 spacePosition,
        //                 new Vector3(HighestChunkGrouping * ChunkSettings.size, MaxTerrainHeight, HighestChunkGrouping * ChunkSettings.size)
        //             );
        //         }
        //     }
        // }
    }
}

[Serializable]
public struct SpawnableSettings{
    public Spawable type;
    public float minHeight;
    public float maxHeight;
    public float maxSlope;
    public int countInChunk;

    public SpawnableSettings(Spawable type, float minHeight, float maxHeight, float maxSlope, int countInChunk)
    {
        this.type = type;
        this.minHeight = minHeight;
        this.maxHeight = maxHeight;
        this.maxSlope = maxSlope;
        this.countInChunk = countInChunk;
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