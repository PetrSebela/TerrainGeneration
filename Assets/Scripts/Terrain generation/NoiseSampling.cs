using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class NoiseSampling
{
    public static float sampleNoise(float x, float y, float sampleRate, Vector3 offset, float _chunkSize, NoiseLayer[] noiseLayers, Vector3 worldSeed)
    {
        float sample = 0;

        Vector2 samplePosition = Vector2.zero;
        samplePosition.x = (x * sampleRate) + (offset.x * _chunkSize);
        samplePosition.y = (y * sampleRate) + (offset.z * _chunkSize);

        for (int layerIndex = 0; layerIndex < noiseLayers.Length; layerIndex++)
        {
            float pureSample = Mathf.PerlinNoise(((samplePosition.x + worldSeed.x) * noiseLayers[layerIndex].scale),
                                                ((samplePosition.y + worldSeed.z) * noiseLayers[layerIndex].scale));

            sample += pureSample * noiseLayers[layerIndex].weight;
        }

        sample /= noiseLayers.Length;
        return sample;
    }
}
