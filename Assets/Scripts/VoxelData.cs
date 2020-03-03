using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelData
{
    int[,] data = new int[,] {
        { 1, 0, 0 },
        { 0, 0, 0 },
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
    public int GetCell(int x, int y)
    {
        return data[x, y];
    }
}
