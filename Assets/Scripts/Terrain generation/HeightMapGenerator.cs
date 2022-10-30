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
        _worldSeed = Vector3.zero;
        _fallOffMap = new FallOffMap(chunkResolution * renderDistance, 0.5f, 1);
        size = renderDistance - 1 * chunkSize;
    }


    //! Add normal noise layering (persistence etc) remove user specified layer (might add back later depending on result)
    // return value can be modified by mathematical function such as {(sample * sample) * Mathf.Sign(sample)}
    private float SampleNoise(float x, float y, float sampleRate, Vector3 offset, float chunkSize, Vector3 worldSeed)
    {
        float sample = 0;
        float amplitude = 4f;
        float frequency = 0.00005f;


        Vector2 samplePosition = Vector2.zero;
        samplePosition.x = (x * sampleRate) + (offset.x * chunkSize);
        samplePosition.y = (y * sampleRate) + (offset.z * chunkSize);

        for (int i = 0; i < 5; i++)
        {
            sample += Unity.Mathematics.noise.snoise(new Unity.Mathematics.float2((samplePosition.x + worldSeed.x) * frequency, (samplePosition.y + worldSeed.z) * frequency)) * amplitude;
            frequency *= 2;
            amplitude *= 0.75f;
        }

        return sample;
    }

    /*
        return 2D array of heights for specific ponints
        size of one side is chunkResolution + 3
    */
    public float[,] SampleChunkData(Vector3 offset, int chunkResolution, float chunkSize, float maxHeight)
    {
        // chunkSize -= 2
        float[,] chunkSamples = new float[chunkResolution + 2, chunkResolution + 2];

        float sampleRate = (float)chunkSize / (chunkResolution);

        for (int x = 0; x < chunkResolution + 2; x++)
        {
            for (int y = 0; y < chunkResolution + 2; y++)
            {
                float x1 = SampleNoise(x, y, sampleRate, offset, chunkSize, _worldSeed) * maxHeight;
                float x2 = SampleNoise(x + 51.6f, y + 101.76f, sampleRate, offset, chunkSize, _worldSeed) * maxHeight;
                float sample = SampleNoise(x + 0.015f * x1, y + 0.015f * x2, sampleRate, offset, chunkSize, _worldSeed);

                sample = (Mathf.Pow(Mathf.Abs(sample), 0.95f) * Mathf.Sign(sample) * 0.1f + 1) / 2;
                sample *= _fallOffMap.getValue((int)(offset.x * chunkResolution + chunkResolution * renderDistance / 2) + x,
                                               (int)(offset.z * chunkResolution + chunkResolution * renderDistance / 2) + y) * -1 + 1;
                sample *= maxHeight;

                chunkSamples[x, y] = sample;

                // chunkSamples[x, y] = ();
                // chunkSamples[x, y] *= _fallOffMap.getValue((int)(offset.x * chunkResolution + chunkResolution * renderDistance / 2) + x,
                //                                            (int)(offset.z * chunkResolution + chunkResolution * renderDistance / 2) + y);
                // chunkSamples[x, y];
                // chunkSamples[x, y] = (((sample * 0.25f + 1) / 2)) * maxHeight;
            }
        }

        return chunkSamples;
    }
}
