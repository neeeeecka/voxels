﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Threading;
using Unity.Mathematics;

public class TerrainGeneratorAsync : MonoBehaviour
{
    [Range(0, 1f)]
    public float mountainScale = 0.01f;

    public float mountainPersistence = 1f;

    [Range(0, 1f)]
    public float stonesScale = 0.1f;

    public float stonesPersistence = 1f;


    [Range(0, 1f)]
    public float detailScale = 0.2f;
    public float detailPersistence = 1f;

    public int maxTerrainHeight = 10;
    public int blocksGenerated = 0;

    public Dictionary<Vector2, ChunkVoxelData> chunks = new Dictionary<Vector2, ChunkVoxelData>();

    public bool threadFinished = true;

    public GameObject chunkPrefab;

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
        //chunksQueue.Add(MakeChunkAt(0, 0));
        //MakeNextChunk();
    }

    List<Action> functionsQueue = new List<Action>();
    List<ChunkVoxelData> chunksQueue = new List<ChunkVoxelData>();

    public Transform player;
    

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
        
        GameObject chunk = Instantiate(chunkPrefab, new Vector3(x * ChunkVoxelData.size + 0.5f, 0.5f, z * ChunkVoxelData.size + 0.5f), Quaternion.Euler(0, 0, 0), transform);
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
            chunksQueue.RemoveAt(0);
            MakeNextChunk();
        };
        functionsQueue.Add(toMainThread);
    }

    private void MakeNextChunk()
    {
        if (chunksQueue.Count >= 1)
        {
            Async(RegenerateSyncWrapper, chunksQueue[0]);
            //RegenerateSyncWrapper(chunksQueue[0]);
        }
    }

    public void EditWorld(int x, int y, int z, int cubeType)
    {
        int chunkX = x % ChunkVoxelData.size;
        int chunkZ = z % ChunkVoxelData.size;

        int chunkPosX = x / ChunkVoxelData.size;
        int chunkPosZ = z / ChunkVoxelData.size;

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

    int cubeTypes = 5;

    public int[] InitChunkData(int chunkPosX, int chunkPosZ)
    {
        int size = ChunkVoxelData.size;
        int[] raw = new int[size * size * size];
        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                int yVal = GetNoiseValue(chunkPosZ * ChunkVoxelData.size + z, chunkPosX * ChunkVoxelData.size + x);

                for (int y = 1; y < yVal; y++)
                {
                    int cubeType = cubeTypes - Mathf.FloorToInt((float)yVal / (float)maxTerrainHeight * cubeTypes);
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
        float mountains = Mathf.PerlinNoise(x * mountainScale, y * mountainScale) * mountainPersistence;
        float stones = Mathf.PerlinNoise(x * stonesScale, y * stonesScale) * stonesPersistence;
        float detail = Mathf.PerlinNoise(x * detailScale, y * detailScale) * detailPersistence;

        float sum = (mountains - 0.8f + stones + detail) / 3;

        return Mathf.FloorToInt(Mathf.Clamp(sum, 0, 1) * (maxTerrainHeight-1));
    }

}
