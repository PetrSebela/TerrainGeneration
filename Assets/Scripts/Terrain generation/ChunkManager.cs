using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
using Unity.Mathematics;

public class ChunkManager : MonoBehaviour
{
    [Header("Chunk setting")]
    [SerializeField] private int _chunkResolution;
    [SerializeField] private float _chunkSize;
    [SerializeField] private Material _defaultMaterial;

    [Header("World setting")]
    [SerializeField] private int _chunkRenderDistance;
    [SerializeField] private int _prerenderDistance;

    [Header("Water")]
    [SerializeField] private bool _useWater;
    [SerializeField] private float _waterHeight;
    [SerializeField] private int _waterChunkRenderDistance;
    [SerializeField] private float _waterChunkSize;
    [SerializeField] private Material _waterMaterial;

    private List<Vector3> _waterChunkList = new List<Vector3>();

    [Header("Terrain")]
    [SerializeField] private float _maxTerrainHeight;
    [SerializeField] private Transform _tracker;

    [Header("Trees")]
    [SerializeField] private int _treesInChunk;
    [SerializeField] private Range _treeSpawnHeight;
    [SerializeField] private GameObject _treeModel;
    [SerializeField] private bool _useTrees;

    public Transform _player;
    private HeightMapGenerator _heightMapGenerator;
    private Queue<ChunkUpdate> _meshQueue = new Queue<ChunkUpdate>();
    private Queue<Vector3> _chunkUpdateRequestQueue = new Queue<Vector3>();

    private Dictionary<Vector3, Chunk> _chunkDictionary = new Dictionary<Vector3, Chunk>();
    private Dictionary<Vector3, GameObject> _chunkObjectDictionary = new Dictionary<Vector3, GameObject>();

    private int _layerMask;
    private bool _prerenderDone = false;
    private float totalChunkCount = 0;

    private bool _drawChunkBorders = false;
    private Thread[] _threads;

    private Vector3 pastChunkChecker;

    void Start()
    {
        pastChunkChecker = Vector3.one;

        _player.position = new Vector3(0, _maxTerrainHeight, 0);
        _chunkRenderDistance++;
        _layerMask = LayerMask.GetMask("Ground");
        _heightMapGenerator = new HeightMapGenerator(_chunkRenderDistance, _chunkSize, _chunkResolution);

        _threads = new Thread[6];

        ThreadStart threadStart = delegate
        {
            UpdateWorld();
        };

        for (int i = 0; i < 6; i++)
        {
            Thread thread = new Thread(threadStart);
            thread.Start();
            _threads[i] = thread;
        }

        // rendering water chunks
        if (_useWater)
        {
            for (int x1 = _waterChunkRenderDistance / -2; x1 < _waterChunkRenderDistance / 2; x1++)
            {
                for (int y1 = _waterChunkRenderDistance / -2; y1 < _waterChunkRenderDistance / 2; y1++)
                {
                    Vector3 sampler = new Vector3(x1, 0, y1);
                    if (!_waterChunkList.Contains(sampler))
                    {
                        CreateWaterChunk(new Vector3(x1 + 0.5f, 0, y1 + 0.5f) * _waterChunkSize + new Vector3(0, _maxTerrainHeight * _waterHeight, 0));
                        _waterChunkList.Add(new Vector3(x1, 0, y1));
                    }
                }
            }
        }

        // Chunks have their own coordinate system. 1 Chunk (0,0,0) = 1 Unit (0,0,1);  
        // Vector3 trackerPosition = Vector3.zero;
    }
    void FixedUpdate()
    {
        Vector3 chunkChecker = new Vector3(Mathf.Round(_tracker.position.x / _chunkSize), 0, Mathf.Round(_tracker.position.z / _chunkSize));
        if (chunkChecker != pastChunkChecker)
        {
            pastChunkChecker = chunkChecker;

            int X = _chunkRenderDistance;
            int Y = _chunkRenderDistance;

            int x, y, dx, dy;
            x = y = dx = 0;
            dy = -1;
            int t = math.max(X, Y);
            int maxI = t * t;

            for (int i = 0; i < maxI; i++)
            {
                if ((-X / 2 <= x) && (x <= X / 2) && (-Y / 2 <= y) && (y <= Y / 2))
                {
                    Vector3 sampler = chunkChecker + new Vector3(x, 0, y);
                    if (Math.Abs(sampler.x) < _chunkRenderDistance / 2 && Math.Abs(sampler.z) < _chunkRenderDistance / 2)
                    {
                        lock (_chunkUpdateRequestQueue)
                            _chunkUpdateRequestQueue.Enqueue(sampler);
                    }

                }

                if ((x == y) || ((x < 0) && (x == -y)) || ((x > 0) && (x == 1 - y)))
                {
                    t = dx;
                    dx = -dy;
                    dy = t;
                }

                x += dx;
                y += dy;
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
            _drawChunkBorders = !_drawChunkBorders;

        float progress = ((float)totalChunkCount / ((_chunkRenderDistance * _chunkRenderDistance))) * 100;

        if (_prerenderDone || _meshQueue.Count >= (_prerenderDistance * _prerenderDistance))
        {
            _prerenderDone = true;

            int hold = _meshQueue.Count;
            for (int f = 0; f < hold; f++)
            {
                totalChunkCount++;
                ChunkUpdate meshData;

                lock (_meshQueue)
                    meshData = _meshQueue.Dequeue();
                GameObject chunk = CreateTerrainChunk(meshData.meshData, meshData.LODindex);

                if (_useTrees)
                {
                    for (int i = 0; i < _treesInChunk; i++)
                    {
                        RaycastHit hit;
                        Vector3 offset = new Vector3(UnityEngine.Random.Range(0, _chunkSize), _maxTerrainHeight * 1.5f, UnityEngine.Random.Range(0, _chunkSize));
                        if (Physics.Raycast(new Vector3(meshData.position.x, 0, meshData.position.z) * _chunkSize + offset, Vector3.down, out hit, Mathf.Infinity, _layerMask))
                        {
                            if (hit.point.y >= _maxTerrainHeight * _waterHeight && hit.point.y >= _treeSpawnHeight.from * _maxTerrainHeight && hit.point.y <= _treeSpawnHeight.to * _maxTerrainHeight && Vector3.Angle(hit.normal, Vector3.up) < 25f)
                            {
                                if (Unity.Mathematics.noise.snoise(new float2(hit.point.x * 0.00025f, hit.point.z * 0.00025f)) >= UnityEngine.Random.Range(-0.9f, 0.9f))
                                {
                                    GameObject tree = Instantiate(_treeModel);
                                    tree.transform.position = hit.point;
                                    tree.transform.parent = chunk.transform;
                                    float scale = UnityEngine.Random.Range(3.1f, 5f);
                                    tree.transform.localScale = new Vector3(scale, scale, scale);
                                    tree.transform.localScale = new Vector3(3.5f, 3.5f, 3.5f);
                                    tree.isStatic = true;
                                }
                            }
                        }
                    }
                }
            }
        }
    }


    // this method is running in threading 
    void UpdateWorld()
    {
        while (true)
        {
            Vector3? toGenerateNull = null;

            lock (_chunkUpdateRequestQueue)
            {
                if (_chunkUpdateRequestQueue.Count != 0)
                    toGenerateNull = _chunkUpdateRequestQueue.Dequeue();
            }

            if (toGenerateNull != null)
            {
                Vector3 toGenerate = (Vector3)toGenerateNull;


                if (!_chunkDictionary.ContainsKey(toGenerate))
                {
                    float[,] heightMap = _heightMapGenerator.SampleChunkData(toGenerate, _chunkResolution, _chunkSize, _maxTerrainHeight);
                    Chunk chunk = new Chunk(heightMap, toGenerate, _chunkSize, _chunkResolution);
                    lock (_chunkDictionary)
                        _chunkDictionary.Add(toGenerate, chunk);
                }

                // getting LOD index
                // subtracting pastChunkCheckt in order to be everything centered around [0,0]
                toGenerate -= pastChunkChecker;

                int LODindex;

                if (Math.Abs(toGenerate.x) >= 5 || Math.Abs(toGenerate.z) >= 5)
                    LODindex = 8;
                else if (Math.Abs(toGenerate.x) >= 4 || Math.Abs(toGenerate.z) >= 4)
                    LODindex = 4;
                else if (Math.Abs(toGenerate.x) >= 3 || Math.Abs(toGenerate.z) >= 3)
                    LODindex = 2;
                else
                    LODindex = 1;

                // getting border vector
                Vector2 borderVector = Vector2.zero;
                int[] borderNumbers = new int[] { 2, 3, 4 };

                for (int i = 0; i < borderNumbers.Length; i++)
                {
                    if (toGenerate.z == borderNumbers[i] && toGenerate.x <= borderNumbers[i] && toGenerate.x >= -borderNumbers[i])
                        borderVector.x = (borderVector.x == 0) ? 1 : borderVector.x;

                    if (toGenerate.z == -borderNumbers[i] && toGenerate.x <= borderNumbers[i] && toGenerate.x >= -borderNumbers[i])
                        borderVector.x = (borderVector.x == 0) ? -1 : borderVector.x;

                    if (toGenerate.x == borderNumbers[i] && toGenerate.z <= borderNumbers[i] && toGenerate.z >= -borderNumbers[i])
                        borderVector.y = (borderVector.y == 0) ? 1 : borderVector.y;

                    if (toGenerate.x == -borderNumbers[i] && toGenerate.z <= borderNumbers[i] && toGenerate.z >= -borderNumbers[i])
                        borderVector.y = (borderVector.y == 0) ? -1 : borderVector.y;
                }

                toGenerate += pastChunkChecker;



                if (_chunkDictionary[toGenerate].CurrentLODindex != LODindex || _chunkDictionary[toGenerate].BorderVector != borderVector)
                {
                    MeshData meshData = _chunkDictionary[toGenerate].GetMeshData(LODindex, borderVector);
                    lock (_meshQueue)
                    {
                        ChunkUpdate chunkUpdate = new ChunkUpdate(toGenerate, meshData, LODindex);
                        _meshQueue.Enqueue(chunkUpdate);
                    }
                }
            }
        }
    }

    void CreateWaterChunk(Vector3 position)
    {
        GameObject waterChunk = GameObject.CreatePrimitive(PrimitiveType.Plane);
        waterChunk.transform.localScale = new Vector3(_waterChunkSize / 10, 1, _waterChunkSize / 10);
        waterChunk.transform.position = position;
        waterChunk.GetComponent<MeshRenderer>().material = _waterMaterial;
        waterChunk.layer = LayerMask.NameToLayer("Water");
        Destroy(waterChunk.GetComponent<MeshCollider>());
    }

    GameObject CreateTerrainChunk(MeshData meshData, int LODindex)
    {
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = meshData.vertexList;
        mesh.triangles = meshData.triangleList;
        mesh.RecalculateNormals();

        GameObject chunk;
        if (!_chunkObjectDictionary.ContainsKey(meshData.position))
        {
            chunk = new GameObject();
            chunk.layer = LayerMask.NameToLayer("Ground");
            chunk.isStatic = true;
            chunk.transform.parent = this.transform;
            chunk.transform.position = meshData.position * _chunkSize;
            chunk.transform.name = meshData.position.ToString();

            MeshFilter meshFilter = chunk.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = chunk.AddComponent<MeshRenderer>();
            meshFilter.mesh = mesh;
            _chunkObjectDictionary.Add(meshData.position, chunk);
        }
        else
        {
            chunk = _chunkObjectDictionary[meshData.position];
            chunk.GetComponent<MeshFilter>().mesh = mesh;
        }

        if (chunk.GetComponent<MeshCollider>() != null && LODindex > 1)
            Destroy(chunk.GetComponent<MeshCollider>());

        if (chunk.GetComponent<MeshCollider>() == null && LODindex == 1)
            chunk.AddComponent<MeshCollider>();



        Material mat = new Material(_defaultMaterial);
        mat.color = Color.white;
        chunk.GetComponent<MeshRenderer>().material = mat;
        return chunk;
    }

    void OnDrawGizmos()
    {
        if (_drawChunkBorders)
        {
            Vector3 chunkDimensions = new Vector3(_chunkSize, _maxTerrainHeight, _chunkSize);
            try
            {
                foreach (var item in _chunkDictionary.Values)
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
                    Gizmos.DrawWireCube(item.Position * _chunkSize + chunkDimensions / 2, chunkDimensions);
                    Gizmos.DrawCube(item.Position * _chunkSize + chunkDimensions / 2, chunkDimensions);
                }
            }
            catch { }
        }
    }
}

[System.Serializable]
public struct NoiseLayer
{
    public string name;
    public float scale;
    public float weight;
    [HideInInspector] public Vector2 offset;

    public NoiseLayer(string name, float scale, float weight)
    {
        this.name = name;
        this.scale = scale;
        this.weight = weight;
        this.offset = Vector2.zero;
    }
}

[System.Serializable]
public struct Range
{
    public float from;
    public float to;

    public Range(float from, float to)
    {
        this.from = from;
        this.to = to;
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
