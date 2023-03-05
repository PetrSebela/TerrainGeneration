using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
public class TextureCreator : MonoBehaviour
{

    public string FileName;
    void Start(){
        RenderTexture rt = RenderTexture.GetTemporary( 1024, 1024, 16 );
        GetComponent<Camera>().targetTexture = rt;
        GetComponent<Camera>().Render();

        Texture2D texture = RenderTextureTo2DTexture(rt);

        byte[] bytes = texture.EncodeToPNG();
        var dirPath = Application.dataPath + "/Models/Textures/";
        if(!Directory.Exists(dirPath)) {
            Directory.CreateDirectory(dirPath);
        }
        File.WriteAllBytes(dirPath + FileName + ".png", bytes);
        Debug.Log("render finished");
    }

    private Texture2D RenderTextureTo2DTexture(RenderTexture rt)
    {
        var old_rt = RenderTexture.active;
        Texture2D texture = new Texture2D(rt.width, rt.height, rt.graphicsFormat, 0);
        RenderTexture.active = rt;
        texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        texture.Apply();
        RenderTexture.active = old_rt;
        return texture;
    }


    public static Texture2D GenerateTexture(ChunkManager chunkManager){
        int size = 512;
        Texture2D terrainTexture = new Texture2D(size,size);
        for (float x = 0; x < size; x++)
        {
            for (float y = 0; y < size; y++)
            {
                terrainTexture.SetPixel((int)x,(int)y,new Color(0.92f,0.92f,0.92f));
            }
        }

        terrainTexture.Apply();

        // snow 
        for (float x = 0; x < size; x++)
        {
            for (float y = 0; y < size; y++)
            {
                float normlized = (y/size);
                float sl = EaseInOut(normlized,0.875f,0.925f);
                terrainTexture.SetPixel((int)x,(int)y,MixColors(new Color(0.92f,0.92f,0.92f), terrainTexture.GetPixel((int)x,(int)y), 1 - sl));
            }
        }
        terrainTexture.Apply();

        // dark grass
        for (float x = 0; x < size; x++)
        {
            for (float y = 0; y < size; y++)
            {
                float normlized = (y/size);
                float sl = EaseInOut(normlized,0.85f,0.9f);
                terrainTexture.SetPixel((int)x,(int)y,MixColors(chunkManager.GrassDarker, terrainTexture.GetPixel((int)x,(int)y), 1 - sl));
            }
        }
        terrainTexture.Apply();

        // grass
        for (float x = 0; x < size; x++)
        {
            for (float y = 0; y < size; y++)
            {
                float normlized = (y/size);
                float sl = EaseInOut(normlized,0.5f,0.6f);
                terrainTexture.SetPixel((int)x,(int)y,MixColors(chunkManager.Grass, terrainTexture.GetPixel((int)x,(int)y), 1 - sl));
            }
        }
        terrainTexture.Apply();

        // sand
        for (float x = 0; x < size; x++)
        {
            for (float y = 0; y < size; y++)
            {
                float normlized = (y/size);
                float sl = EaseInOut(normlized,0.1f,0.105f);
                terrainTexture.SetPixel((int)x,(int)y,MixColors(chunkManager.Sand, terrainTexture.GetPixel((int)x,(int)y), 1 - sl));
            }
        }


        // conputing light slope
        for (float x = 10; x < size; x++)
        {
            for (float y = 0; y < size; y++)
            {
                float normlizedX = (x/size);
                float sl = EaseInOut(normlizedX,0.7f,0.8f);
                terrainTexture.SetPixel((int)x,(int)y,MixColors(chunkManager.StoneLighter, terrainTexture.GetPixel((int)x,(int)y), 1 - sl));
            }
        }
        terrainTexture.Apply();

        // conputing slope
        for (float x = 10; x < size; x++)
        {
            for (float y = 0; y < size; y++)
            {
                float normlizedX = (x/size);
                float sl = EaseInOut(normlizedX,0.65f,0.7f);
                terrainTexture.SetPixel((int)x,(int)y,MixColors(chunkManager.Stone, terrainTexture.GetPixel((int)x,(int)y), 1 - sl));
            }
        }
        // conputing dark slope
        for (float x = 10; x < size; x++)
        {
            for (float y = 0; y < size; y++)
            {
                float normlizedX = (x/size);
                float sl = EaseInOut(normlizedX,0.45f,0.62f);
                terrainTexture.SetPixel((int)x,(int)y,MixColors(chunkManager.StoneDarker, terrainTexture.GetPixel((int)x,(int)y), 1 - sl));
            }
        }
        
        terrainTexture.Apply();

        for (int x = 0; x < 10; x++)
        {
            for (int i = 0; i < size; i++)
            {
                terrainTexture.SetPixel(i,size-x, new Color(1,1,1));
            }
        }
        terrainTexture.Apply();

        return terrainTexture;
    }

    static float EaseInOut(float p, float s, float e){
        if(p < s){
            return 0f;
        }
        else if( p > e){
            return 1f;
        }
        else{
            return Mathf.SmoothStep(0,1,Mathf.InverseLerp(s,e,p));
        }   
    }

    static Color MixColors(Color color1 , Color color2, float alpha){
        return alpha * color1 + (1 - alpha) * color2;
    }
}
