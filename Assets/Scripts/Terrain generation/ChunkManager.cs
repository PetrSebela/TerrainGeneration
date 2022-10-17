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

    [SerializeField] private List<Vector3> _chunkPositionList = new List<Vector3>();
    [SerializeField] private Dictionary<Vector3, GameObject> _chunkDictionary = new Dictionary<Vector3, GameObject>();
    [SerializeField] private Dictionary<Vector3, float[,]> _chunkDataDict = new Dictionary<Vector3, float[,]>();

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

    private HeightMapGenerator _heightMapGenerator;
    private Queue<MeshBuildData> _meshQueue = new Queue<MeshBuildData>();
    private Queue<Vector3> _chunkQueue = new Queue<Vector3>();
    private int _layerMask;
    private bool _prerenderDone = false;
    private float totalChunkCount = 0;

    void Start()
    {
        _chunkRenderDistance++;
        _layerMask = LayerMask.GetMask("Ground");
        _heightMapGenerator = new HeightMapGenerator(_chunkRenderDistance, _chunkSize, _chunkResolution);
        // Generating world seed and starting generation thread
        ThreadStart threadStart = delegate
        {
            UpdateWorld();
        };
        for (int i = 0; i < 6; i++)
        {
            new Thread(threadStart).Start();
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

        // }
        // void FixedUpdate()
        // {
        // Chunks have their own coordinate system. 1 Chunk (0,0,0) = 1 Unit (0,0,1);  
        // Vector3 trackerPosition = Vector3.zero;
        Vector3 chunkChecker = new Vector3(Mathf.Round(_tracker.position.x / _chunkSize), 0, Mathf.Round(_tracker.position.z / _chunkSize));

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
                if (!_chunkPositionList.Contains(sampler))
                {
                    lock (_chunkPositionList)
                        _chunkPositionList.Add(sampler);
                    lock (_chunkQueue)
                        _chunkQueue.Enqueue(sampler);
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

    void Update()
    {
        float progress = ((float)totalChunkCount / ((_chunkRenderDistance * _chunkRenderDistance))) * 100;

        Debug.Log("Progress ->" + progress.ToString());

        if (_prerenderDone || _meshQueue.Count >= (_prerenderDistance * _prerenderDistance))
        {
            _prerenderDone = true;
            int hold = _meshQueue.Count;
            for (int f = 0; f < hold; f++)
            {
                totalChunkCount++;
                MeshBuildData meshData;
                lock (_meshQueue)
                    meshData = _meshQueue.Dequeue();
                GameObject chunk = CreateTerrainMesh(meshData);
                _chunkDictionary.Add(meshData.position, chunk);

                if (_useTrees)
                {
                    for (int i = 0; i < _treesInChunk; i++)
                    {
                        RaycastHit hit;
                        Vector3 offset = new Vector3(UnityEngine.Random.Range(0, _chunkSize), _maxTerrainHeight * 1.5f, UnityEngine.Random.Range(0, _chunkSize));
                        if (Physics.Raycast(new Vector3(meshData.position.x, 0, meshData.position.z) * _chunkSize + offset, Vector3.down, out hit, Mathf.Infinity, _layerMask))
                        {
                            if (hit.point.y >= _maxTerrainHeight * _waterHeight && hit.point.y >= _treeSpawnHeight.from && hit.point.y <= _treeSpawnHeight.to && Vector3.Angle(hit.normal, Vector3.up) < 25f)
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

    void UpdateWorld()
    {
        while (true)
        {
            Vector3? toGenerate = null;
            lock (_chunkQueue)
            {
                if (_chunkQueue.Count != 0)
                    toGenerate = _chunkQueue.Dequeue();
            }
            if (toGenerate != null)
            {
                Vector3 toGen = (Vector3)toGenerate;
                MeshBuildData meshData;
                if (Vector3.Distance(toGen * _chunkSize, Vector3.zero) >= 3072)
                {
                    float[,] heightMap = _heightMapGenerator.SampleChunkData(toGen, _chunkResolution, _chunkSize, _maxTerrainHeight);
                    meshData = MeshConstructor.ConstructTerrain(heightMap, toGen, _chunkSize, _chunkResolution, 8);
                }
                else if (Vector3.Distance(toGen * _chunkSize, Vector3.zero) >= 2048)
                {
                    float[,] heightMap = _heightMapGenerator.SampleChunkData(toGen, _chunkResolution, _chunkSize, _maxTerrainHeight);
                    meshData = MeshConstructor.ConstructTerrain(heightMap, toGen, _chunkSize, _chunkResolution, 4);
                }
                else if (Vector3.Distance(toGen * _chunkSize, Vector3.zero) >= 1024)
                {
                    float[,] heightMap = _heightMapGenerator.SampleChunkData(toGen, _chunkResolution, _chunkSize, _maxTerrainHeight);
                    meshData = MeshConstructor.ConstructTerrain(heightMap, toGen, _chunkSize, _chunkResolution, 2);
                }
                else
                {
                    float[,] heightMap = _heightMapGenerator.SampleChunkData(toGen, _chunkResolution, _chunkSize, _maxTerrainHeight);
                    meshData = MeshConstructor.ConstructTerrain(heightMap, toGen, _chunkSize, _chunkResolution, 1);
                }
                lock (_meshQueue)
                    _meshQueue.Enqueue(meshData);
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

    GameObject CreateTerrainMesh(MeshBuildData meshData)
    {
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = meshData.vertexList;
        mesh.triangles = meshData.triangleList;
        mesh.RecalculateNormals();
        GameObject chunk = new GameObject();
        chunk.layer = LayerMask.NameToLayer("Ground");
        chunk.isStatic = true;
        chunk.transform.parent = this.transform;
        chunk.transform.position = meshData.position * _chunkSize;
        chunk.transform.name = meshData.position.ToString();
        MeshFilter meshFilter = chunk.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = chunk.AddComponent<MeshRenderer>();
        meshFilter.mesh = mesh;
        MeshCollider meshCollider = chunk.AddComponent<MeshCollider>();
        meshRenderer.material = _defaultMaterial;
        return chunk;
    }

    void OnDrawGizmos()
    {
        Vector3 chunkDimensions = new Vector3(_chunkSize, _maxTerrainHeight, _chunkSize);
        Gizmos.color = Color.blue;
        foreach (var item in _chunkPositionList)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(item * _chunkSize + chunkDimensions / 2, chunkDimensions);
            Gizmos.color = new Color(0, 0, 1, 0.25f);
            Gizmos.DrawCube(item * _chunkSize + chunkDimensions / 2, chunkDimensions);
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
