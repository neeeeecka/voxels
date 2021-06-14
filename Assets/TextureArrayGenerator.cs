using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[System.Serializable]
public struct T_Material
{
    public Texture2D albedo;
    public Texture2D normal;
    public Texture2D occulusion;
    public Texture2D rougness;

}

public class TextureArrayGenerator : MonoBehaviour
{
    public List<T_Material> materials;

    public Texture2DArray materialArray;
    public Texture2DArray normalArray;


    // Start is called before the first frame update
    void Awake()
    {
        StartCoroutine(CreateAsset());
    }

    IEnumerator CreateAsset()
    {

        yield return new WaitForSeconds(1f);
        if (materialArray == null || normalArray == null)
        {
            Texture2D sample = materials[0].albedo;
            materialArray = new Texture2DArray(sample.width, sample.height, materials.Count * 4, TextureFormat.RGBA32, true, false); //SRGB TRUE(its reverse ikr)
            normalArray = new Texture2DArray(sample.width, sample.height, materials.Count, TextureFormat.RGBA32, true, true); //SRGB FALSE

            for (int i = 0; i < materials.Count * 4; i += 4)
            {
                T_Material material = materials[i / 4];

                materialArray.SetPixels(material.albedo.GetPixels(), i);
                materialArray.SetPixels(material.normal.GetPixels(), i + 1);
                materialArray.SetPixels(material.occulusion.GetPixels(), i + 2);
                materialArray.SetPixels(material.rougness.GetPixels(), i + 3);

                normalArray.SetPixels(material.normal.GetPixels(), i / 4);
            }
            normalArray.Apply(true);
            materialArray.Apply(true);

            AssetDatabase.CreateAsset(materialArray, "Assets/_Generated/" + "terrainTextures.asset");
            AssetDatabase.CreateAsset(normalArray, "Assets/_Generated/" + "terrainNormals.asset");

            AssetDatabase.SaveAssets();
        }
    }

    void Update()
    {

    }

}
