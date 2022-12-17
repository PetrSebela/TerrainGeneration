using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections;
using System.Threading;
using Unity.Mathematics;

public class ChunkManager : MonoBehaviour
{
    [Header("Chunk setting")]
    public ChunkSettings ChunkSettings;
    [SerializeField] private Material DefaultMaterial;

    [Header("World setting")]
    [SerializeField] public int ChunkRenderDistance;
    [SerializeField] public int HighestChunkGrouping;

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
    public TreeNode HeightMapTree = new TreeNode();


    public Dictionary<(Vector2,int), Chunk> ChunkDictionary = new Dictionary<(Vector2,int), Chunk>();
    private Dictionary<Vector3, GameObject> ChunkObjectDictionary = new Dictionary<Vector3, GameObject>();

    // Queues
    public Queue<Vector2> HeightmapGenQueue = new Queue<Vector2>();
    // public Queue<Vector2> ChunkUpdateRequestQueue = new Queue<Vector2>();
    public Queue<TreeNode> ChunkUpdateRequestQueue = new Queue<TreeNode>();

    public Queue<ChunkUpdate> MeshQueue = new Queue<ChunkUpdate>();


    // Vector2.one so that the chunks update one the player is unlocked
    public Vector2 PastChunkPosition = Vector2.one;
    public bool GenerationComplete = false;


    [SerializeField] public ComputeShader HeightMapShader;
    public SeedGenerator SeedGenerator;

    public Dictionary<Vector2, TreeNode> LeafDict = new Dictionary<Vector2, TreeNode>();

    public Vector2[] CellCord = new Vector2[10];

    public MeshBuilder meshBuilder;


    private Vector2 CurrentCell;
    // Vector3 -> Vector2
    // z -> y
    // V3(x,y,z) -> V2(x,z)
    void Start()
    {
        meshBuilder = new MeshBuilder(ChunkSettings);
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

        // populating tree with empty childs
        for (int x = -ChunkRenderDistance / HighestChunkGrouping; x < ChunkRenderDistance / HighestChunkGrouping; x++)
        {
            for (int y = -ChunkRenderDistance / HighestChunkGrouping; y < ChunkRenderDistance / HighestChunkGrouping; y++)
            {
                TreeNode p = HeightMapTree.AddChild(new Vector2(x, y) * HighestChunkGrouping);
                p.Spread(HighestChunkGrouping / 2, LeafDict);
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
            // Selecting which chunks to update
            // This will be replace with quad tree division
            Vector2 currentChunkPosition = new Vector2(Mathf.Round(Tracker.position.x / ChunkSettings.size), Mathf.Round(Tracker.position.z / ChunkSettings.size));
            if (currentChunkPosition != PastChunkPosition)
            {
                PastChunkPosition = currentChunkPosition;
                // for (int x = -34; x <= 34; x++)
                // {
                //     for (int y = -34; y <= 34; y++)
                //     {
                //         Vector2 sampler = currentChunkPosition + new Vector2(x, y);
                //         if (Math.Abs(sampler.x) < ChunkRenderDistance && Math.Abs(sampler.y) < ChunkRenderDistance)
                //             lock (ChunkUpdateRequestQueue)
                //                 ChunkUpdateRequestQueue.Enqueue(sampler);
                //     }
                // }

                TreeNode layerParent = HeightMapTree;
                TreeNode tmp;
                int k = 0;
                for (int i = HighestChunkGrouping; i >= 1; i /= 2)
                {
                    if (Math.Abs(currentChunkPosition.x) >= ChunkRenderDistance && Math.Abs(currentChunkPosition.y) >= ChunkRenderDistance){
                        continue;
                    }
                    Vector2 layerPosition = new Vector2(
                        Mathf.Floor(currentChunkPosition.x / i) * i,
                        Mathf.Floor(currentChunkPosition.y / i) * i
                    );
                    
                    Debug.Log(layerPosition);
                    CellCord[k++] = layerPosition;

                    tmp = layerParent.Children[layerPosition];

                    foreach (var branch in layerParent.Children.Values)
                    {
                        if (branch != tmp)
                        {
                            ChunkUpdateRequestQueue.Enqueue(branch);
                        }
                    }
                    layerParent = tmp;
                }
                // Debug.Log(layerParent.Position);
            }

            // Rendering chunks
            int hold = MeshQueue.Count;
            for (int f = 0; f < hold; f++)
            {
                ChunkUpdate updateMeshData;
                lock (MeshQueue)
                    updateMeshData = MeshQueue.Dequeue();
                UpdateChunk(updateMeshData.meshData);
            }

            // Drawing trees
            foreach (Chunk chunkInstance in ChunkDictionary.Values)
            {
                if (chunkInstance.nodeDepth > 4)
                {
                    Graphics.DrawMeshInstanced(TreeMesh, 0, TreeMaterial, chunkInstance.treesTransforms);
                }
            }
        }
        else
            Progress = (float)HeightMapDict.Count / math.pow(ChunkRenderDistance * 2, 2);
    }

    GameObject UpdateChunk(MeshData meshData)
    {
        GameObject chunk;
        Vector3 key = new Vector3(
            meshData.basedOn.Position.x,
            meshData.basedOn.NodeDepth(),
            meshData.basedOn.Position.y
        );

        if (ChunkObjectDictionary.ContainsKey(key))
        {
            chunk = ChunkObjectDictionary[key];
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
            chunk.transform.position = new Vector3(meshData.basedOn.Position.x, 0, meshData.basedOn.Position.y) * ChunkSettings.size;
            chunk.transform.name = key.ToString();

            MeshFilter meshFilter = chunk.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = chunk.AddComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;

            Mesh mesh = new Mesh();
            mesh.vertices = meshData.vertexList;
            mesh.triangles = meshData.triangleList;
            mesh.RecalculateNormals();

            meshFilter.mesh = mesh;

            ChunkObjectDictionary.Add(key, chunk);
        }

        chunk.GetComponent<MeshRenderer>().material = DefaultMaterial;
        return chunk;
    }

    void OnDrawGizmos()
    {
        if (DrawChunkBorders && GenerationComplete)
        {
            for (int i = 0; i < CellCord.Length; i++)
            {
                
            }
            foreach (var item in HeightMapTree.Children)
            {
                // Vector2 flatPosition = item.Value.Position;
                // Vector3 spacePosition = new Vector3(flatPosition.x, 0, flatPosition.y) * ChunkSettings.size;
                // spacePosition += new Vector3(HighestChunkGrouping * ChunkSettings.size / 2, 0, HighestChunkGrouping * ChunkSettings.size / 2);

                
                Gizmos.color = Color.blue;                
                int index = 0;
                for (int i = HighestChunkGrouping; i >= 1; i /= 2)
                {
                    Vector3 spacePosition = new Vector3(CellCord[index].x, 0, CellCord[index].y) * ChunkSettings.size;
                    spacePosition += new Vector3(i * ChunkSettings.size / 2, 0, i * ChunkSettings.size / 2);
   
                    Gizmos.DrawWireCube(
                        spacePosition,
                        new Vector3(i * ChunkSettings.size, MaxTerrainHeight, i * ChunkSettings.size)
                    );                    
                    index++;
                }
                // if (flatPosition == CurrentCell)
                // {
                //     Gizmos.color = Color.yellow;
                //     Gizmos.DrawWireCube(
                //         spacePosition,
                //         new Vector3(HighestChunkGrouping * ChunkSettings.size, MaxTerrainHeight, HighestChunkGrouping * ChunkSettings.size)
                //     );

                //     Gizmos.color *= new Color(1, 1, 1, 0.25f);
                //     Gizmos.DrawCube(
                //         spacePosition,
                //         new Vector3(HighestChunkGrouping * ChunkSettings.size, MaxTerrainHeight, HighestChunkGrouping * ChunkSettings.size)
                //     );
                // }
                // else
                // {
                //     Gizmos.color = Color.black;
                //     Gizmos.DrawWireCube(
                //         spacePosition,
                //         new Vector3(HighestChunkGrouping * ChunkSettings.size, MaxTerrainHeight, HighestChunkGrouping * ChunkSettings.size)
                //     );
                // }
            }
        }
    }
}