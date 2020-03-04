using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TerrainGenerator : MonoBehaviour
{
    private float lastSeed = 0;
    private int lastDivisor = 0;
    public float seed = 209323094;
    [Range(1, 200)]
    public int divisor = 98;

    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();

    List<Vector2> uvs = new List<Vector2>();
    Mesh mesh;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        mesh.MarkDynamic();
        MakeWorld();
        GetComponent<MeshCollider>().sharedMesh = mesh;

    }

    private void Update()
    {
        if (lastDivisor != divisor || lastSeed != seed)
        {
            lastDivisor = divisor;
            lastSeed = seed;
            MakeWorld();
        }
        // seed += 0.1f;
    }

    void MakeWorld()
    {
        vertices.Clear();
        uvs.Clear();
        triangles.Clear();

        int depth = 1, width = 1, height = 1;

        Noise.Seed = (int)seed; // Optional
        float scale = 0.10f;
        float[,] noiseValues = Noise.Calc2D(depth, width, scale);

        VoxelData data = new VoxelData(width, height, depth);


        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                int yVal = (int)Mathf.Clamp(noiseValues[z, x] / divisor, 0, height);
                // data.SetCell(x, yVal, z, 1);
                for (int y = yVal - 1; y >= 0; y--)
                {
                    data.SetCell(x, y, z, 1);
                }
            }
        }

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
        UpdateMesh();
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
        uvs.AddRange(CubeMeshData.faceUVs(dir));


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
        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
    }
}
