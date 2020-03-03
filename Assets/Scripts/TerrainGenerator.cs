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
            fv[i] = (vertices[faceTriangles[dir][i]]) * 0.5f + pos;
        }
        return fv;
    }
    public static Vector3[] faceVertices(Direction dir, Vector3 pos)
    {
        return faceVertices((int)dir, pos);
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
            for (int z = 0; z < d; z++)
            {
                if (data.GetCell(x, 0, z) == 1)
                {
                    MakeCube(new Vector3(x, 0, z), data);
                }
            }
        }

    }

    void MakeCube(Vector3 pos, VoxelData data)
    {
        for (int i = 0; i < 6; i++)
        {
            if (data.GetNeighbor((int)pos.x, (int)pos.y, (int)pos.z, (Direction)i) == 0)
            {
                MakeFace((Direction)i, pos);
            }
        }
    }
    void MakeFace(Direction dir, Vector3 pos)
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
