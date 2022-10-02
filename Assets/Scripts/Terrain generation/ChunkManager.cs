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
    [SerializeField] private int _renderDistance;

    [SerializeField] private List<Vector3> _chunkPositionList = new List<Vector3>();
    [SerializeField] private Dictionary<Vector3, GameObject> _ChunkDictionary = new Dictionary<Vector3, GameObject>();

    [Header("Water")]
    [SerializeField] private bool _useWater;
    [SerializeField] private float _waterLevel;
    [SerializeField] private int _waterChunkRenderDistance;
    [SerializeField] private float _waterChunkSize;
    [SerializeField] private Material _waterMaterial;

    private List<Vector3> _waterChunkDictionary = new List<Vector3>();

    [Header("Terrain")]
    [SerializeField] private float _maxHeight;
    [SerializeField] private Transform tracker;
    [SerializeField] private AnimationCurve _terrainMapping;

    [Header("Trees")]
    [SerializeField] private int _treesPerChunk;
    [SerializeField] private Range _treeExistanceHeights;
    [SerializeField] private GameObject _treeModel;
    [SerializeField] private bool _useTrees;

    [Header("Preview")]
    public int _previewSize;
    public Texture2D texture;

    private HeightMapGenerator heightMapGenerator;
    private Queue<MeshBuildData> meshQueue = new Queue<MeshBuildData>();
    private Queue<Vector3> chunkQueue = new Queue<Vector3>();
    private int _layerMask;


    private float max;
    private float low;
    // test

    void Start()
    {
        _layerMask = LayerMask.GetMask("Ground");
        heightMapGenerator = new HeightMapGenerator(_renderDistance, _chunkSize, _chunkResolution);
        texture = heightMapGenerator._fallOffMap.GetTexture();
        // Generating world seed and starting generation thread
        ThreadStart threadStart = delegate
        {
            UpdateWorld();
        };
        for (int i = 0; i < 3; i++)
        {
            new Thread(threadStart).Start();
        }
    }


    // Chunks have their own coordinate system. 1 Chunk (0,0,0) = 1 Unit (0,0,1);  
    void FixedUpdate()
    {
        // Vector3 trackerPosition = tracker.position;
        Vector3 trackerPosition = Vector3.zero;
        Vector3 chunkChecker = new Vector3(Mathf.Round(trackerPosition.x / _chunkSize), 0, Mathf.Round(trackerPosition.z / _chunkSize));
        int X = _renderDistance;
        int Y = _renderDistance;

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
                    lock (chunkQueue)
                        chunkQueue.Enqueue(sampler);
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


        // Water chunks
        if (_useWater)
        {
            Vector3 waterChunkChecker = new Vector3(Mathf.Round(trackerPosition.x / _waterChunkSize), 0, Mathf.Round(trackerPosition.z / _waterChunkSize));
            for (int x1 = _waterChunkRenderDistance / -2; x1 < _waterChunkRenderDistance / 2; x1++)
            {
                for (int y1 = _waterChunkRenderDistance / -2; y1 < _waterChunkRenderDistance / 2; y1++)
                {
                    Vector3 sampler = waterChunkChecker + new Vector3(x1, 0, y1);
                    if (!_waterChunkDictionary.Contains(sampler))
                    {
                        CreateWaterChunk(waterChunkChecker + new Vector3(x1 + 0.5f, 0, y1 + 0.5f) * _waterChunkSize + new Vector3(0, _maxHeight * _waterLevel, 0));
                        _waterChunkDictionary.Add(waterChunkChecker + new Vector3(x1, 0, y1));
                    }
                }
            }
        }
    }

    void Update()
    {
        if (meshQueue.Count > 0)
        {
            for (int f = 0; f < meshQueue.Count; f++)
            {
                // Creating chunk GameObject
                MeshBuildData meshData = meshQueue.Dequeue();
                GameObject chunk = CreateTerrainMesh(meshData);
                _ChunkDictionary.Add(meshData.position, chunk);


                if (_useTrees)
                {
                    for (int i = 0; i < _treesPerChunk; i++)
                    {
                        RaycastHit hit;
                        Vector3 offset = new Vector3(UnityEngine.Random.Range(0, _chunkSize), _maxHeight * 1.5f, UnityEngine.Random.Range(0, _chunkSize));
                        if (Physics.Raycast(new Vector3(meshData.position.x, 0, meshData.position.z) * _chunkSize + offset, Vector3.down, out hit, Mathf.Infinity, _layerMask))
                        {
                            if (hit.point.y >= _maxHeight * _waterLevel && hit.point.y >= _treeExistanceHeights.from && hit.point.y <= _treeExistanceHeights.to && Vector3.Angle(hit.normal, Vector3.up) < 25f)
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
            for (int i = 0; i < chunkQueue.Count; i++)
            {
                Vector3 toGenerate;
                lock (chunkQueue)
                    toGenerate = chunkQueue.Dequeue();

                MeshBuildData meshData;
                if (Vector3.Distance(toGenerate * _chunkSize, Vector3.zero) >= 3072)
                {
                    float[,] heightMap = heightMapGenerator.SampleChunkData(toGenerate, _chunkResolution, _chunkSize, _maxHeight, _terrainMapping, 8);
                    meshData = MeshConstructor.ConstructTerrain(heightMap, toGenerate, _chunkSize, _chunkResolution / 8);
                }
                else if (Vector3.Distance(toGenerate * _chunkSize, Vector3.zero) >= 2048)
                {
                    float[,] heightMap = heightMapGenerator.SampleChunkData(toGenerate, _chunkResolution, _chunkSize, _maxHeight, _terrainMapping, 4);
                    meshData = MeshConstructor.ConstructTerrain(heightMap, toGenerate, _chunkSize, _chunkResolution / 4);
                }
                else if (Vector3.Distance(toGenerate * _chunkSize, Vector3.zero) >= 1024)
                {
                    float[,] heightMap = heightMapGenerator.SampleChunkData(toGenerate, _chunkResolution, _chunkSize, _maxHeight, _terrainMapping, 2);
                    meshData = MeshConstructor.ConstructTerrain(heightMap, toGenerate, _chunkSize, _chunkResolution / 2);
                }
                else
                {
                    float[,] heightMap = heightMapGenerator.SampleChunkData(toGenerate, _chunkResolution, _chunkSize, _maxHeight, _terrainMapping, 1);
                    meshData = MeshConstructor.ConstructTerrain(heightMap, toGenerate, _chunkSize, _chunkResolution);
                }
                meshQueue.Enqueue(meshData);
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
        Vector3 chunkDimensions = new Vector3(_chunkSize, _maxHeight, _chunkSize);
        Gizmos.color = Color.blue;
        foreach (var item in _chunkPositionList)
        {
            Gizmos.DrawWireCube(item * _chunkSize + chunkDimensions / 2, chunkDimensions);
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
