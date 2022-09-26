using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class ChunkManager : MonoBehaviour
{
    [Header("Chunk setting")]
    [SerializeField] private int _chunkResolution;
    [SerializeField] private float _chunkSize;
    [SerializeField] private Material _defaultMaterial;

    [Header("World setting")]
    [SerializeField] private int _renderDistance;
    [SerializeField] private int _unloadChunks;
    [SerializeField] private bool _generate = true;
    [SerializeField] private bool _unloadChunksFlag = true;
    [SerializeField] private List<Vector3> _chunkPositionList = new List<Vector3>();
    [SerializeField] private Dictionary<Vector3, GameObject> _ChunkDictionary = new Dictionary<Vector3, GameObject>();

    [SerializeField] private float _maxHeight;
    [SerializeField] private NoiseLayer[] noiseLayers;
    [SerializeField] private Transform tracker;

    [SerializeField] private Texture2D texture;





    private Vector3 _worldSeed;
    private Queue<MeshBuildData> meshQueue = new Queue<MeshBuildData>();
    private Queue<Vector3> chunkQueue = new Queue<Vector3>();

    void Start()
    {
        for (int layerIndex = 0; layerIndex < noiseLayers.Length; layerIndex++)
        {
            noiseLayers[layerIndex].offset = new Vector2(UnityEngine.Random.Range(0, 10000), UnityEngine.Random.Range(0, 10000));
        }


        texture = new Texture2D(_chunkResolution, _chunkResolution, TextureFormat.ARGB32, false);
        float[,] samples = SampleChunkData(new Vector3(0, 0, 0));

        for (int x = 0; x < _chunkResolution; x++)
        {
            for (int y = 0; y < _chunkResolution; y++)
            {
                texture.SetPixel(x, y, new Color(samples[x, y], samples[x, y], samples[x, y]));
            }
        }

        texture.Apply();

    }


    void FixedUpdate()
    {
        Vector3 cp = tracker.position;
        Vector3 chunkChecker = new Vector3(Mathf.Round(cp.x / _chunkSize), 0, Mathf.Round(cp.z / _chunkSize));
        Debug.Log(chunkChecker);


        // Chunks are name without relationship to their size. (0,0,0) -> neighbour (0,0,1)
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


        if (_generate)
        {
            _worldSeed = new Vector3(UnityEngine.Random.Range(0, 10000), UnityEngine.Random.Range(0, 10000), UnityEngine.Random.Range(0, 10000));
            ThreadStart threadStart = delegate
            {
                UpdateWorld();
            };

            new Thread(threadStart).Start();
            _generate = false;
        }


    }

    void Update()
    {
        if (meshQueue.Count > 0)
        {
            MeshBuildData meshData = meshQueue.Dequeue();
            Mesh mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = meshData.vertexList;
            mesh.triangles = meshData.triangleList;
            mesh.RecalculateNormals();
            GameObject chunk = new GameObject();
            chunk.isStatic = true;
            chunk.transform.parent = this.transform;
            chunk.transform.position = meshData.position * _chunkSize - new Vector3(0, _maxHeight / 2, 0);
            chunk.transform.name = meshData.position.ToString();
            MeshFilter meshFilter = chunk.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = chunk.AddComponent<MeshRenderer>();
            meshFilter.mesh = mesh;
            MeshCollider meshCollider = chunk.AddComponent<MeshCollider>();
            meshRenderer.material = _defaultMaterial;
            _ChunkDictionary.Add(meshData.position, chunk);
        }

        List<Vector3> chunkTmp = new List<Vector3>(_ChunkDictionary.Keys);


        if (_unloadChunksFlag)
            foreach (Vector3 chunk in chunkTmp)
            {
                if (Vector3.Distance(tracker.position, chunk * _chunkSize) >= (_unloadChunks * _chunkSize) && _ChunkDictionary.ContainsKey(chunk))
                {
                    Destroy(_ChunkDictionary[chunk]);
                    _ChunkDictionary.Remove(chunk);
                    _chunkPositionList.Remove(chunk);
                }
            }
    }

    void UpdateWorld()
    {
        while (true)
        {
            for (int i = 0; i < chunkQueue.Count; i++)
            {
                try
                {
                    Vector3 toGenerate;
                    lock (chunkQueue)
                    {
                        toGenerate = chunkQueue.Dequeue();
                    }
                    // Vector3 sampleOffset = new Vector3(x, 0, z);
                    MeshBuildData meshData = MeshGenerator.ConstructChunkMesh(SampleChunkData(toGenerate), toGenerate, _chunkSize, _chunkResolution);
                    meshQueue.Enqueue(meshData);
                }
                catch { }
            }
        }
    }


    float[,] SampleChunkData(Vector3 offset)
    {
        // float[,,] chunk = new float[_chunkSize + 1, _chunkSize + 1, _chunkSize + 1];

        float[,] chunkSamples = new float[_chunkResolution + 1, _chunkResolution + 1];
        float sampleRate = _chunkSize / _chunkResolution;

        for (int x = 0; x < _chunkResolution + 1; x++)
        {
            for (int y = 0; y < _chunkResolution + 1; y++)
            {
                float x1 = NoiseSampling.sampleNoise(x, y, sampleRate, offset, _chunkSize, noiseLayers, _worldSeed);
                float x2 = NoiseSampling.sampleNoise(x + 64.6f, y + 49.76f, sampleRate, offset, _chunkSize, noiseLayers, _worldSeed);

                chunkSamples[x, y] = NoiseSampling.sampleNoise(x + 2048 * x1, y + 2048 * x2, sampleRate, offset, _chunkSize, noiseLayers, _worldSeed) * _maxHeight;
            }
        }

        return chunkSamples;
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
