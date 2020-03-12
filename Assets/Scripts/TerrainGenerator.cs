﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class TerrainGenerator : MonoBehaviour
{
    private float lastSeed = 0;
    private int lastDivisor = 0;
    public float seed = 209323094;
    [Range(1, 200)]
    public int divisor = 98;

    private int lastWorld;
    public int chunkSize;
    public int blocksGenerated = 0;

    List<Vector3> vertices = new List<Vector3>();
    Dictionary<VertexSignature, int> verticesDict = new Dictionary<VertexSignature, int>();
    int vertexCount = 0;
    List<int> triangles = new List<int>();
    List<Vector3> normals = new List<Vector3>();

    List<Vector3> uvs = new List<Vector3>();
    Mesh mesh;

    VoxelData data;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    void Start()
    {

        lastSeed = seed;
        lastDivisor = divisor;
        lastWorld = chunkSize;

        mesh = new Mesh();
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();

        meshFilter.mesh = mesh;
        mesh.MarkDynamic();
        MakeWorld();

        meshCollider.sharedMesh = mesh;

        AssetDatabase.CreateAsset(mesh, "Assets/Temp/mesh.asset");

    }

    private void Update()
    {
        if (lastDivisor != divisor || lastSeed != seed || lastWorld != chunkSize)
        {
            lastDivisor = divisor;
            lastSeed = seed;
            lastWorld = chunkSize;
            MakeWorld();
        }
    }
    void ClearWorld()
    {
        blocksGenerated = 0;
        vertexCount = 0;
        vertices.Clear();
        normals.Clear();
        verticesDict.Clear();
        uvs.Clear();
        triangles.Clear();
    }
    void MakeWorld()
    {
        Noise.Seed = (int)seed;
        float scale = 0.10f;
        float[,] noiseValues = Noise.Calc2D(chunkSize, chunkSize, scale);

        data = new VoxelData(chunkSize, chunkSize, chunkSize);

        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                int yVal = (int)Mathf.Clamp(noiseValues[z, x] / divisor, 0, chunkSize);
                // data.SetCell(x, yVal, z, 1);
                for (int y = yVal - 1; y >= 0; y--)
                {
                    int cubeType = 5 - (int)Mathf.Clamp(noiseValues[z, x] / divisor, 1, 4);
                    data.SetCell(x, y, z, cubeType);
                }
            }
        }

        MakeChunk();
        UpdateMesh();

    }

    public void MakeChunk()
    {
        ClearWorld();

        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    int cubeType = data.GetCell(x, y, z);
                    if (cubeType != 0)
                    {
                        MakeCube(new Vector3(x, y, z), data, cubeType);
                        blocksGenerated++;
                    }
                }
            }
        }
    }

    public void EditWorld(int x, int y, int z, int cubeType)
    {
        data.SetCell(x, y, z, cubeType);
        MakeChunk();

        UpdateMesh();
    }
    void MakeCube(Vector3 pos, VoxelData data, int cubeType)
    {
        for (int i = 0; i < 6; i++)
        {
            if (data.GetNeighbor((int)pos.x, (int)pos.y, (int)pos.z, (Direction)i) == 0)
            {
                MakeFace((Direction)i, pos, cubeType);
            }
        }
    }
    int GetVertexIndex(VertexSignature signature)
    {
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
        VertexSignature signature;
        signature.normal = dir;

        Vector3[] faceVertices = CubeMeshData.faceVertices(dir, pos);
        int[] triangleIndices = new int[4];
        signature.cubeType = cubeType;

        for (int i = 0; i < 4; i++)
        {
            signature.position = faceVertices[i];
            Vector3 uv = CubeMeshData.ProjectPositionToUV(signature.position, dir);
            uv.z = signature.cubeType;

            int vertex = GetVertexIndex(signature);
            triangleIndices[i] = vertex;
        }

        triangles.Add(triangleIndices[0]);
        triangles.Add(triangleIndices[1]);
        triangles.Add(triangleIndices[2]);

        triangles.Add(triangleIndices[0]);
        triangles.Add(triangleIndices[2]);
        triangles.Add(triangleIndices[3]);
    }

    void PrepareMeshData()
    {
        foreach (var pair in verticesDict)
        {
            int index = pair.Value;
            vertices.Add(pair.Key.position);
            normals.Add(CubeMeshData.offsets[(int)pair.Key.normal].ToVector());

            Vector3 uv = CubeMeshData.ProjectPositionToUV(pair.Key.position, pair.Key.normal);
            uv.z = pair.Key.cubeType;
            uvs.Add(uv);
        }
    }

    void UpdateMesh()
    {
        PrepareMeshData();

        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        mesh.SetUVs(0, uvs);
        meshCollider.sharedMesh = mesh;
    }

    struct VertexSignature
    {
        public Vector3 position;
        public Direction normal;

        public int cubeType;
    };
}
