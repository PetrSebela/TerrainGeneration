using System.Collections.Generic;
using UnityEngine;
using System;
using System.Collections;
using System.Threading;
using Unity.Mathematics;

public class ChunkManager : MonoBehaviour
{
    [Header("Chunk setting")]
    [SerializeField] private int chunkResolution;
    [SerializeField] private float chunkSize;
    [SerializeField] private Material defaultMaterial;

    [Header("World setting")]
    [SerializeField] private int chunkRenderDistance;

    [Header("Terrain")]
    [SerializeField] private float maxTerrainHeight;
    [SerializeField] private Transform tracker;

    private HeightMapGenerator heightMapGenerator;
    private Queue<ChunkUpdate> meshQueue = new Queue<ChunkUpdate>();
    private Dictionary<Vector2, Chunk> chunkDictionary = new Dictionary<Vector2, Chunk>();
    private Dictionary<Vector3, GameObject> chunkObjectDictionary = new Dictionary<Vector3, GameObject>();

    private bool drawChunkBorders = false;
    private Thread[] HeighMapThreads;
    private Thread[] MeshConstructorThreads;

    public bool prerenderDone = false;
    public float progress = 0;

    // ! --- current working with  ---
    private Dictionary<Vector2, float[,]> heightMapDict = new Dictionary<Vector2, float[,]>();
    private Queue<Vector2> heightmapGenQueue = new Queue<Vector2>();
    private Queue<Vector2> chunkUpdateRequestQueue = new Queue<Vector2>();
    private Vector2 pastChunkChecker;
    public bool generationComplete = false;

    [SerializeField] public ComputeShader heightMapShader;
    SeedGenerator seedGenerator;

    // Vector3 -> Vector2
    // z -> y
    // V3(x,y,z) -> V2(x,z)
    void Start()
    {
        seedGenerator = new SeedGenerator(123);

        pastChunkChecker = Vector3.one;
        tracker.position = new Vector3(0, maxTerrainHeight, 0);
        // heightMapGenerator = new HeightMapGenerator(chunkRenderDistance, chunkSize, chunkResolution);

        // sampling terrain chunks
        for (int x = -chunkRenderDistance; x < chunkRenderDistance; x++)
        {
            for (int y = -chunkRenderDistance; y < chunkRenderDistance; y++)
            {
                Vector2 sampler = new Vector2(x, y);
                heightmapGenQueue.Enqueue(sampler);
            }
        }

        StartCoroutine(ThreadDispatcher());
    }

    IEnumerator ThreadDispatcher()
    {
        ComputeBuffer heightMapBuffer = new ComputeBuffer((int)Mathf.Pow(64 + 4, 2), sizeof(float));
        ComputeBuffer offsets = new ComputeBuffer(24, sizeof(float) * 2);

        offsets.SetData(seedGenerator.noiseLayers);

        while (heightmapGenQueue.Count > 0)
        {
            Vector2 toGenerate;

            lock (heightmapGenQueue)
                toGenerate = heightmapGenQueue.Dequeue();

            float[,] heightMap = new float[68, 68];

            heightMapBuffer.SetData(heightMap);

            heightMapShader.SetVector("offset", toGenerate);
            heightMapShader.SetBuffer(0, "heightMap", heightMapBuffer);
            heightMapShader.SetBuffer(0, "layerOffsets", offsets);

            heightMapShader.Dispatch(0, 17, 17, 1);
            heightMapBuffer.GetData(heightMap);

            lock (heightMapDict)
                heightMapDict.Add(toGenerate, heightMap);

            Chunk chunk = new Chunk(heightMap, new Vector3(toGenerate.x, 0, toGenerate.y), chunkSize, chunkResolution);
            lock (chunkDictionary)
                chunkDictionary.Add(toGenerate, chunk);

            if (heightmapGenQueue.Count % 32 == 0)
                yield return null;
        }

        heightMapBuffer.Dispose();
        offsets.Dispose();

        ThreadStart meshConstructorThread = delegate
        {
            ConstructChunkMesh_T();
        };

        MeshConstructorThreads = new Thread[6];
        //these threads will be running forever
        for (int i = 0; i < 1; i++)
        {
            Thread thread = new Thread(meshConstructorThread);
            thread.Start();
            MeshConstructorThreads[i] = thread;
        }

        for (int x = -chunkRenderDistance; x < chunkRenderDistance; x++)
        {
            for (int y = -chunkRenderDistance; y < chunkRenderDistance; y++)
            {
                Vector2 sampler = new Vector2(x, y);
                lock (chunkUpdateRequestQueue)
                    chunkUpdateRequestQueue.Enqueue(sampler);
            }
        }

        while (chunkUpdateRequestQueue.Count > 0)
        {
            yield return null;
        }

        generationComplete = true;
        Debug.Log("World generation and prerender corutine complete");
    }

    void Update()
    {
        // generating chunk update requests
        if (generationComplete == true)
        {
            Vector2 chunkChecker = new Vector2(Mathf.Round(tracker.position.x / chunkSize), Mathf.Round(tracker.position.z / chunkSize));
            if (chunkChecker != pastChunkChecker)
            {
                pastChunkChecker = chunkChecker;
                for (int x = -34; x <= 34; x++)
                {
                    for (int y = -34; y <= 34; y++)
                    {
                        Vector2 sampler = chunkChecker + new Vector2(x, y);
                        if (Math.Abs(sampler.x) < chunkRenderDistance && Math.Abs(sampler.y) < chunkRenderDistance)
                            lock (chunkUpdateRequestQueue)
                                chunkUpdateRequestQueue.Enqueue(sampler);
                    }
                }
            }
        }
        else
            progress = (float)heightMapDict.Count / math.pow(chunkRenderDistance * 2, 2);
        // if (Input.GetKeyDown(KeyCode.G))
        //     drawChunkBorders = !drawChunkBorders;


        int hold = meshQueue.Count;

        for (int f = 0; f < hold; f++)
        {
            ChunkUpdate meshData;

            lock (meshQueue)
                meshData = meshQueue.Dequeue();
            GameObject chunk = CreateTerrainChunk(meshData.meshData, meshData.LODindex);
        }
    }

    void ConstructChunkMesh_T()
    {
        while (true)
        {
            Vector2? toGenerateNull = null;

            lock (chunkUpdateRequestQueue)
            {
                if (chunkUpdateRequestQueue.Count != 0)
                    toGenerateNull = chunkUpdateRequestQueue.Dequeue();
            }

            if (toGenerateNull != null)
            {
                Vector2 toGenerate = (Vector2)toGenerateNull;
                // lock (chunkUpdateRequestQueue)
                //     toGenerate = chunkUpdateRequestQueue.Dequeue();

                toGenerate -= pastChunkChecker;

                int LODindex;
                if (Math.Abs(toGenerate.x) >= 32 || Math.Abs(toGenerate.y) >= 32)
                    LODindex = 32;
                else if (Math.Abs(toGenerate.x) >= 24 || Math.Abs(toGenerate.y) >= 24)
                    LODindex = 16;
                else if (Math.Abs(toGenerate.x) >= 12 || Math.Abs(toGenerate.y) >= 12)
                    LODindex = 8;
                else if (Math.Abs(toGenerate.x) >= 6 || Math.Abs(toGenerate.y) >= 6)
                    LODindex = 4;
                else if (Math.Abs(toGenerate.x) >= 3 || Math.Abs(toGenerate.y) >= 3)
                    LODindex = 2;
                else
                    LODindex = 1;

                // getting border vector
                Vector2 borderVector = Vector2.zero;
                // distance - 1
                // int[] borderNumbers = new int[] { 2, 3, 4, 5, 8 };
                int[] borderNumbers = new int[] { 2, 5, 11, 23, 31 };

                for (int i = 0; i < borderNumbers.Length; i++)
                {
                    if (toGenerate.y == borderNumbers[i] && toGenerate.x <= borderNumbers[i] && toGenerate.x >= -borderNumbers[i])
                        borderVector.x = (borderVector.x == 0) ? 1 : borderVector.x;

                    if (toGenerate.y == -borderNumbers[i] && toGenerate.x <= borderNumbers[i] && toGenerate.x >= -borderNumbers[i])
                        borderVector.x = (borderVector.x == 0) ? -1 : borderVector.x;

                    if (toGenerate.x == borderNumbers[i] && toGenerate.y <= borderNumbers[i] && toGenerate.y >= -borderNumbers[i])
                        borderVector.y = (borderVector.y == 0) ? 1 : borderVector.y;

                    if (toGenerate.x == -borderNumbers[i] && toGenerate.y <= borderNumbers[i] && toGenerate.y >= -borderNumbers[i])
                        borderVector.y = (borderVector.y == 0) ? -1 : borderVector.y;
                }

                toGenerate += pastChunkChecker;

                if (chunkDictionary[toGenerate].CurrentLODindex != LODindex || chunkDictionary[toGenerate].borderVector != borderVector)
                {
                    MeshData meshData = chunkDictionary[toGenerate].GetMeshData(LODindex, borderVector);
                    lock (meshQueue)
                    {
                        ChunkUpdate chunkUpdate = new ChunkUpdate(new Vector3(toGenerate.x, 0, toGenerate.y), meshData, LODindex);
                        meshQueue.Enqueue(chunkUpdate);
                    }
                }
            }
        }
    }

    GameObject CreateTerrainChunk(MeshData meshData, int LODindex)
    {
        GameObject chunk;
        if (chunkObjectDictionary.ContainsKey(meshData.position))
        {
            chunk = chunkObjectDictionary[meshData.position];
            Mesh mesh = chunk.GetComponent<MeshFilter>().mesh;
            mesh.Clear();
            mesh.vertices = meshData.vertexList;
            mesh.triangles = meshData.triangleList;
            mesh.RecalculateNormals();
            // chunk.GetComponent<MeshCollider>().sharedMesh = mesh;
        }
        else
        {
            chunk = new GameObject();
            chunk.layer = LayerMask.NameToLayer("Ground");
            chunk.isStatic = true;
            chunk.transform.parent = this.transform;
            chunk.transform.position = meshData.position * chunkSize;
            chunk.transform.name = meshData.position.ToString();

            MeshFilter meshFilter = chunk.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = chunk.AddComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;

            Mesh mesh = new Mesh();
            mesh.vertices = meshData.vertexList;
            mesh.triangles = meshData.triangleList;
            mesh.RecalculateNormals();

            meshFilter.mesh = mesh;
            // MeshCollider meshCollider = chunk.AddComponent<MeshCollider>();

            chunkObjectDictionary.Add(meshData.position, chunk);
        }

        chunk.GetComponent<MeshRenderer>().material = defaultMaterial;
        return chunk;
    }

    void OnDrawGizmos()
    {
        if (drawChunkBorders)
        {
            Vector3 chunkDimensions = new Vector3(chunkSize, maxTerrainHeight, chunkSize);
            try
            {
                foreach (var item in chunkDictionary.Values)
                {
                    switch (item.CurrentLODindex)
                    {
                        case 1:
                            Gizmos.color = new Color(0, 0, 1, 0.25f);
                            break;
                        case 2:
                            Gizmos.color = new Color(0, 0, 0.9f, 0.25f);
                            break;
                        case 4:
                            Gizmos.color = new Color(0, 0, 0.8f, 0.25f);
                            break;
                        case 8:
                            Gizmos.color = new Color(0, 0, 0.7f, 0.25f);
                            break;
                        default:
                            Gizmos.color = new Color(0, 0, 0, 0.25f);
                            break;
                    }
                    Gizmos.DrawWireCube(item.position * chunkSize + chunkDimensions / 2, chunkDimensions);
                    Gizmos.DrawCube(item.position * chunkSize + chunkDimensions / 2, chunkDimensions);
                }
            }
            catch { }
        }
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