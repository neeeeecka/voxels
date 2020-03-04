﻿using System.Collections;
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
    Dictionary<VertexSignature, int> verticesDict = new Dictionary<VertexSignature, int>();
    int vertexCount = 0;
    List<int> triangles = new List<int>();
    List<Vector3> normals = new List<Vector3>();

    List<Vector2> uvs = new List<Vector2>();
    Mesh mesh;

    void Start()
    {

        lastSeed = seed;
        lastDivisor = divisor;

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
        vertexCount = 0;
        vertices.Clear();
        normals.Clear();
        verticesDict.Clear();
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
                    int cubeType = data.GetCell(x, y, z);
                    if (cubeType != 0)
                    {
                        MakeCube(new Vector3(x, y, z), data, cubeType);
                    }
                }
            }
        }
        UpdateMesh();
    }

    void MakeCube(Vector3 pos, VoxelData data, int cubeType)
    {
        for (int i = 0; i < 6; i++)
        {
            if (data.GetNeighbor((int)pos.x, (int)pos.y, (int)pos.z, (Direction)i) == 0)
            {
                // MakeFace((Direction)i, pos, cubeType);
                MakeFaceClean((Direction)i, pos, cubeType);
            }
        }
    }
    int GetVertexIndex(VertexSignature signature)
    {
        //return new vertex index, 
        //if it doesn't exist we create new, 
        //if it does exist return last index

        int index;
        if (!verticesDict.TryGetValue(signature, out index))
        {
            index = vertexCount++;
            verticesDict.Add(signature, index);
        }
        return index;
    }
    void MakeFace(Direction dir, Vector3 pos, int cubeType)
    {
        vertices.AddRange(CubeMeshData.faceVertices(dir, pos));
        uvs.AddRange(CubeMeshData.faceUVs(dir, cubeType));

        int zero = vertices.Count - 4;

        triangles.Add(zero);
        triangles.Add(zero + 1);
        triangles.Add(zero + 2);
        triangles.Add(zero);
        triangles.Add(zero + 2);
        triangles.Add(zero + 3);
    }
    void MakeFaceClean(Direction dir, Vector3 pos, int cubeType)
    {

        // vertices.AddRange(CubeMeshData.faceVertices(dir, pos));
        VertexSignature signature;
        signature.normal = dir;

        Vector3[] faceVertices = CubeMeshData.faceVertices(dir, pos);
        int[] triangleIndices = new int[4];

        signature.position = faceVertices[0];


        int a = GetVertexIndex(signature);

        signature.position = faceVertices[1];


        int b = GetVertexIndex(signature);

        signature.position = faceVertices[2];


        int c = GetVertexIndex(signature);

        signature.position = faceVertices[3];


        int d = GetVertexIndex(signature);

        // uvs.AddRange(CubeMeshData.faceUVs(dir, cubeType));

        int zero = verticesDict.Count - 4;

        triangles.Add(a);
        triangles.Add(b);
        triangles.Add(c);

        triangles.Add(a);
        triangles.Add(c);
        triangles.Add(d);
    }

    void SetFinalData()
    {
        foreach (var pair in verticesDict)
        {
            int index = pair.Value;
            vertices.Add(pair.Key.position);
            normals.Add(CubeMeshData.offsets[(int)pair.Key.normal].ToVector());

            // Vector3 uv = ProjectPositionToUV(pair.key.position, pair.key.normal);
            // Vector2 uv = CubeMeshData.GetVertexUV(pair.Key.normal);
            // uvs[index] = uv;
        }

    }

    void UpdateMesh()
    {
        mesh.Clear();

        SetFinalData();

        // mesh.vertices = JustConvertDictionaryToVertices();

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        // mesh.uv = uvs.ToArray();
        // mesh.RecalculateNormals();
    }

    struct VertexSignature
    {
        public Vector3 position;
        public Direction normal;

    };
}
