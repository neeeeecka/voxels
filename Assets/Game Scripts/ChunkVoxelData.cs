using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkVoxelData : MonoBehaviour
{

    public int[] raw = new int[1 * 1 * 1];
    public int size = 0;

    public ChunkVoxelData(int size)
    {
        this.size = size;
        raw = new int[size * size * size];
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