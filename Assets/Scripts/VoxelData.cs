using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelData
{
    // int[,] data = new int[,] {
    //     { 1, 1, 1 },
    //     { 1, 0, 1 },
    //     { 1, 1, 1 }
    // };

    //int[][][] data = new int[][][] {
    //       new int[32]
    //};
    public int[] raw = new int[32 * 32 * 32];
    //int[,,] data = new int[,,] {
    //    {{ 1, 1, 1, 1, 1, 1 }},
    //    {{ 1, 1, 0, 0, 1, 1 }},
    //    {{ 1, 1, 0, 0, 1, 1 }},
    //    {{ 1, 1, 1, 1, 1, 1 }},
    //    {{ 1, 1, 1, 1, 1, 1 }}
    //};

    public VoxelData(int x, int y, int z)
    {
        //this.data = new int[x, y, z];
    }

    public int Width()
    {
        //return data.GetLength(0);
        return 32;
    }
    public int Height()
    {
        //return data.GetLength(1);
        return 32;

    }
    public int Depth()
    {
        //return data.GetLength(2);
        return 32;

    }
    public int GetCell(int x, int y, int z)
    {
        //return data[x, y, z];
        return raw[x + 32 * (y + 32 * z)];
    }

    public void SetCell(int x, int y, int z, int val)
    {
        //data[x, y, z] = val;
        raw[x + 32 * (y + 32 * z)] = val;

        
    }
    public int GetNeighbor(int x, int y, int z, Direction dir)
    {
        CubeMeshData.DataCoordinate checkOffset = CubeMeshData.offsets[(int)dir];
        CubeMeshData.DataCoordinate neighborCoordinate = new CubeMeshData.DataCoordinate(
            x + checkOffset.x,
            y + checkOffset.y,
            z + checkOffset.z
        );

        if (neighborCoordinate.x < 0 || neighborCoordinate.x >= Width())
        {
            return 0;
        }
        if (neighborCoordinate.y < 0 || neighborCoordinate.y >= Height())
        {
            return 0;
        }
        // if (neighborCoordinate.y != 0)
        // {
        //     return 0;
        // }
        if (neighborCoordinate.z < 0 || neighborCoordinate.z >= Depth())
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