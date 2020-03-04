using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CubeMeshData
{
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
    public static Vector2[] faceUVs(Direction dir)
    {

        Vector2[] sideUV = new Vector2[4]{
            new Vector2(0, 1),
            new Vector2(1f/3, 1),
            new Vector2(1f/3, 0),
            new Vector2(0, 0)
        };
        Vector2[] topUV = new Vector2[4]{
            new Vector2(1f/3, 0),
            new Vector2(1f/3, 1),
            new Vector2(2f/3, 1),
            new Vector2(2f/3, 0)
        };
        Vector2[] bottomUV = new Vector2[4]{
            new Vector2(2f/3, 0),
            new Vector2(2f/3, 1),
            new Vector2(1, 1),
            new Vector2(1, 0)
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