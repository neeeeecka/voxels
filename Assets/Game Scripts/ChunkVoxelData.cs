﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class ChunkVoxelData : MonoBehaviour
{

    public Vector3 chunkPos = Vector3.zero;
    private int[] raw = new int[32 * 32 * 32];
    public static int size = 32;

    Dictionary<VertexSignature, int> verticesDict = new Dictionary<VertexSignature, int>();
    int vertexCount = 0;

    List<int> triangles = new List<int>();

    Vector3[] _normals;
    Vector4[] _uvs;
    Vector3[] _vertices;

    Mesh mesh;
    public MeshFilter meshFilter;
    public MeshCollider meshCollider;

    public bool threadFinished = true;
    List<Action> functionsQueue = new List<Action>();

    public int blocksGenerated = 0;
    public TerrainGeneratorAsync terrain;

    public bool ready = false;

    void Start()
    {
        mesh = new Mesh();
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        meshFilter.mesh = mesh;
        mesh.MarkDynamic();
    }

    private void Update()
    {
        while (functionsQueue.Count > 0)
        {
            Action func = functionsQueue[0];
            functionsQueue.RemoveAt(0);
            func();
        }
    }

    public void SetRaw(int[] arr)
    {
        this.raw = arr;
    }

    public void RegenerateAsync(bool needsGlobalChange)
    {
        //Debug.Log("Needs global change: " + needsGlobalChange);
        Async(ChunkUpdate, needsGlobalChange);
    }
    public void RegenerateSync(bool needsGlobalChange)
    {
        ChunkUpdate(needsGlobalChange);
    }

    public void ChunkUpdate(bool needsGlobalChange)
    {
        MakeChunk(needsGlobalChange);
        PrepareMeshData();

        int[] triArr = triangles.ToArray();

        Action toMainThread = () =>
        {
            mesh.Clear();
            mesh.vertices = _vertices;
            mesh.normals = _normals;
            mesh.triangles = triArr;
            mesh.SetUVs(0, new List<Vector4>(_uvs));

            meshCollider.sharedMesh = mesh;
            threadFinished = true;
        };

        functionsQueue.Add(toMainThread);
    }
    public void MakeChunk(bool needsGlobalChange)
    {
        blocksGenerated = 0;
        vertexCount = 0;
        verticesDict.Clear();
        triangles.Clear();

        int len = size * size * size;

        for (int i = 0; i < len; i++)
        {
            int cubeType = raw[i];
            if (cubeType != 0)
            {
                int x = i % size;
                int y = (i / size) % size;
                int z = i / (size * size);
                MakeCube(x, y, z, cubeType, needsGlobalChange);
                blocksGenerated++;
            }
        }
    }

    void MakeCube(int x, int y, int z, int cubeType, bool needsGlobalChange)
    {
        if (needsGlobalChange)
        {
            for (int i = 0; i < 6; i++)
            {
                Direction dir = (Direction)i;
                if (GetGlobalNeighbor(x, y, z, dir) == 0)
                {
                    MakeFace(dir, x, y, z, cubeType);
                }
            }
        }
        else
        {
            for (int i = 0; i < 6; i++)
            {
                Direction dir = (Direction)i;
                if (GetNeighbor(x, y, z, dir) == 0)
                {
                    MakeFace(dir, x, y, z, cubeType);
                }
            }
        }

    }
    void MakeFace(Direction dir, int x, int y, int z, int cubeType)
    {
        VertexSignature signature;
        signature.normal = dir;

        Vector3[] faceVertices = CubeMeshData.faceVertices((int)dir, x, y, z);
        int[] triangleIndices = new int[4];
        signature.cubeType = cubeType;

        for (int i = 0; i < 4; i++)
        {
            signature.position = faceVertices[i];

            int vertex = GetVertexIndex(signature);
            //int vertex2 = GetVertexEntry(signature);

            triangleIndices[i] = vertex;
        }

        triangles.AddRange(new int[6] {
            triangleIndices[0],
            triangleIndices[1],
            triangleIndices[2],
            triangleIndices[0],
            triangleIndices[2],
            triangleIndices[3]
            }
        );

    }

    float VertexAO(int side1, int side2, float corner)
    {
        if (side1 == 1 && side2 == 1)
        {
            return 0;
        }
        return 3 - (side1 + side2 + corner);
    }

    void PrepareMeshData()
    {
        _vertices = new Vector3[vertexCount];
        _normals = new Vector3[vertexCount];
        _uvs = new Vector4[vertexCount];

        foreach (var pair in verticesDict)
        {
            int index = pair.Value;
            CubeMeshData.DataCoordinate coord = CubeMeshData.offsets[(int)pair.Key.normal];
            Vector4 uv = CubeMeshData.ProjectPositionToUV(pair.Key.position, pair.Key.normal);
            uv.z = pair.Key.cubeType;

            _vertices[index] = pair.Key.position;
            _normals[index] = new Vector3(coord.x, coord.y, coord.z);

            //Vector3 worldPos = pair.Key.position;
            //Vector3 normal = CubeMeshData.offsets[(int)pair.Key.normal].ToVector();

            //worldPos -= normal * 0.5f;

            //int x = Mathf.FloorToInt(worldPos.x);
            //int y = Mathf.FloorToInt(worldPos.y);
            //int z = Mathf.FloorToInt(worldPos.z);

            //int side1 = GetNeighbor(x, y, z, Direction.North) != 0 ? 1 : 0;
            //int side2 = GetNeighbor(x, y, z, Direction.West) != 0 ? 1 : 0;

            //uv.w = VertexAO(side1, side2, 1);

            uv.w = 1;
            _uvs[index] = uv;
        }
    }
    public void Async(Action<bool> func, bool param)
    {
        //Thread thread = new Thread(new ThreadStart(func));
        Thread thread = new Thread(() => func(param));

        thread.Start();
        threadFinished = false;
    }
    struct VertexSignature
    {
        public Vector3 position;
        public Direction normal;
        public int cubeType;

        public override int GetHashCode()
        {
            int x = (int)(position.x * 2);
            int y = (int)(position.y * 2);
            int z = (int)(position.z * 2);

            int n = (int)normal;
            int c = cubeType;

            int p = (((x + y * size * 2) * size * 2 + z) * 6 + n) * 4 + c;
            return p;
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            return obj.GetHashCode() == GetHashCode();
        }
    };
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

    public int GetCell(int x, int y, int z)
    {
        return raw[x + size * (y + size * z)];
    }
    public void SetCell(int x, int y, int z, int val)
    {
        raw[x + size * (y + size * z)] = val;
    }
    public int GetGlobalNeighbor(int x, int y, int z, Direction dir)
    {
        x = x + size * (int)chunkPos.x;
        y = y + size * (int)chunkPos.y;
        z = z + size * (int)chunkPos.z;

        CubeMeshData.DataCoordinate checkOffset = CubeMeshData.offsets[(int)dir];
        CubeMeshData.DataCoordinate neighborCoordinate = new CubeMeshData.DataCoordinate(
            x + checkOffset.x,
            y + checkOffset.y,
            z + checkOffset.z
        );

        return terrain.GetBlockAt(neighborCoordinate.x, neighborCoordinate.y, neighborCoordinate.z);
    }
    public int GetNeighbor(int x, int y, int z, Direction dir)
    {
        CubeMeshData.DataCoordinate checkOffset = CubeMeshData.offsets[(int)dir];
        CubeMeshData.DataCoordinate neighborCoordinate = new CubeMeshData.DataCoordinate(
            x + checkOffset.x,
            y + checkOffset.y,
            z + checkOffset.z
        );

        if (neighborCoordinate.x < 0 || neighborCoordinate.x >= size)
        {
            return 0;
        }
        if (neighborCoordinate.y < 0 || neighborCoordinate.y >= size)
        {
            return 0;
        }
        if (neighborCoordinate.z < 0 || neighborCoordinate.z >= size)
        {
            return 0;
        }

        return GetCell(neighborCoordinate.x, neighborCoordinate.y, neighborCoordinate.z);
    }
}

public enum Direction
{
    North,
    East,
    South,
    West,
    Up,
    Down
}