using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

public class ChunkManager : MonoBehaviour
{
    [Header("Chunk setting")]
    [SerializeField] private int _chunkSize;
    [SerializeField] private Material _defaultMaterial;

    [Header("World setting")]
    [SerializeField] private int _renderDistance;
    [SerializeField] private bool _generate = true;
    [SerializeField][Range(0, 1)] private float _noiseScale;
    [SerializeField][Range(0, 1)] private float _surfaceLevel = 0;
    [SerializeField] private Dictionary<Vector3, float[,,]> _chunkDictionary = new Dictionary<Vector3, float[,,]>();
    [SerializeField] private float _maxHeight;


    private float _lastSurfaceLevel;
    private float _lastScale;
    private Vector3 _worldSeed;
    private float _startTime;
    private Queue<MeshBuildData> meshQueue = new Queue<MeshBuildData>();
    private List<Thread> threads = new List<Thread>();


    void FixedUpdate()
    {
        if (_generate)
        {
            _worldSeed = new Vector3(UnityEngine.Random.Range(0, 10000), UnityEngine.Random.Range(0, 10000), UnityEngine.Random.Range(0, 10000)) / 10000;
            UpdateWorld();
            _generate = false;
        }

        if (meshQueue.Count > 0)
        {
            for (int i = 0; i < meshQueue.Count; i++)
            {
                MeshBuildData meshData = meshQueue.Dequeue();
                Mesh mesh = new Mesh();
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.vertices = meshData.vertexList;
                mesh.triangles = meshData.triangleList;
                mesh.RecalculateNormals();
                GameObject chunk = new GameObject();
                chunk.transform.parent = this.transform;
                chunk.transform.position = meshData.position;
                chunk.transform.name = meshData.position.ToString();
                MeshFilter meshFilter = chunk.AddComponent<MeshFilter>();
                MeshRenderer meshRenderer = chunk.AddComponent<MeshRenderer>();
                meshFilter.mesh = mesh;
                meshRenderer.material = _defaultMaterial;
            }
        }
    }


    void UpdateWorld()
    {
        _startTime = Time.realtimeSinceStartup;
        for (int x = 0; x < _renderDistance; x++)
        {
            for (int y = 0; y < _renderDistance; y++)
            {
                for (int z = 0; z < _renderDistance; z++)
                {
                    Vector3 sampleOffset = new Vector3(x, y, z);
                    MeshBuildData meshData = MeshGenerator.ConstructChunkMesh(SampleChunkData(sampleOffset), sampleOffset * _chunkSize, _surfaceLevel, _chunkSize);
                    meshQueue.Enqueue(meshData);
                }
            }
        }
        Debug.Log("Procedure took " + (Time.realtimeSinceStartup - _startTime).ToString() + "ms to execute");
    }

    float[,,] SampleChunkData(Vector3 offset)
    {
        float[,,] chunk = new float[_chunkSize + 1, _chunkSize + 1, _chunkSize + 1];

        for (int x = 0; x < _chunkSize + 1; x++)
        {
            for (int z = 0; z < _chunkSize + 1; z++)
            {

                float sample = Mathf.PerlinNoise(((x + offset.x * _chunkSize) * _noiseScale) + _worldSeed.x, ((z + offset.z * _chunkSize) * _noiseScale) + _worldSeed.z) * _maxHeight - (offset.y * _chunkSize);

                for (int y = 0; y < _chunkSize + 1; y++)
                {
                    chunk[x, y, z] = sample - y;
                }
            }
        }

        return chunk;
    }
}


