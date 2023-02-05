using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Chunk
{
    [Header("Rendering")]
    public MeshFilter MeshFilter;
    public MeshRenderer MeshRenderer;
    public MeshCollider MeshCollider;


    public Vector2 Position;
    
    
    public float[,] HeightMap;
    public int CurrentLODindex;

    public Dictionary<Spawnable,Matrix4x4[]> DetailDictionary = new Dictionary<Spawnable, Matrix4x4[]>();
    public Dictionary<Spawnable,Matrix4x4[]> LowDetailDictionary = new Dictionary<Spawnable, Matrix4x4[]>();

    public float LocalMaximum;
    public float LocalMinimum;

    private ChunkManager ChunkManager;

    public Chunk(float[,] heightMap, Vector2 position, float localMinimum, float localMaximum, ChunkManager chunkManager)
    {
        this.HeightMap = heightMap;
        this.Position = position;
        this.LocalMaximum = localMaximum;
        this.LocalMinimum = localMinimum;
        ChunkManager = chunkManager;
    }

    public void UpdateChunk(int LOD, Vector2 borderVector,Vector3 viewerPosition){
        MeshRequest request = new MeshRequest(HeightMap, Vector3.zero,this,viewerPosition);
        lock(ChunkManager.MeshRequests){
            ChunkManager.MeshRequests.Enqueue(request);
        }
    }

    public void OnMeshRecieved(MeshData meshData){
        CurrentLODindex = meshData.LOD;
        
        Mesh mesh = MeshFilter.mesh;
        mesh.Clear();
        mesh.vertices = meshData.vertexList;
        mesh.triangles = meshData.triangleList;
        mesh.RecalculateNormals();
        MeshFilter.mesh = mesh;

        if ((meshData.LOD == 1 && MeshCollider.sharedMesh == null) || (meshData.LOD == 4 && !ChunkManager.GenerationComplete)  ){
            MeshCollider.enabled = true;
            MeshCollider.sharedMesh = mesh;
        }
        else if (meshData.LOD == 1){
            MeshCollider.enabled = true;
        }
        else{
            MeshCollider.enabled = false;
        }

        if(meshData.LOD <= 4){
            ChangeChildrenState(true);
        }
        else{
            ChangeChildrenState(false);
        }
    }

    private void ChangeChildrenState(bool state){
        for (int i = 0; i < MeshCollider.transform.childCount ; i++)
        {
            MeshCollider.transform.GetChild(i).gameObject.SetActive(state);
        }
    }
}

public struct MeshRequest
{
    public readonly float[,] HeightMap;
    public readonly Vector3 Position;
    public readonly Chunk CallbackObject;
    public readonly Vector3 ViewerPosition;

    public MeshRequest(float[,] heightMap, Vector3 position, Chunk callbackObject, Vector3 viewerPosition)
    {
        HeightMap = heightMap;
        Position = position;
        CallbackObject = callbackObject;
        ViewerPosition = viewerPosition;
    }
}



public enum Spawnable
{
    ConiferTree,
    DeciduousTree,
    Rock,
    Bush 
}