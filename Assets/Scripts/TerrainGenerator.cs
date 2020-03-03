using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Face
{
    List<Vector3> vertices;
    List<int> triangles;
    List<Vector3> normals;
    List<Vector2> uv;
    public Face(
        Vector3 direction,
        int startAt,
        Vector3[] verArr,
        int[] triArr,
        Vector3[] normArr,
        Vector2[] uvArr
        )
    {
        vertices = new List<Vector3>(verArr);
        triangles = new List<int>(triArr);
        normals = new List<Vector3>(normArr);
        uv = new List<Vector2>(uvArr);

        // Vector3 startVert = vertices[startVertIndex];

        int lastVertIndex = vertices.Count - 1;
        if (lastVertIndex <= 0)
        {
            lastVertIndex = 0;
        }

        //last + 0
        vertices.Add(new Vector3(0, 0, 0));

        //last +1
        vertices.Add(new Vector3(1, 0, 0));

        //last +2
        vertices.Add(new Vector3(0, 1, 0));

        //last +3
        vertices.Add(new Vector3(1, 1, 0));

        //first triangle
        triangles.Add(lastVertIndex);
        triangles.Add(lastVertIndex + 2);
        triangles.Add(lastVertIndex + 1);

        //second triangle
        triangles.Add(lastVertIndex + 2);
        triangles.Add(lastVertIndex + 3);
        triangles.Add(lastVertIndex + 1);


        //add normals
        for (int i = 0; i < vertices.Count; i++)
        {
            normals.Add(-Vector3.forward);
        }
        //add UVs
        uv.Add(new Vector2(0, 0));
        uv.Add(new Vector2(1, 0));
        uv.Add(new Vector2(0, 1));
        uv.Add(new Vector2(1, 1));
    }

    public Vector3[] GetVertices()
    {
        return vertices.ToArray();
    }
    public int[] GetTriangles()
    {
        return triangles.ToArray();
    }

    public Vector3[] GetNormals()
    {
        return normals.ToArray();
    }
    public Vector2[] GetUVs()
    {
        return uv.ToArray();
    }

}

public class TerrainGenerator : MonoBehaviour
{

    void Start()
    {
        Mesh mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        Face face = new Face(
            new Vector3(0, 0, 0),
            0,
            new Vector3[] { },
            new int[] { },
            new Vector3[] { },
            new Vector2[] { }
             );

        mesh.vertices = face.GetVertices();
        mesh.triangles = face.GetTriangles();
        mesh.normals = face.GetNormals();
        mesh.uv = face.GetUVs();

    }
}
