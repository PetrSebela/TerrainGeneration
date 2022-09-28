using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeightMapGenerator
{
    private int octaves;
    private float lacunarity;
    private float gain;

    // private float frequency;
    // private float amplitude;
    private AnimationCurve animCurve;
    private Vector3 _worldSeed;

    public HeightMapGenerator(float frequency, float amplitude, int octaves, float lacunarity, float gain, AnimationCurve animCurve)
    {
        _worldSeed = new Vector3(UnityEngine.Random.Range(0, 10000), UnityEngine.Random.Range(0, 10000), UnityEngine.Random.Range(0, 10000));
        // this.frequency = frequency;
        // this.amplitude = amplitude;
        this.octaves = octaves;
        this.lacunarity = lacunarity;
        this.gain = gain;
        this.animCurve = animCurve;
    }


    //! Add normal noise layering (persistence etc) remove user specified layer (might add later depending on result)
    private float SampleNoise(float x, float y, float sampleRate, Vector3 offset, float _chunkSize, Vector3 worldSeed)
    {
        float sample = 0;
        float amplitude = 1.25f;
        float frequency = 0.0001f;


        Vector2 samplePosition = Vector2.zero;
        samplePosition.x = (x * sampleRate) + (offset.x * _chunkSize);
        samplePosition.y = (y * sampleRate) + (offset.z * _chunkSize);

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

    public float[,] SampleChunkData(Vector3 offset, int chunkResolution, float chunkSize, float maxHeight, AnimationCurve terrainMapping)
    {
        float[,] chunkSamples = new float[chunkResolution + 1, chunkResolution + 1];
        float sampleRate = chunkSize / chunkResolution;

        for (int x = 0; x < chunkResolution + 1; x++)
        {
            for (int y = 0; y < chunkResolution + 1; y++)
            {
                float x1 = SampleNoise(x, y, sampleRate, offset, chunkSize, _worldSeed) * maxHeight;
                float x2 = SampleNoise(x + 51.6f, y + 101.76f, sampleRate, offset, chunkSize, _worldSeed) * maxHeight;
                float sample = SampleNoise(x + 0.015f * x1, y + 0.015f * x2, sampleRate, offset, chunkSize, _worldSeed);


                // chunkSamples[x, y] = (sample + 1) / 2 * maxHeight;
                chunkSamples[x, y] = (sample * 0.25f + 1) / 2 * maxHeight;
                // chunkSamples[x, y] = SampleNoise(x, y, sampleRate, offset, chunkSize, _worldSeed) * maxHeight;
            }
        }

        return chunkSamples;
    }
}
