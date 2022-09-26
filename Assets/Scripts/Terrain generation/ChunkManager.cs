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
    [SerializeField][Range(0, 1)] private float _surfaceLevel = 0;
    [SerializeField] private List<Vector3> _chunkDictionary = new List<Vector3>();
    [SerializeField] private Dictionary<Vector3,GameObject> _ChunkKeeper = new Dictionary<Vector3, GameObject>();

    [SerializeField] private float _maxHeight;
    [SerializeField] private NoiseLayer[] noiseLayers;
    [SerializeField] private Transform tracker;





    private Vector3 _worldSeed;
    private Queue<MeshBuildData> meshQueue = new Queue<MeshBuildData>();
    private Queue<Vector3> chunkQueue = new Queue<Vector3>();

    void Start()
    {
        for (int layerIndex = 0; layerIndex < noiseLayers.Length; layerIndex++)
        {
            noiseLayers[layerIndex].offset = new Vector2(UnityEngine.Random.Range(0, 10000) / 10000, UnityEngine.Random.Range(0, 10000) / 10000);
        }
    }


    void FixedUpdate()
    {
        Vector3 cp = tracker.position;
        Vector3 chunkChecker = new Vector3(Mathf.Round(cp.x / _chunkSize), 0, Mathf.Round(cp.z / _chunkSize));

        for (int x = _renderDistance / -2; x < _renderDistance / 2; x++)
        {
            for (int y = _renderDistance / -2; y < _renderDistance / 2; y++)
            {
                Vector3 sampler = chunkChecker + new Vector3(x, 0, y);
                if (!_chunkDictionary.Contains(sampler))
                {
                    _chunkDictionary.Add(sampler);
                    chunkQueue.Enqueue(sampler);
                }
            }
        }


        if (_generate)
        {
            _worldSeed = new Vector3(UnityEngine.Random.Range(0, 10000), UnityEngine.Random.Range(0, 10000), UnityEngine.Random.Range(0, 10000));
            Debug.Log(_worldSeed);
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
            // for (int i = 0; i < meshQueue.Count; i++)
            // {
            MeshBuildData meshData = meshQueue.Dequeue();
            Mesh mesh = new Mesh();
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
            _ChunkKeeper.Add(meshData.position,chunk);
            // }
        }

        Vector3[] cp = _chunkDictionary.ToArray();
        foreach (Vector3 chunk in cp)
        {
            if(Vector3.Distance(tracker.position,chunk) >= _unloadChunks * _chunkSize){
                Debug.Log(_ChunkKeeper[chunk]);
                Destroy(_ChunkKeeper[chunk]);
                _ChunkKeeper.Remove(chunk);
                _chunkDictionary.Remove(chunk);
            }
        }
    }

    // ty vole jak dlouho na tom hajzlu ses.. honis?
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
                    MeshBuildData meshData = MeshGenerator.ConstructChunkMesh(SampleChunkData(toGenerate), toGenerate, _surfaceLevel, _chunkSize, _chunkResolution);
                    meshQueue.Enqueue(meshData);
                }
                catch { }
            }
        }
        // for (int x = 0; x < _renderDistance; x++)
        // {
        //     for (int z = 0; z < _renderDistance; z++)
        //     {
        //         Vector3 sampleOffset = new Vector3(x, 0, z);
        //         MeshBuildData meshData = MeshGenerator.ConstructChunkMesh(SampleChunkData(sampleOffset), sampleOffset * _chunkSize, _surfaceLevel, _chunkSize, _chunkResolution);
        //         meshQueue.Enqueue(meshData);
        //     }
        // }
    }


    //! This shit will be reworkerd later because of shit scalability (cant remove chunks that contain nothing without further processing)
    float[,] SampleChunkData(Vector3 offset)
    {
        // float[,,] chunk = new float[_chunkSize + 1, _chunkSize + 1, _chunkSize + 1];

        float[,] chunkSamples = new float[_chunkResolution + 1, _chunkResolution + 1];
        float sampleRate = _chunkSize / _chunkResolution;

        for (int x = 0; x < _chunkResolution + 1; x++)
        {
            for (int y = 0; y < _chunkResolution + 1; y++)
            {
                // float sample = 0;

                // Vector2 samplePosition = Vector2.zero;
                // samplePosition.x = (x * sampleRate) + (offset.x * _chunkSize);
                // samplePosition.y = (y * sampleRate) + (offset.z * _chunkSize);

                // for (int layerIndex = 0; layerIndex < noiseLayers.Length; layerIndex++)
                // {
                //     float pureSample = Mathf.PerlinNoise(((samplePosition.x + _worldSeed.x) * noiseLayers[layerIndex].scale),
                //                                         ((samplePosition.y + _worldSeed.z) * noiseLayers[layerIndex].scale));

                //     sample += pureSample * noiseLayers[layerIndex].weight;
                // }

                // sample /= noiseLayers.Length;


                float x1 = NoiseSampling.sampleNoise(x, y, sampleRate, offset, _chunkSize, noiseLayers, _worldSeed);
                float x2 = NoiseSampling.sampleNoise(x + 24.2f, y + 32.3f, sampleRate, offset, _chunkSize, noiseLayers, _worldSeed);

                chunkSamples[x, y] = NoiseSampling.sampleNoise(x + 750 * x1, y + 750 * x2, sampleRate, offset, _chunkSize, noiseLayers, _worldSeed) * _maxHeight;



                // Distribution to higher chunks 
                // for (int y = 0; y < _chunkSize + 1; y++)
                // {
                //     chunk[x, y, z] = sample - y;
                // }
            }
        }

        return chunkSamples;
    }


    void OnDrawGizmos()
    {
        Vector3 chunkDimensions = new Vector3(_chunkSize, _maxHeight, _chunkSize);
        Gizmos.color = Color.blue;
        foreach (var item in _chunkDictionary)
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
