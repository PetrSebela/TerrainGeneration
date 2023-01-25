using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshStitcher
{
    public static Mesh StitchMeshes( CombineInstance[] meshes ){
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        List<Vector3> vertexList = new List<Vector3>();
        List<int> triangleList = new List<int>();
        int triangleOffset = 0;

        foreach (CombineInstance instance in meshes)
        {       
            Vector3 offset = instance.transform.GetPosition();
            Mesh meshInstance = instance.mesh;

            foreach (Vector3 vertex in meshInstance.vertices)
            {
                vertexList.Add(vertex + offset);
            }

            foreach (int triangleIndex in meshInstance.triangles)
            {
                triangleList.Add(triangleIndex + triangleOffset);
            }
            triangleOffset += meshInstance.vertices.Length;
        }


        Dictionary<Vector3, int> duplicateMapping = new Dictionary<Vector3, int>();

        int vertexMapIndex = 0;
        foreach (Vector3 item in vertexList)
        {
            if (!duplicateMapping.ContainsKey(item))
            {
                duplicateMapping.Add(item, vertexMapIndex++);
            }
        }

        List<Vector3> constructVertexList = new List<Vector3>();
        List<int> constructTriangleList = new List<int>();
        foreach (int item in triangleList)
        {
            constructTriangleList.Add(duplicateMapping[vertexList[item]]);
        }

        foreach (Vector3 item in duplicateMapping.Keys)
        {
            constructVertexList.Add(item);
        }

        mesh.vertices = constructVertexList.ToArray();
        mesh.triangles = constructTriangleList.ToArray();
        mesh.RecalculateNormals();
        return mesh;
    }
}
