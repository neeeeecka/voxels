using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using Unity.Jobs;
using Unity.Collections;
struct ChunkUpdateJob : IJobParallelFor
{
    public NativeArray<ChunkData> chunkDataArray;

    public void Execute(int index)
    {
        var data = chunkDataArray[index];
        data.Update();
        chunkDataArray[index] = data;
    }
}

public struct ChunkData
{
    public bool isEmpty;
    private int[] raw;
    Dictionary<VertexSignature, int> verticesDict;
    public int blocksGenerated;
    int vertexCount;
    public List<int> triangles;
    public Vector3[] _normals;
    public Vector4[] _uvs;
    public Vector3[] _vertices;
    int verticesCounter;

    public ChunkData(ChunkVoxelData chunkVoxelData)
    {
        isEmpty = true;
        blocksGenerated = 0;
        vertexCount = 0;
        verticesCounter = 0;
        triangles = new List<int>();
        _normals = new Vector3[0];
        _uvs = new Vector4[0];
        _vertices = new Vector3[0];
        this.verticesDict = new Dictionary<VertexSignature, int>();

        this.raw = chunkVoxelData.raw;
    }

    public void Update()
    {
        isEmpty = true;
        verticesCounter = 0;
        _normals = new Vector3[0];
        _uvs = new Vector4[0];
        _vertices = new Vector3[0];

        MakeChunk();
        PrepareMeshData();
    }

    void MakeChunk()
    {
        // public bool MakeChunk(bool needsGlobalChange)
        // {
        blocksGenerated = 0;
        vertexCount = 0;
        verticesDict.Clear();
        triangles.Clear();

        int len = 16 * 16 * 16;

        for (int i = 0; i < len; i++)
        {
            int cubeType = raw[i];
            if (cubeType != 0)
            {
                int x = i % 16;
                int y = (i / 16) % 16;
                int z = i / (16 * 16);
                MakeCube(x, y, z, cubeType);
                blocksGenerated++;
            }
        }
    }

    void MakeCube(int x, int y, int z, int cubeType)
    {
        // NeighborGetter neighborGetter = needsGlobalChange ? globalNeighborGetter : localNeighborGetter;


        bool hasBlockUpwards = GetLocalNeighbor(x, y, z, Direction.Up) != 0;

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
            if (GetLocalNeighbor(x, y, z, dir) == 0)
            {
                MakeFace(dir, x, y, z, textureType);
            }
        }

    }
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

    public void PrepareMeshData()
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

    public int GetCell(int x, int y, int z)
    {
        return raw[x + 16 * (y + 16 * z)];
    }
    public void SetCell(int x, int y, int z, int val)
    {
        if (val != 0)
        {
            isEmpty = false;
        }
        raw[x + 16 * (y + 16 * z)] = val;
    }
    public int GetLocalNeighbor(int x, int y, int z, Direction dir)
    {
        CubeMeshData.DataCoordinate checkOffset = CubeMeshData.offsets[(int)dir];
        CubeMeshData.DataCoordinate neighborCoordinate = new CubeMeshData.DataCoordinate(
            x + checkOffset.x,
            y + checkOffset.y,
            z + checkOffset.z
        );

        if (neighborCoordinate.x < 0 || neighborCoordinate.x >= 16)
        {
            return 0;
        }
        if (neighborCoordinate.y < 0 || neighborCoordinate.y >= 16)
        {
            return 0;
        }
        if (neighborCoordinate.z < 0 || neighborCoordinate.z >= 16)
        {
            return 0;
        }

        return GetCell(neighborCoordinate.x, neighborCoordinate.y, neighborCoordinate.z);
    }
}

public class ChunkVoxelData : MonoBehaviour
{

    public Vector3 chunkPos = Vector3.zero;
    public int[] raw = new int[16 * 16 * 16];
    public static int size = 16;

    Mesh mesh;
    public MeshFilter meshFilter;
    public MeshCollider meshCollider;

    public MeshRenderer meshRenderer;

    public bool threadFinished = true;
    List<Action> functionsQueue = new List<Action>();

    public int blocksGenerated = 0;
    public TerrainGeneratorAsync terrain;

    List<int> triangles = new List<int>();

    Vector3[] _normals;
    Vector4[] _uvs;
    Vector3[] _vertices;
    public bool isEmpty;

    void Start()
    {

        mesh = new Mesh();
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter.mesh = mesh;
        mesh.MarkDynamic();
        RegenerateAsync(true);

        var data = new ChunkData(this);
        data.Update();
        ChunkUpdate(data);

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
        // Async(ChunkUpdate, needsGlobalChange);
        // ChunkUpdate(needsGlobalChange);
    }

    int verticesCounter = 0;

    public int GetCell(int x, int y, int z)
    {
        return raw[x + 16 * (y + 16 * z)];
    }
    public void SetCell(int x, int y, int z, int val)
    {
        if (val != 0)
        {
            isEmpty = false;
        }
        raw[x + 16 * (y + 16 * z)] = val;
    }

    void ChunkUpdate(ChunkData chunkData)
    {
        // public void ChunkUpdate(bool needsGlobalChange)
        // {
        // MakeChunk(needsGlobalChange);
        // PrepareMeshData();

        // ChunkData chunkData = new ChunkData(this);
        chunkData.Update();
        // Debug.Log();
        blocksGenerated = chunkData.blocksGenerated;
        _vertices = chunkData._vertices;
        _normals = chunkData._normals;
        triangles = chunkData.triangles;
        _uvs = chunkData._uvs;

        int[] triArr = triangles.ToArray();

        // Action toMainThread = () =>
        // {
        mesh.Clear();
        mesh.vertices = _vertices;
        mesh.normals = _normals;
        mesh.triangles = triArr;
        mesh.SetUVs(0, new List<Vector4>(_uvs));
        mesh.RecalculateTangents();

        meshRenderer.shadowCastingMode = ShadowCastingMode.On;
        meshCollider.sharedMesh = mesh;
        // threadFinished = true;

        // AssetDatabase.CreateAsset(mesh, "Assets/_Generated/" + "chunkie.asset");
        // AssetDatabase.SaveAssets();
        // };

        // functionsQueue.Add(toMainThread);
        // }
    }



    // public int GetGlobalNeighbor(int x, int y, int z, Direction dir)
    // {
    //     x = x + size * (int)chunkPos.x;
    //     y = y + size * (int)chunkPos.y;
    //     z = z + size * (int)chunkPos.z;

    //     CubeMeshData.DataCoordinate checkOffset = CubeMeshData.offsets[(int)dir];
    //     CubeMeshData.DataCoordinate neighborCoordinate = new CubeMeshData.DataCoordinate(
    //         x + checkOffset.x,
    //         y + checkOffset.y,
    //         z + checkOffset.z
    //     );

    //     return terrain.GetBlockAt(neighborCoordinate.x, neighborCoordinate.y, neighborCoordinate.z);
    // }

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

