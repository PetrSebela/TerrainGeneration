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

    [SerializeField] private int _unloadChunks;
    [SerializeField] private bool _unloadChunksFlag = true;

    [SerializeField] private List<Vector3> _chunkPositionList = new List<Vector3>();
    [SerializeField] private Dictionary<Vector3, GameObject> _ChunkDictionary = new Dictionary<Vector3, GameObject>();
    [SerializeField] private float _waterLevel;
    [SerializeField] private float _maxHeight;
    [SerializeField] private float _noiseScale;
    [SerializeField] private Transform tracker;
    [SerializeField] private AnimationCurve _terrainMapping;
    [SerializeField] private int _treesPerChunk;
    [SerializeField] private Range _treeExistanceHeights;
    [SerializeField] private GameObject _treeModel;
    [SerializeField] private bool _useTrees;

    [Header("Preview")]
    public int _previewSize;
    public Texture2D texture;

    private HeightMapGenerator heightMapGenerator;
    private Vector3 _worldSeed;
    private Queue<MeshBuildData> meshQueue = new Queue<MeshBuildData>();
    private Queue<Vector3> chunkQueue = new Queue<Vector3>();

    void Start()
    {

        heightMapGenerator = new HeightMapGenerator(_noiseScale, 0.005f, 8, 1.25f, 0.5f, _terrainMapping);

        //? height map preview
        texture = new Texture2D(_previewSize, _previewSize, TextureFormat.ARGB32, false);
        float[,] samples = heightMapGenerator.SampleChunkData(new Vector3(0, 0, 0), _previewSize, _previewSize * 32, 1, _terrainMapping);

        for (int x = 0; x < _previewSize; x++)
        {
            for (int y = 0; y < _previewSize; y++)
            {
                texture.SetPixel(x, y, new Color(samples[x, y], samples[x, y], samples[x, y]));
            }
        }
        texture.Apply();


        // Generating world seed and starting generation thread
        _worldSeed = new Vector3(UnityEngine.Random.Range(0, 10000), UnityEngine.Random.Range(0, 10000), UnityEngine.Random.Range(0, 10000));
        ThreadStart threadStart = delegate
        {
            UpdateWorld();
        };

        new Thread(threadStart).Start();
    }


    // Chunks have their own coordinate system. 1 Chunk (0,0,0) = 1 Unit (0,0,1);  
    void FixedUpdate()
    {
        Vector3 trackerPosition = tracker.position;
        Vector3 chunkChecker = new Vector3(Mathf.Round(trackerPosition.x / _chunkSize), 0, Mathf.Round(trackerPosition.z / _chunkSize));

        for (int x = _renderDistance / -2; x < _renderDistance / 2; x++)
        {
            for (int y = _renderDistance / -2; y < _renderDistance / 2; y++)
            {
                Vector3 sampler = chunkChecker + new Vector3(x, 0, y);
                if (!_chunkPositionList.Contains(sampler))
                {
                    _chunkPositionList.Add(sampler);
                    chunkQueue.Enqueue(sampler);
                }
            }
        }
    }

    void Update()
    {
        if (meshQueue.Count > 0)
        {
            MeshBuildData meshData = meshQueue.Dequeue();
            Mesh mesh = new Mesh();

            // GameObject waterPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            // waterPlane.transform.position = new Vector3(_chunkSize, _maxHeight * _waterLevel, _chunkSize) / 2 + meshData.position;
            // waterPlane.transform.localScale = new Vector3(_chunkSize, 0, _chunkSize);

            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = meshData.vertexList;
            mesh.triangles = meshData.triangleList;
            mesh.RecalculateNormals();
            GameObject chunk = new GameObject();
            chunk.isStatic = true;
            chunk.transform.parent = this.transform;
            chunk.transform.position = meshData.position * _chunkSize;
            chunk.transform.name = meshData.position.ToString();
            MeshFilter meshFilter = chunk.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = chunk.AddComponent<MeshRenderer>();
            meshFilter.mesh = mesh;
            MeshCollider meshCollider = chunk.AddComponent<MeshCollider>();
            meshRenderer.material = _defaultMaterial;
            _ChunkDictionary.Add(meshData.position, chunk);

            if (_useTrees)
                for (int i = 0; i < _treesPerChunk; i++)
                {
                    RaycastHit hit;
                    Vector3 offset = new Vector3(UnityEngine.Random.Range(0, _chunkSize), _maxHeight * 1.5f, UnityEngine.Random.Range(0, _chunkSize));
                    if (Physics.Raycast(new Vector3(meshData.position.x, 0, meshData.position.z) * _chunkSize + offset, Vector3.down, out hit, Mathf.Infinity))
                    {
                        if (hit.point.y >= _treeExistanceHeights.from && hit.point.y <= _treeExistanceHeights.to && Vector3.Angle(hit.normal, Vector3.up) < 25f)
                        {
                            if (Unity.Mathematics.noise.snoise(new float2(hit.point.x * 0.00025f, hit.point.z * 0.00025f)) >= UnityEngine.Random.Range(-0.9f, 0.9f))
                            {
                                GameObject tree = Instantiate(_treeModel);
                                tree.transform.position = hit.point;
                                tree.transform.parent = chunk.transform;
                                float scale = UnityEngine.Random.Range(3.1f, 5f);
                                tree.transform.localScale = new Vector3(scale, scale, scale);
                                tree.isStatic = true;
                            }
                        }
                    }
                }
        }

        // List<Vector3> chunkTmp = new List<Vector3>(_ChunkDictionary.Keys);
        // if (_unloadChunksFlag)
        //     foreach (Vector3 chunk in chunkTmp)
        //     {
        //         if (Vector3.Distance(tracker.position, chunk * _chunkSize) >= (_unloadChunks * _chunkSize) && _ChunkDictionary.ContainsKey(chunk))
        //         {
        //             Destroy(_ChunkDictionary[chunk]);
        //             _ChunkDictionary.Remove(chunk);
        //             _chunkPositionList.Remove(chunk);
        //         }
        //     }
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

                MeshBuildData meshData = MeshConstructor.ConstructTerrain(heightMapGenerator.SampleChunkData(toGenerate, _chunkResolution, _chunkSize, _maxHeight, _terrainMapping), toGenerate, _chunkSize, _chunkResolution);
                meshQueue.Enqueue(meshData);
            }
        }
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
