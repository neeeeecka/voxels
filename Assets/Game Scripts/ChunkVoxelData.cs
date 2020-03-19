using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class ChunkVoxelData : MonoBehaviour
{

    private int[] raw = new int[32*32*32];
    public static int size = 32;

    Dictionary<VertexSignature, int> verticesDict = new Dictionary<VertexSignature, int>();
    int vertexCount = 0;

    List<int> triangles = new List<int>();

    Vector3[] _normals;
    Vector3[] _uvs;
    Vector3[] _vertices;

    Mesh mesh;
    public MeshFilter meshFilter;
    public MeshCollider meshCollider;

    public bool threadFinished = true;
    List<Action> functionsQueue = new List<Action>();

    public int blocksGenerated = 0;


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

    public void RegenerateAsync()
    {
        Async(ChunkUpdate);
    }
    public void RegenerateSync()
    {
        ChunkUpdate();
    }

    public void ChunkUpdate()
    {
        MakeChunk();
        PrepareMeshData();

        int[] triArr = triangles.ToArray();

        Action toThread = () =>
        {
            mesh.Clear();
            mesh.vertices = _vertices;
            mesh.normals = _normals;
            mesh.triangles = triArr;
            mesh.SetUVs(0, new List<Vector3>(_uvs));

            meshCollider.sharedMesh = mesh;
            threadFinished = true;
        };

        functionsQueue.Add(toThread);
    }
    public void MakeChunk()
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
                MakeCube(x, y, z, cubeType);
                blocksGenerated++;
            }
        }
    }

    void MakeCube(int x, int y, int z, int cubeType)
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
    void PrepareMeshData()
    {
        _vertices = new Vector3[vertexCount];
        _normals = new Vector3[vertexCount];
        _uvs = new Vector3[vertexCount];

        foreach (var pair in verticesDict)
        {
            int index = pair.Value;
            CubeMeshData.DataCoordinate coord = CubeMeshData.offsets[(int)pair.Key.normal];
            Vector3 uv = CubeMeshData.ProjectPositionToUV(pair.Key.position, pair.Key.normal);
            uv.z = pair.Key.cubeType;

            _vertices[index] = pair.Key.position;
            _normals[index] = new Vector3(coord.x, coord.y, coord.z);
            _uvs[index] = uv;
        }
    }
    public void Async(Action func)
    {
        Thread thread = new Thread(new ThreadStart(func));
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