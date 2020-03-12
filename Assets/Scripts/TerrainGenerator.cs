using System.Collections;
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
        // seed += 0.1f;
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
        ClearWorld();
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
                    int cubeType = (int)Random.Range(1, 4);
                    // int cubeType = 1;
                    data.SetCell(x, y, z, cubeType);
                }
            }
        }

        DrawWorld();
        UpdateMesh();

    }

    public void DrawWorld()
    {

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
        ClearWorld();

        data.SetCell(x, y, z, cubeType);
        DrawWorld();

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
        int index;
        if (!verticesDict.TryGetValue(signature, out index))
        {
            index = vertexCount++;
            verticesDict.Add(signature, index);
        }

        return index;
    }

    void MakeFaceClean(Direction dir, Vector3 pos, int cubeType)
    {

        // vertices.AddRange(CubeMeshData.faceVertices(dir, pos));
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
        mesh.Clear();

        PrepareMeshData();

        // mesh.vertices = JustConvertDictionaryToVertices();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        mesh.SetUVs(0, uvs);
        meshCollider.sharedMesh = mesh;
        // mesh.uv = uvs.ToArray();
        // mesh.RecalculateNormals();
    }

    struct VertexSignature
    {
        public Vector3 position;
        public Direction normal;

        public int cubeType;
    };
}
