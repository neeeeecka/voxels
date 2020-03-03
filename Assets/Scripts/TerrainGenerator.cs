using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public static class CubeMeshData
{
    public static Vector3[] vertices = {
        new Vector3(1,1,1),
        new Vector3(-1,1,1),
        new Vector3(-1,-1,1),
        new Vector3(1,-1,1),
        new Vector3(-1,1,-1),
        new Vector3(1,1,-1),
        new Vector3(1,-1,-1),
        new Vector3(-1,-1,-1),
    };

    public static int[][] faceTriangles = {
        new int[]{0,1,2,3},
        new int[]{5,0,3,6},
        new int[]{4,5,6,7},
        new int[]{1,4,7,2},
        new int[]{5,4,1,0},
        new int[]{3,2,7,6}
    };

    public static Vector3[] faceVertices(int dir, Vector3 pos)
    {
        Vector3[] fv = new Vector3[4];
        for (int i = 0; i < 4; i++)
        {
            fv[i] = (vertices[faceTriangles[dir][i]] + pos) * 0.5f;
        }
        return fv;
    }
}

public class TerrainGenerator : MonoBehaviour
{
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    Mesh mesh;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        MakeWorld();
        UpdateMesh();
    }

    void MakeWorld()
    {
        VoxelData data = new VoxelData();
        int w = data.Width();
        int d = data.Depth();
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < d; y++)
            {
                if (data.GetCell(x, y) == 1)
                {
                    MakeCube(new Vector3(x, y, 0));
                }
            }
        }
    }

    void MakeCube(Vector3 pos)
    {
        for (int i = 0; i < 6; i++)
        {
            MakeFace(i, pos);
        }
    }
    void MakeFace(int dir, Vector3 pos)
    {
        vertices.AddRange(CubeMeshData.faceVertices(dir, pos));
        int zero = vertices.Count - 4;

        triangles.Add(zero);
        triangles.Add(zero + 1);
        triangles.Add(zero + 2);
        triangles.Add(zero);
        triangles.Add(zero + 2);
        triangles.Add(zero + 3);
    }

    void UpdateMesh()
    {
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

    }
}
