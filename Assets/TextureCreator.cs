using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureCreator : MonoBehaviour
{
    // Start is called before the first frame update
    List<GameObject> Models = new List<GameObject>();
    public Texture2D[] textures;
    public Texture2D test;
    public Shader shaderBase;
    
    public Mesh mesh;
    Camera cam;
    void Start()
    {
        cam = this.GetComponent<Camera>();
        cam.aspect = 1f;
        Material material = new Material(shaderBase);

        RenderTexture rt = new RenderTexture(1024,1024,16,RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();//render stuff        
        transform.parent.transform.Rotate(0,360f / textures.Length,0);
        material.SetTexture("_BaseTexture",RenderTextureTo2DTexture(rt));
        GameObject go = new GameObject();
        MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();
        MeshFilter meshFilter = go.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;
        meshRenderer.material = material;
        go.transform.position = new Vector3(0,2,0);
    }

    private Texture2D RenderTextureTo2DTexture(RenderTexture rt)
    {
        var texture = new Texture2D(rt.width, rt.height, rt.graphicsFormat, 0);
        RenderTexture.active = rt;
        texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        texture.Apply();
        
        RenderTexture.active = null;

        return texture;
    }
}
