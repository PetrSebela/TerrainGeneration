using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FallOffMap
{
    private float[,] fallOffMap;
    private int size;
    public FallOffMap(int size, float start, float end)
    {
        this.size = size;
        fallOffMap = new float[size, size];

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                float x = (float)i / size * 2 - 1;
                float y = (float)j / size * 2 - 1;

                float t = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));

                if (t < start)
                    fallOffMap[i, j] = 1;
                else if (t > end)
                    fallOffMap[i, j] = 0;
                else
                    fallOffMap[i, j] = Mathf.SmoothStep(1, 0, Mathf.InverseLerp(start, end, t));
            }

        }
    }

    public float getValue(int x, int y)
    {

        try
        {
            return -fallOffMap[x, y] + 1;
        }
        catch
        {
            return 1f;
        }
    }

    public Texture2D GetTexture()
    {
        Texture2D texture = new Texture2D(size, size);
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                float value = fallOffMap[i, j];
                texture.SetPixel(i, j, new Color(value, value, value));
            }
        }
        texture.Apply();
        return texture;
    }
}