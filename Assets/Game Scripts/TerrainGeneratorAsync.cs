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
    public static int chunkSize = 32;
    public int blocksGenerated = 0;
    
    Dictionary<VertexSignature, int> verticesDict = new Dictionary<VertexSignature, int>();
    int vertexCount = 0;

    int[] vertexEntries = new int[5059850];
    List<VertexSignature> vertexEntriesList = new List<VertexSignature>();



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
        data = new VoxelData(chunkSize);

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

    public void ChunkUpdate() {
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


        int len = chunkSize * chunkSize * chunkSize;

        for (int i = 0; i < len; i++)
        {
            int cubeType = data.raw[i];
            if (cubeType != 0)
            {
                int x = i % chunkSize;
                int y = (i / chunkSize) % chunkSize;
                int z = i / (chunkSize * chunkSize);
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
        //_vertices = vertices.ToArray();

        //Debug.Log("Vertex array entries: " + vertexEntriesList.Count);
        //Debug.Log(vertexCount2);
        //Debug.Log("Vertex dictionary entries: " + vertexCount);


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

        //for (int i = 0; i < vertexCount2; i++)
        //{
        //    VertexSignature signature = vertexEntriesList[i];
        //    CubeMeshData.DataCoordinate coord = CubeMeshData.offsets[(int)signature.normal];
        //    Vector3 uv = CubeMeshData.ProjectPositionToUV(signature.position, signature.normal);
        //    uv.z = signature.cubeType;

        //    _vertices[i] = signature.position;
        //    _normals[i] = new Vector3(coord.x, coord.y, coord.z);
        //    _uvs[i] = uv;
        //}
    }


    public void Async(Action func) {
        Thread thread = new Thread(new ThreadStart(func));
        thread.Start();
        threadFinished = false;
    }

    //private int MakeHashCode(ref VertexSignature sign)
    //{
    //    int n = 0;
    //    n = (int)sign.position.x +
    //        (int)sign.position.y * chunkSize +
    //        (int)sign.position.z * chunkSize * chunkSize +
    //        (int)sign.normal * chunkSize * chunkSize * chunkSize +
    //        sign.cubeType * chunkSize * chunkSize * chunkSize * chunkSize
    //        ;
    //    return n;
    //}
 

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

            int p = (((x + y * chunkSize * 2) * chunkSize * 2 + z) * 6 + n) * 4 + c;
            return p;
        }
        public override bool Equals(object obj)
        {
            if(obj == null)
            {
                return false;
            }
            return obj.GetHashCode() == GetHashCode();
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
    int firstVertexHash = 0;
    int vertexCount2 = 0;

    int GetVertexEntry(VertexSignature signature)
    {
        int hash = signature.GetHashCode();
        int vertexIndex = -1;

        if(vertexEntries[hash] == 0)
        {
            if(vertexCount2 == 0)
            {
                vertexIndex = vertexCount2;
                vertexEntries[hash] = vertexIndex;
                firstVertexHash = hash;
                vertexCount2++;
            }
            else 
            {
                if (hash == firstVertexHash)
                {
                    vertexIndex = 0;
                }
                else
                {
                    vertexIndex = vertexCount2;
                    vertexEntries[hash] = vertexIndex;
                    vertexCount2++;
                }
            }
        }
        else
        {
            vertexIndex = vertexEntries[hash];
        }

        return vertexIndex;
    }

}
