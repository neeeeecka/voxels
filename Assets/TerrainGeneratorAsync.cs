using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Threading;
using Unity.Mathematics;

public class TerrainGeneratorAsync : MonoBehaviour
{
    private float lastScale = 0, lastHeight = 0;
    [Range(0, 0.1f)]
    public float scale = 0.01f;
    [Range(1, 32)]

    public int maxTerrainHeight = 10;
    private int lastWorld;
    public int chunkSize;
    public int blocksGenerated = 0;
    
    Dictionary<VertexSignature, int> verticesDict = new Dictionary<VertexSignature, int>();
    VertexSignature[] vertexEntries = new VertexSignature[10000];
    int vertexCount = 0;

    List<int> triangles = new List<int>();

    Vector3[] _normals;
    Vector3[] _uvs;
    Vector3[] _vertices;

    Mesh mesh;
    VoxelData data;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    public bool threadFinished = true;

    void Start()
    {

        lastScale = scale;
        lastHeight = maxTerrainHeight;

        mesh = new Mesh();
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        meshFilter.mesh = mesh;
        mesh.MarkDynamic();

        SetWorldData();
        ChunkUpdate();

        //AssetDatabase.CreateAsset(mesh, "Assets/Temp/mesh.asset");
        //GetVertexEntry(new VertexSignature(), 12);
    }

    List<Action> functionsQueue = new List<Action>();


    private void Update()
    {
        if (lastScale != scale || lastHeight != maxTerrainHeight)
        {
            lastScale = scale;
            lastHeight = maxTerrainHeight;
        }
        while(functionsQueue.Count > 0){
            Action func = functionsQueue[0];
            functionsQueue.RemoveAt(0);
            func();

        }
    }
    public void EditWorld(int x, int y, int z, int cubeType)
    {
        if (threadFinished)
        {
            data.SetCell(x, y, z, cubeType);

            //Async(ChunkUpdate);
            ChunkUpdate();
        }
    }

    void SetWorldData()
    {
        data = new VoxelData(chunkSize, chunkSize, chunkSize);

        for (int x = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++)
            {
                int yVal = GetNoiseValue(z, x);

                for (int y = yVal - 1; y >= 0; y--)
                {
                    int cubeType = 4 - Mathf.FloorToInt((float)yVal / (float)maxTerrainHeight * 4);
                    data.SetCell(x, y, z, cubeType);
                }
            }
        }

    }

    bool first = true;
    public void ChunkUpdate() {
        //if (first)
        //{
            MakeChunk();
        //    first = false;
        //}

        PrepareMeshData();

        //Vector3[] vertexArr = vertices.ToArray();
        int[] triArr = triangles.ToArray();
        //Vector3[] normalsArr = normals.ToArray();

        Action toThread = () =>
        {
            mesh.Clear();

          
            mesh.vertices = _vertices;
            mesh.normals = _normals;
            mesh.triangles = triArr;
            mesh.SetUVs(0, new List<Vector3>(_uvs));

            //mesh.normals = normalsArr;
            //mesh.SetUVs(0, uvs);
            //mesh.vertices = vertexArr;

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

        int len = 32 * 32 * 32;

        for (int i = 0; i < len; i++)
        {
            int cubeType = data.raw[i];
            if (cubeType != 0)
            {
                int x = i % 32;
                int y = (i / 32) % 32;
                int z = i / (32 * 32);
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
            if (data.GetNeighbor(x, y, z, dir) == 0)
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


    public void Async(Action func) {
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
            uint h = 0x811c9dc5;
            h = (h ^ (uint)position.x) * 0x01000193;
            h = (h ^ (uint)position.y) * 0x01000193;
            h = (h ^ (uint)position.z) * 0x01000193;
            h = (h ^ (uint)normal) * 0x01000193;
            h = (h ^ (uint)cubeType) * 0x01000193;

            return (int)h;
        }
    };
    int GetNoiseValue(float x, float y)
    {
        return Mathf.FloorToInt(Mathf.PerlinNoise(x * scale, y * scale) * maxTerrainHeight);
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
    //int GetVertexEntry(VertexSignature signature, int vertexIndex)
    //{
    //    int count = vertexEntries.Length;

    //    if(vertexEntries[vertexIndex] )

    //    return vertexIndex;
    //}
}
