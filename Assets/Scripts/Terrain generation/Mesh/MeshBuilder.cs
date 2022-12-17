using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshBuilder
{
    private static Vector2Int[] offsets = new Vector2Int[]{
        new Vector2Int(0,0),
        new Vector2Int(1,0),
        new Vector2Int(1,1),
        new Vector2Int(0,1),
    };

    private static int[] triOrder = new int[]{
        0,2,1,
        0,3,2
    };

    private ChunkSettings chunkSettings;
    public MeshBuilder(ChunkSettings chunkSettings){
        this.chunkSettings = chunkSettings;
    }

    // takes in constant size 2D array and creates mesh with variable leavel of detail
    public MeshData ConstructTerrain(TreeNode branch)
    {
        int LODnumber = (int)(64 / Mathf.Pow(2, branch.NodeDepth() -1));
        float sampleRate = (chunkSettings.size) / ((float)(chunkSettings.maxResolution) / LODnumber);

        List<Vector3> vertexList = new List<Vector3>();
        List<int> triangleList = new List<int>();
        int vertexCout = 0;

        List<TreeNode> leafs = new List<TreeNode>();
        GetLeafs(leafs,branch);

        foreach (TreeNode leaf in leafs)
        {
            for (int x = 0; x < chunkSettings.maxResolution / LODnumber; x++)
            {
                for (int y = 0; y < chunkSettings.maxResolution / LODnumber; y++)
                {
                    for (int offsetIndex = 0; offsetIndex < offsets.Length; offsetIndex++)
                    {
                        float xPosition = (x + offsets[offsetIndex].x) * sampleRate;
                        float yPosition = (y + offsets[offsetIndex].y) * sampleRate;
                        float height = leaf.Data.heightMap[
                            (x + offsets[offsetIndex].x) * LODnumber + 1, 
                            (y + offsets[offsetIndex].y) * LODnumber + 1];

                        vertexList.Add(new Vector3(
                            xPosition,
                            height,
                            yPosition
                        ) + new Vector3(leaf.Position.x,0,leaf.Position.y) * chunkSettings.size);  
                        // add offset
                    }
                    for (int tIndex = 0; tIndex < triOrder.Length; tIndex++)
                    {
                        triangleList.Add(vertexCout + triOrder[tIndex]);
                    }
                    vertexCout += 4;
                }
            }
        }

        // MeshData meshData = new MeshData(constructVertexList.ToArray(), constructTriangleList.ToArray(), position);
        MeshData meshData = new MeshData(vertexList.ToArray(), triangleList.ToArray(), branch);
        return meshData;
    }

    private void GetLeafs(List<TreeNode> list, TreeNode branch){
        if( branch.Children.Count == 0){
            list.Add(branch);
            return;
        }

        foreach (TreeNode item in branch.Children.Values)
        {
            GetLeafs(list, item);
        }
    }
}
public struct MeshData
{
    public readonly Vector3[] vertexList;
    public readonly int[] triangleList;
    public readonly TreeNode basedOn;

    public MeshData(Vector3[] vertexList, int[] triangleList, TreeNode basedOn)
    {
        this.vertexList = vertexList;
        this.triangleList = triangleList;
        this.basedOn = basedOn;
    }
}
