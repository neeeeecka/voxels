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

    private Vector3 lastWorld;
    public Vector3 world;
    public int blocksGenerated = 0;

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
        lastWorld = world;

        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        mesh.MarkDynamic();
        MakeWorld();
        GetComponent<MeshCollider>().sharedMesh = mesh;

    }

    private void Update()
    {
        if (lastDivisor != divisor || lastSeed != seed || lastWorld != world)
        {
            lastDivisor = divisor;
            lastSeed = seed;
            lastWorld = world;
            MakeWorld();
        }
        // seed += 0.1f;
    }

    void MakeWorld()
    {
        blocksGenerated = 0;
        vertexCount = 0;
        vertices.Clear();
        normals.Clear();
        verticesDict.Clear();
        uvs.Clear();
        triangles.Clear();

        int depth = (int)world.z, width = (int)world.x, height = (int)world.y;

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
                        blocksGenerated++;
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
    BoolInt GetVertexIndex(VertexSignature signature)
    {
        //return new vertex index, 
        //if it doesn't exist we create new, 
        //if it does exist return last index

        int index;
        bool add = false;
        if (!verticesDict.TryGetValue(signature, out index))
        {
            index = vertexCount++;
            verticesDict.Add(signature, index);
            add = true;
        }
        BoolInt bi;
        bi.INT = index;
        bi.BOOL = add;
        return bi;
    }
    struct BoolInt
    {
        public int INT;
        public bool BOOL;
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
        signature.cubeType = cubeType;

        // signature.position = faceVertices[0];
        // Vector2 uva = CubeMeshData.GetVertexUV(dir, 0, cubeType);
        // int a = GetVertexIndex(signature);

        // signature.position = faceVertices[1];
        // int b = GetVertexIndex(signature);

        // signature.position = faceVertices[2];
        // int c = GetVertexIndex(signature);

        // signature.position = faceVertices[3];
        // int d = GetVertexIndex(signature);

        for (int i = 0; i < 4; i++)
        {
            signature.position = faceVertices[i];
            Vector2 uv = CubeMeshData.GetVertexUV(dir, i, cubeType);
            BoolInt res = GetVertexIndex(signature);
            triangleIndices[i] = res.INT;
            if (res.BOOL)
            {
                uvs.Add(uv);
            }
            else
            {
                uvs[res.INT] = uv;
            }
        }

        triangles.Add(triangleIndices[0]);
        triangles.Add(triangleIndices[1]);
        triangles.Add(triangleIndices[2]);

        triangles.Add(triangleIndices[0]);
        triangles.Add(triangleIndices[2]);
        triangles.Add(triangleIndices[3]);

        // uvs.AddRange(CubeMeshData.faceUVs(dir, cubeType));

        // int zero = verticesDict.Count - 4;

        // triangles.Add(a);
        // triangles.Add(b);
        // triangles.Add(c);

        // triangles.Add(a);
        // triangles.Add(c);
        // triangles.Add(d);


    }

    void SetFinalData()
    {
        foreach (var pair in verticesDict)
        {
            int index = pair.Value;
            vertices.Add(pair.Key.position);
            normals.Add(CubeMeshData.offsets[(int)pair.Key.normal].ToVector());

            // Vector3 uv = ProjectPositionToUV(pair.key.position, pair.key.normal);
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
        mesh.uv = uvs.ToArray();
        // mesh.RecalculateNormals();
    }

    struct VertexSignature
    {
        public Vector3 position;
        public Direction normal;

        public int cubeType;
    };
}
