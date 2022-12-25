using UnityEngine;
using System;
using System.Collections;
using System.Threading;
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
    [SerializeField] private float MaxTerrainHeight;
    [SerializeField] private Transform Tracker;

    [Header("Tree")]
    [SerializeField] private Mesh TreeMesh;

    [SerializeField] private Material TreeMaterial;


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
    


    [SerializeField] public ComputeShader HeightMapShader;
    public SeedGenerator SeedGenerator;

    private Vector2 CurrentCell;

    public Vector3 HighestPoint;
    public GameObject HighestPointMonument;
    private bool PastGenerationComplete = false;
    // Vector3 -> Vector2
    // z -> y
    // V3(x,y,z) -> V2(x,z)
    void Start()
    {
        SeedGenerator = new SeedGenerator(123);
        Tracker.position = new Vector3(0, MaxTerrainHeight, 0);

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
                Instantiate(HighestPointMonument,HighestPoint,Quaternion.Euler(-90,-90,0));
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

            // Rendering trees trees
            foreach (Chunk chunkInstance in TreeChunkDictionary.Values)
            {
                Graphics.DrawMeshInstanced(TreeMesh, 0, TreeMaterial, chunkInstance.treesTransforms);
                Graphics.DrawMeshInstanced(TreeMesh, 1, TreeMaterial, chunkInstance.treesTransforms);
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
        return chunk;
    }

    void OnDrawGizmos()
    {
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