﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public class ChunkVoxelData : MonoBehaviour
{

    public Vector3 chunkPos = Vector3.zero;
    private int[] raw = new int[16 * 16 * 16];
    public static int size = 16;

    Dictionary<VertexSignature, int> verticesDict = new Dictionary<VertexSignature, int>();
    int vertexCount = 0;

    List<int> triangles = new List<int>();

    Vector3[] _normals;
    Vector4[] _uvs;
    Vector3[] _vertices;

    Mesh mesh;
    public MeshFilter meshFilter;
    public MeshCollider meshCollider;

    public MeshRenderer meshRenderer;

    public bool threadFinished = true;
    List<Action> functionsQueue = new List<Action>();

    public int blocksGenerated = 0;
    public TerrainGeneratorAsync terrain;

    public bool ready = false;
    public bool isEmpty = true;

    delegate int NeighborGetter(int x, int y, int z, Direction dir);

    NeighborGetter globalNeighborGetter;
    NeighborGetter localNeighborGetter;

    ChunkVoxelData()
    {
        globalNeighborGetter = delegate (int x, int y, int z, Direction dir)
            {
                return GetGlobalNeighbor(x, y, z, dir);
            };
        localNeighborGetter = delegate (int x, int y, int z, Direction dir)
            {
                return GetLocalNeighbor(x, y, z, dir);
            };

    }

    void Start()
    {


        mesh = new Mesh();
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        meshRenderer = GetComponent<MeshRenderer>();
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

    public void SetRaw(int[] arr, bool isEmpty)
    {
        this.isEmpty = isEmpty;
        this.raw = arr;
    }

    public void RegenerateAsync(bool needsGlobalChange)
    {
        //Debug.Log("Needs global change: " + needsGlobalChange);
        //Debug.Log("chunk " + chunkPos);

        needsGlobalChange = true;
        Async(ChunkUpdate, needsGlobalChange);
        //ChunkUpdate(needsGlobalChange)
    }
    public void RegenerateSync(bool needsGlobalChange)
    {
        ChunkUpdate(true);
        //Debug.Log("reg");
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
            mesh.RecalculateTangents();

            meshRenderer.shadowCastingMode = ShadowCastingMode.On;
            meshCollider.sharedMesh = mesh;
            threadFinished = true;

            // AssetDatabase.CreateAsset(mesh, "Assets/_Generated/" + "chunkie.asset");
            // AssetDatabase.SaveAssets();
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
        NeighborGetter neighborGetter = needsGlobalChange ? globalNeighborGetter : localNeighborGetter;

        bool hasBlockUpwards = neighborGetter(x, y, z, Direction.Up) != 0;

        for (int i = 0; i < 6; i++)
        {
            Direction dir = (Direction)i;
            int textureType = 1;
            //textureIndex = realIndex + 1 cause air = 0
            // textureType = 8;
            //TODO: Change for custom behaviour for each block
            if (cubeType == 1)
            {
                if (dir == Direction.Up)
                {
                    textureType = 5; //grass top
                }
                else if (!hasBlockUpwards)
                {
                    textureType = 9; //grass side
                }

                // textureType = 5;

            }
            if (neighborGetter(x, y, z, dir) == 0)
            {
                MakeFace(dir, x, y, z, textureType);
            }
        }

    }
    int verticesCounter = 0;
    void MakeFace(Direction dir, int x, int y, int z, int cubeType)
    {
        VertexSignature signature;
        signature.normal = dir;

        Vector3[] faceVertices = CubeMeshData.faceVertices((int)dir, x, y, z);
        int[] triangleIndices = new int[4];
        signature.cubeType = (char)cubeType;

        for (int i = 0; i < 4; i++)
        {
            signature.index = verticesCounter;
            verticesCounter++;

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

            uv.w = pair.Key.cubeType;
            // uv.w = 9;

            _vertices[index] = pair.Key.position;
            _normals[index] = new Vector3(coord.x, coord.y, coord.z);

            //get vertex chunk position
            Vector3 chunkPos = pair.Key.position;
            Vector3 normal = CubeMeshData.offsets[(int)pair.Key.normal].ToVector();

            _uvs[index] = uv;

            // int cx = Mathf.FloorToInt(chunkPos.x) + (int)normal.x;
            // int cy = Mathf.FloorToInt(chunkPos.y) + (int)normal.y;
            // int cz = Mathf.FloorToInt(chunkPos.z) + (int)normal.y;

            // int side1 = GetNeighbor(cx, cy, cz, Direction.East);
            // int side2 = GetNeighbor(cx, cy, cz, Direction.West);
            // int corner = GetNeighbor(cx, cy, cz, Direction.Down);

            //int side3 = GetNeighbor(cx, cy, cz, Direction.North);
            //int side4 = GetNeighbor(cx, cy, cz, Direction.South);


            //uv.w = (side1 + side2 + side3 + side4) + 1;
            //uv.w = (side1 + side2 + corner) + 1;

            //uv.w = VertexAO(side1, side2, corner);
            // uv.w = 1; 
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
        public char cubeType;
        public int index;

        //Blocks will share vertices if faces have same normals and position 
        // public override int GetHashCode()
        // {
        //     char x = (char)(position.x * 2);
        //     char y = (char)(position.y * 2);
        //     char z = (char)(position.z * 2);

        //     char n = (char)normal;
        //     char c = cubeType;

        //     // int p = (((x + y * size * 2) * size * 2 + z) * 6 + n) * 4 + c;
        //     int p = (((x + y * size * 2) * size * 2 + z) * 6 + n);
        //     //ignore cube type, 
        //     //treat as same block
        //     //...cant UV map that thing

        //     return p;
        // }

        //will use quads before I figure out how to do UV mapping for optimized meshes
        public override int GetHashCode()
        {
            return index;
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
        if (val != 0)
        {
            isEmpty = false;
        }
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
    public int GetLocalNeighbor(int x, int y, int z, Direction dir)
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