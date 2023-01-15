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
        var dirPath = Application.dataPath + "/Textures/";
        if(!Directory.Exists(dirPath)) {
            Directory.CreateDirectory(dirPath);
        }
        File.WriteAllBytes(dirPath + FileName + ".png", bytes);
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
}
