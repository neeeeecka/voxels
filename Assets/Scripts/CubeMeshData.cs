using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CubeMeshData
{
    public struct DataCoordinate
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

        public Vector3 ToVector()
        {
            return new Vector3(this.x, this.y, this.z);
        }
    };
    public static DataCoordinate[] offsets = {
        new DataCoordinate(0, 0, 1),
        new DataCoordinate(1, 0, 0),
        new DataCoordinate(0, 0, -1),
        new DataCoordinate(-1, 0, 0),
        new DataCoordinate(0, 1, 0),
        new DataCoordinate(0, -1, 0),
    };
    public static Vector3[] vertices = {
        new Vector3(1,1,1),
        new Vector3(-1,1,1),
        new Vector3(-1,-1,1),
        new Vector3(1,-1,1),
        new Vector3(-1,1,-1),
        new Vector3(1,1,-1),
        new Vector3(1,-1,-1),
        new Vector3(-1,-1,-1),
    };

    public static int[][] faces = {
        new int[]{0,1,2,3}, //North face
        new int[]{5,0,3,6}, //East
        new int[]{4,5,6,7}, //South
        new int[]{1,4,7,2}, //West
        new int[]{5,4,1,0}, //Up
        new int[]{3,2,7,6}  //Down
    };

    public static Vector3[] faceVertices(int dir, Vector3 pos)
    {
        Vector3[] fv = new Vector3[4];
        for (int i = 0; i < 4; i++)
        {
            fv[i] = (vertices[faces[dir][i]]) * 0.5f + pos;
        }
        return fv;
    }
    public static Vector3[] faceVertices(Direction dir, Vector3 pos)
    {
        return faceVertices((int)dir, pos);
    }
    public static Vector2 GetVertexUV(Direction dir, int pos, int cubeType)
    {
        return faceUVs(dir, cubeType)[pos];
    }

    public static Vector2[] faceUVs(Direction dir, int cubeType)
    {
        float yDiffUp = 1 - (float)(cubeType - 1) / 4f;
        float yDiffDown = 3f / 4 - (float)(cubeType - 1) / 4f;

        Vector2[] sideUV = new Vector2[4]{
            new Vector2(0,    yDiffUp),
            new Vector2(1f/4, yDiffUp),
            new Vector2(1f/4, yDiffDown),
            new Vector2(0,    yDiffDown)
        };
        Vector2[] topUV = new Vector2[4]{
            new Vector2(1f/4, yDiffUp),
            new Vector2(2f/4, yDiffUp),
            new Vector2(2f/4, yDiffDown),
            new Vector2(1f/4, yDiffDown)
        };
        Vector2[] bottomUV = new Vector2[4]{
            new Vector2(2f/4, yDiffUp),
            new Vector2(3f/4, yDiffUp),
            new Vector2(3f/4, yDiffDown),
            new Vector2(2f/4, yDiffDown)
        };
        if ((int)dir == 4)
        {
            return topUV;
        }
        if ((int)dir == 5)
        {
            return bottomUV;
        }

        return sideUV;
    }
}