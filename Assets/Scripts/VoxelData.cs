using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelData
{
    int[,] data = new int[,] {
        { 1, 1, 1 },
        { 1, 0, 0 },
        { 0, 0, 0 }
    };
    public int Width()
    {
        return data.GetLength(0);
    }
    public int Depth()
    {
        return data.GetLength(1);
    }
    public int GetCell(int x, int y, int z)
    {
        return data[x, z];
    }

    public int GetNeighbor(int x, int y, int z, Direction dir)
    {
        DataCoordinate checkOffset = offsets[(int)dir];
        DataCoordinate neighborCoordinate = new DataCoordinate(
            x + checkOffset.x,
            y + checkOffset.y,
            z + checkOffset.z
        );

        if (neighborCoordinate.x < 0 || neighborCoordinate.x >= Width())
        {
            return 0;
        }
        // if (neighborCoordinate.y < 0 || neighborCoordinate.y >= Height())
        // {
        //     return 0;
        // }
        if (neighborCoordinate.z < 0 || neighborCoordinate.z >= Depth())
        {
            return 0;
        }

        return GetCell(neighborCoordinate.x, neighborCoordinate.y, neighborCoordinate.z);
    }

    struct DataCoordinate
    {
        public int x;
        public int y;
        public int z;

        public DataCoordinate(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }
    DataCoordinate[] offsets = {
        new DataCoordinate(0, 0, 1),
        new DataCoordinate(1, 0, 0),
        new DataCoordinate(0, 0, -1),
        new DataCoordinate(-1, 0, 0),
        new DataCoordinate(0, 1, 0),
        new DataCoordinate(0, -1, 0),
    };
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