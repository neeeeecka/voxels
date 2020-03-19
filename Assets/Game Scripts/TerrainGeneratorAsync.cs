using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Threading;
using Unity.Mathematics;

public class TerrainGeneratorAsync : MonoBehaviour
{

    [Range(0, 0.1f)]
    public float scale = 0.01f;
    [Range(1, 32)]

    public int maxTerrainHeight = 10;
    public int blocksGenerated = 0;

    public Dictionary<Vector2, ChunkVoxelData> chunks = new Dictionary<Vector2, ChunkVoxelData>();
    //public ChunkVoxelData data;

    public bool threadFinished = true;

    public GameObject chunkPrefab;


    IEnumerator chunkGen()
    {
        for (int x = 0; x < 10; x++)
        {
            for (int z = 0; z < 10; z++)
            {
                yield return new WaitForSeconds(0.1f);
                MakeChunkAt(x, z);
            }
        }

    }

    void Start()
    {
        for (int x = 0; x < 10; x++)
        {
            for (int z = 0; z < 10; z++)
            {
                chunksQueue.Add(MakeChunkAt(x, z));
            }
        }
        MakeNextChunk();
    }

    List<Action> functionsQueue = new List<Action>();
    List<ChunkVoxelData> chunksQueue = new List<ChunkVoxelData>();


    private void Update()
    {
        while (functionsQueue.Count > 0)
        {
            Action func = functionsQueue[0];
            functionsQueue.RemoveAt(0);
            func();
        }
    }

    public ChunkVoxelData MakeChunkAt(int x, int z)
    {
        
        GameObject chunk = Instantiate(chunkPrefab, new Vector3(x * 32 + 0.5f, 0.5f, z * 32 + 0.5f), Quaternion.Euler(0, 0, 0), transform);
        chunk.name = "c-" + x + "." + z;
        ChunkVoxelData data = chunk.GetComponent<ChunkVoxelData>();
        chunks.Add(new Vector2(x, z), data);

        data.SetRaw(InitChunkData(x, z));
        return data;
    }

    private void RegenerateSyncWrapper(ChunkVoxelData data)
    {
        data.RegenerateSync();
        Action toMainThread = () =>
        {
            threadFinished = true;
            MakeNextChunk();
        };
        functionsQueue.Add(toMainThread);
    }

    private void MakeNextChunk()
    {
        chunksQueue.RemoveAt(0);
        Async(RegenerateSyncWrapper, chunksQueue[0]);
    }

    public void EditWorld(int x, int y, int z, int cubeType)
    {
        int chunkX = x % 32;
        int chunkZ = z % 32;

        int chunkPosX = x / 32;
        int chunkPosZ = z / 32;

        //Debug.Log("raw coord: " + new Vector2(x, z));

        //Debug.Log("chunk coord: " + new Vector2(chunkX, chunkZ));

        ChunkVoxelData data = null;
        chunks.TryGetValue(new Vector2(chunkPosX, chunkPosZ), out data);
        if (data)
        {
            if (data.threadFinished)
            {
                data.SetCell(chunkX, y, chunkZ, cubeType);
                data.RegenerateAsync();
            }
 
        }
        else
        {
            Debug.LogError("Chunk not found");
        }
    }

    public int[] InitChunkData(int chunkPosX, int chunkPosZ)
    {
        int size = 32;
        int[] raw = new int[size * size * size];
        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                int yVal = GetNoiseValue(chunkPosZ * 32 + z, chunkPosX * 32 + x);

                for (int y = yVal - 1; y >= 0; y--)
                {
                    int cubeType = 4 - Mathf.FloorToInt((float)yVal / (float)maxTerrainHeight * 4);
                    raw[x + size * (y + size * z)] = cubeType;
                }
            }
        }
        return raw;

    }
    public void Async(Action<ChunkVoxelData> func, ChunkVoxelData data) {
        Thread thread = new Thread(() => func(data));
        thread.Start();
        threadFinished = false;
    }
    int GetNoiseValue(float x, float y)
    {
        return Mathf.FloorToInt(Mathf.PerlinNoise(x * scale, y * scale) * maxTerrainHeight);
    }

}
