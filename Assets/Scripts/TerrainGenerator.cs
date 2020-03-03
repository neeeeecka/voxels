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

    public static int[][] faces = {
        new int[]{0,1,2,3}, //North face
        new int[]{5,0,3,6}, //East
        new int[]{4,5,6,7}, //South
        new int[]{1,4,7,2}, //West
        new int[]{5,4,1,0}, //Up
        new int[]{3,2,7,6} //Down
    };

    public static Vector3[] faceVertices(int dir, Vector3 pos)
    {
        Vector3[] fv = new Vector3[4];
        for (int i = 0; i < 4; i++)
        {
            fv[i] = (vertices[faces[dir][i]]) * 0.5f + pos;
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
    public int seed = 209323094;
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
        int depth = 10, width = 15, height = 10;

        Noise.Seed = 209323094; // Optional
        float scale = 0.10f;
        float[,] noiseValues = Noise.Calc2D(depth, width, scale);

        VoxelData data = new VoxelData(width, height, depth);


        // for (int x = 0; x < width; x++)
        // {
        //     for (int z = 0; z < depth; z++)
        //     {
        //         int yVal = (int)Mathf.Clamp(noiseValues[z, x] / 10, 0, height - 1);
        //         data.SetCell(x, yVal, z, 1);
        //     }
        // }

        int w = data.Width();
        int d = data.Depth();
        int h = data.Height();


        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                for (int z = 0; z < d; z++)
                {
                    if (data.GetCell(x, y, z) == 1)
                    {
                        MakeCube(new Vector3(x, y, z), data);
                    }
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
