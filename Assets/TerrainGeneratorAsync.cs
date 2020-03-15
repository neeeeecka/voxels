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

        AssetDatabase.CreateAsset(mesh, "Assets/Temp/mesh.asset");

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

        Vector3[] vertexArr = vertices.ToArray();
        int[] triArr = triangles.ToArray();
        Vector3[] normalsArr = normals.ToArray();

        Action toThread = () =>
        {
            mesh.Clear();
            mesh.vertices = vertexArr;
            mesh.triangles = triArr;
            mesh.normals = normalsArr;
            mesh.SetUVs(0, uvs);
            meshCollider.sharedMesh = mesh;
            threadFinished = true;

        };

        functionsQueue.Add(toThread);
    }

    public void MakeChunk()
    {
        blocksGenerated = 0;
        vertexCount = 0;
        vertices.Clear();
        normals.Clear();
        verticesDict.Clear();
        uvs.Clear();
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
            Vector3 uv = CubeMeshData.ProjectPositionToUV(signature.position, dir);
            uv.z = signature.cubeType;

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
        foreach (var pair in verticesDict)
        {
            int index = pair.Value;
            vertices.Add(pair.Key.position);
            CubeMeshData.DataCoordinate coord = CubeMeshData.offsets[(int)pair.Key.normal];
            normals.Add(new Vector3(coord.x, coord.y, coord.z));

            Vector3 uv = CubeMeshData.ProjectPositionToUV(pair.Key.position, pair.Key.normal);
            uv.z = pair.Key.cubeType;
            uvs.Add(uv);
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
}
