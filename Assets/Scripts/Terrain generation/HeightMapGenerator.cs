using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeightMapGenerator
{
    private Vector3 _worldSeed;
    public FallOffMap _fallOffMap;
    private float size;
    private int renderDistance;

    public HeightMapGenerator(int renderDistance, float chunkSize, int chunkResolution)
    {
        this.renderDistance = renderDistance;
        // _worldSeed = new Vector3(UnityEngine.Random.Range(-100000, 100000), UnityEngine.Random.Range(-100000, 100000), UnityEngine.Random.Range(-100000, 100000));
        _worldSeed = Vector3.zero;
        _fallOffMap = new FallOffMap(chunkResolution * renderDistance, 0.5f, 1f);
        size = renderDistance - 1 * chunkSize;

    }


    //! Add normal noise layering (persistence etc) remove user specified layer (might add back later depending on result)
    private float SampleNoise(float x, float y, float sampleRate, Vector3 offset, float chunkSize, Vector3 worldSeed)
    {
        float sample = 0;
        float amplitude = 1.25f;
        float frequency = 0.0001f;


        Vector2 samplePosition = Vector2.zero;
        samplePosition.x = (x * sampleRate) + (offset.x * chunkSize);
        samplePosition.y = (y * sampleRate) + (offset.z * chunkSize);

        for (int i = 0; i < 6; i++)
        {
            sample += Unity.Mathematics.noise.snoise(new Unity.Mathematics.float2((samplePosition.x + worldSeed.x) * frequency, (samplePosition.y + worldSeed.z) * frequency)) * amplitude;
            frequency *= 2;
            amplitude *= 0.75f;
        }

        return sample;
        // return (sample * sample) * Mathf.Sign(sample);
        // return ((sample + 1) / 2) * ((sample + 1) / 2);
    }

    public float[,] SampleChunkData(Vector3 offset, int chunkResolution, float chunkSize, float maxHeight, AnimationCurve terrainMapping, int LOD)
    {
        float[,] chunkSamples = new float[chunkResolution + 1, chunkResolution + 1];

        float sampleRate = (float)chunkSize / chunkResolution;

        for (int x = 0; x < chunkResolution + 1; x++)
        {
            for (int y = 0; y < chunkResolution + 1; y++)
            {
                float x1 = SampleNoise(x, y, sampleRate, offset, chunkSize, _worldSeed) * maxHeight;
                float x2 = SampleNoise(x + 51.6f, y + 101.76f, sampleRate, offset, chunkSize, _worldSeed) * maxHeight;
                float sample = SampleNoise(x + 0.015f * x1, y + 0.015f * x2, sampleRate, offset, chunkSize, _worldSeed);

                chunkSamples[x, y] = (((sample * 0.25f + 1) / 2) - _fallOffMap.getValue((int)(offset.x * chunkResolution + chunkResolution * renderDistance / 2) + x, (int)(offset.z * chunkResolution + chunkResolution * renderDistance / 2) + y) * 0.5f) * maxHeight;
                // chunkSamples[x, y] = (((sample * 0.25f + 1) / 2)) * maxHeight;
            }
        }

        if (LOD != 1)
        {
            float[,] lowRes = new float[chunkResolution / LOD + 1, chunkResolution / LOD + 1];
            for (int x = 0; x < chunkResolution + 1; x += LOD)
            {
                for (int y = 0; y < chunkResolution + 1; y += LOD)
                {
                    lowRes[x / LOD, y / LOD] = chunkSamples[x, y];
                }
            }
            return lowRes;
        }
        return chunkSamples;
    }
}
