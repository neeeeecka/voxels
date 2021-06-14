using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Threading;
using Unity.Mathematics;

public class TerrainGeneratorAsync : MonoBehaviour
{
    public Vector3 worldDimensions = new Vector3(3, 3, 3);

    private float oldExp = 0;
    public float exponent = 2;

    public int terraces = 0;

    public int waterlevel = 5;

    private float oldms = 0;
    [Range(0, 1f)]
    public float mountainScale = 0.01f;

    private float oldss = 0;
    [Range(0, 1f)]
    public float stonesScale = 0.1f;

    private float oldds = 0;
    [Range(0, 1f)]
    public float detailScale = 0.2f;

    public int maxTerrainHeight = 10;
    public int blocksGenerated = 0;

    public Dictionary<Vector3, ChunkVoxelData> chunks = new Dictionary<Vector3, ChunkVoxelData>();
    //public int[] chunks = new int[3 * 3 * 3];

    public bool threadFinished = true;

    public GameObject chunkPrefab;

    void Start()
    {
        oldExp = exponent;
        oldms = mountainScale;
        oldss = stonesScale;
        oldds = detailScale;

        for (int x = 0; x < worldDimensions.x; x++)
        {
            for (int y = 0; y < worldDimensions.y; y++)
            {
                for (int z = 0; z < worldDimensions.z; z++)
                {
                    chunkUpdateQueue.Add(MakeChunkAt(x, y, z));
                }
            }
        }

        chunkUpdateQueue.Add(MakeChunkAt(12, 3, 12));


        UpdateNextChunk();


        // if (c < 20)
        // {
        //     ChunkVoxelData ch = GetChunk(-1, 0, 0);
        //     Debug.Log(ch);
        // }


    }

    List<Action> functionsQueue = new List<Action>();
    List<ChunkVoxelData> chunkUpdateQueue = new List<ChunkVoxelData>();

    public Transform player;


    int c = 0;

    private void Update()
    {

        // if (
        //     oldExp != exponent ||
        //     oldms != mountainScale ||
        //     oldds != detailScale ||
        //     oldss != stonesScale
        // )
        // {
        //     oldExp = exponent;
        //     oldms = mountainScale;
        //     oldss = stonesScale;
        //     oldds = detailScale;

        //     for (int x = 0; x < worldDimensions.x; x++)
        //     {
        //         for (int y = 0; y < worldDimensions.y; y++)
        //         {
        //             for (int z = 0; z < worldDimensions.z; z++)
        //             {
        //                 //ChunkVoxelData data;
        //                 //chunks.TryGetValue(new Vector3(x, y, z), out data);

        //                 //data.SetRaw(InitChunkData(x, y, z));
        //                 //data.RegenerateAsync();
        //                 //if (!chunkUpdateQueue.Contains(data))
        //                 //{
        //                 //    chunkUpdateQueue.Add(data);
        //                 //    UpdateNextChunk();
        //                 //}
        //             }
        //         }
        //     }
        // }





        int radius = 3;
        int unit = ChunkVoxelData.size;

        Vector3 viewPos = player.transform.position / unit;

        for (int x = 0; x < radius; x++)
        {
            for (int z = 0; z < radius; z++)
            {

                int realX = (int)viewPos.x + x;
                int realZ = (int)viewPos.z + z;

                Vector3 chunkPos = new Vector3(realX, 0, realZ);

                Square chunkSquare = getChunkSquare(new Vector3(chunkPos.x, chunkPos.z));
                Square viewSquare = getChunkSquare(new Vector3(viewPos.x, viewPos.z));

                if (intersects(chunkSquare, viewSquare))
                {
                    ChunkVoxelData chunk;
                    chunks.TryGetValue(chunkPos, out chunk);

                    if (chunk == null)
                    {
                        for (int y = 0; y < worldDimensions.y; y++)
                        {
                            // chunkUpdateQueue.Add(MakeChunkAt(realX, y, realZ));
                        }
                        // UpdateNextChunk();

                    }
                }

            }
        }

        while (functionsQueue.Count > 0)
        {
            Action func = functionsQueue[0];
            functionsQueue.RemoveAt(0);
            func();
        }
    }

    private Square getChunkSquare(Vector3 pos)
    {
        Square square = new Square();
        square.x = (int)pos.x;
        square.y = (int)pos.z;
        square.w = 1;

        return square;
    }

    private bool intersects(Square a, Square b)
    {
        if (a.x >= b.x && a.x <= b.x + 1 && a.y >= b.y && a.y <= b.y + 1)
        {
            return true;
        }
        return false;
    }


    public ChunkVoxelData MakeChunkAt(int x, int y, int z)
    {
        GameObject chunk = Instantiate(chunkPrefab,
            new Vector3(
                x * ChunkVoxelData.size + 0.5f,
                y * ChunkVoxelData.size + 0.5f,
                z * ChunkVoxelData.size + 0.5f
                ), Quaternion.Euler(0, 0, 0), transform);
        chunk.name = "chunk: (" + x + "." + y + "." + z + ")";
        ChunkVoxelData data = chunk.GetComponent<ChunkVoxelData>();
        data.terrain = this;
        data.chunkPos = new Vector3(x, y, z);
        chunks.Add(new Vector3(x, y, z), data);

        //data.SetRaw(InitChunkData(x, y, z));
        InitChunkData(x, y, z, data);
        return data;
    }

    private void RegenerateSyncWrapper(ChunkVoxelData data)
    {
        if (data.isEmpty)
        {
            data.threadFinished = true;
        }
        else
        {
            data.RegenerateSync(true);
        }


        Action toMainThread = () =>
        {
            threadFinished = true;
            chunkUpdateQueue.RemoveAt(0);
            UpdateNextChunk();
        };
        functionsQueue.Add(toMainThread);
    }

    private void UpdateNextChunk()
    {
        if (chunkUpdateQueue.Count >= 1)
        {
            Async(RegenerateSyncWrapper, chunkUpdateQueue[0]);
            //RegenerateSyncWrapper(chunksQueue[0]);
        }
    }

    public ChunkVoxelData GetChunk(int worldX, int worldY, int worldZ)
    {
        int size = ChunkVoxelData.size;

        int chunkPosX = Mathf.FloorToInt((float)worldX / size);
        int chunkPosY = Mathf.FloorToInt((float)worldY / size);
        int chunkPosZ = Mathf.FloorToInt((float)worldZ / size);

        // if (c < 1)
        // {
        //     Debug.Log(new Vector3(worldX, worldY, worldZ));
        //     Debug.Log(new Vector3(chunkPosX, chunkPosY, chunkPosZ));
        //     c++;

        // }

        ChunkVoxelData chunk = null;
        chunks.TryGetValue(new Vector3(chunkPosX, chunkPosY, chunkPosZ), out chunk);

        return chunk;
    }

    public int GetBlockAt(int x, int y, int z)
    {
        int size = ChunkVoxelData.size;
        int chunkX = x % size;
        int chunkY = y % size;
        int chunkZ = z % size;

        // Debug.Log(new Vector3(x, y, z));



        ChunkVoxelData chunk = GetChunk(x, y, z);
        if (chunk == null)
        {
            // Debug.Log("Chunk doesn't exist at: " + new Vector3(x, y, z));
            return 0;
        }
        else
        {
            // Debug.Log("Chunk exists at: " + new Vector3(x, y, z));
        }

        // if (x < 0 || x >= worldDimensions.x * size)
        // {
        //     return 0;
        // }
        // if (y < 0 || y >= worldDimensions.y * size)
        // {
        //     return 0;
        // }
        // if (z < 0 || z >= worldDimensions.z * size)
        // {
        //     return 0;
        // }

        return chunk.GetCell(chunkX, chunkY, chunkZ);
    }

    public void EditWorld(int x, int y, int z, int cubeType)
    {
        int size = ChunkVoxelData.size;
        //get intra chunk coordinates
        int chunkX = x % size;
        int chunkY = y % size;
        int chunkZ = z % size;

        ChunkVoxelData[] adjacentChunks = new ChunkVoxelData[3];

        ChunkVoxelData chunk = GetChunk(x, y, z);
        bool needsGlobalChange = false;

        if (chunkX == size - 1)
        {
            adjacentChunks[0] = GetChunk(x + 1, y, z);
            needsGlobalChange = true;
        }
        if (chunkX == 0)
        {
            adjacentChunks[0] = GetChunk(x - 1, y, z);
            needsGlobalChange = true;
        }

        if (chunkY == size - 1)
        {
            adjacentChunks[1] = GetChunk(x, y + 1, z);
            needsGlobalChange = true;

        }
        if (chunkY == 0)
        {
            adjacentChunks[1] = GetChunk(x, y - 1, z);
            needsGlobalChange = true;

        }

        if (chunkZ == size - 1)
        {
            adjacentChunks[2] = GetChunk(x, y, z + 1);
            needsGlobalChange = true;

        }
        if (chunkZ == 0)
        {
            adjacentChunks[2] = GetChunk(x, y, z - 1);
            needsGlobalChange = true;

        }


        if (chunk)
        {
            //Debug.Log("chunk found " + x / size + " - " + y / size + " - " + z / size);
            if (chunk.threadFinished)
            {
                chunk.SetCell(chunkX, chunkY, chunkZ, cubeType);
                //chunk.RegenerateSync();

                for (int i = 0; i < 3; i++)
                {
                    if (adjacentChunks[i] != null)
                    {
                        if (adjacentChunks[i].threadFinished && !adjacentChunks[i].isEmpty)
                        {
                            adjacentChunks[i].RegenerateAsync(true);
                            //adjacentChunks[i].RegenerateSync(true);
                        }
                    }
                }
                chunk.RegenerateAsync(true);
                //chunk.RegenerateSync(true);
            }
        }
        else
        {
            Debug.LogError("Chunk not found: " + x + " - " + y + " - " + z);
        }
    }



    int cubeTypes = 5;

    public int[] InitChunkData(int chunkPosX, int chunkPosY, int chunkPosZ, ChunkVoxelData data)
    {
        int size = ChunkVoxelData.size;
        int[] raw = new int[size * size * size];
        int floor = chunkPosY * size;

        bool isEmpty = true;

        if (chunkPosY * size > maxTerrainHeight)
        {
            data.SetRaw(raw, isEmpty);
            return raw;
        }

        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                int yVal = GetNoiseValue(
                    chunkPosZ * size + z,
                    chunkPosX * size + x
                    );
                int cubeType = 1;

                if (yVal / size >= chunkPosY)
                {
                    isEmpty = false;
                }

                for (int y = floor; y < yVal; y++)
                {
                    raw[x + size * (y % size + size * z)] = cubeType;
                }
            }
        }
        data.SetRaw(raw, isEmpty);
        return raw;
    }
    public void Async(Action<ChunkVoxelData> func, ChunkVoxelData data)
    {
        Thread thread = new Thread(() => func(data));
        thread.Start();
        threadFinished = false;
    }
    int GetNoiseValue(float x, float y)
    {
        float mountains = Mathf.PerlinNoise(x * mountainScale, y * mountainScale);
        float stones = 0.5f * Mathf.PerlinNoise(x * stonesScale, y * stonesScale) * mountains;
        float detail = 0.25f * Mathf.PerlinNoise(x * detailScale, y * detailScale) * (stones + mountains);

        float e = Mathf.Clamp(mountains + stones + detail, 0, 1);

        //e = Mathf.Round(e * terraces) / terraces;
        e = Mathf.Pow(e, exponent);

        return Mathf.FloorToInt(e * maxTerrainHeight);
    }

}

public struct Square
{
    public int x;
    public int y;

    public int w;
}