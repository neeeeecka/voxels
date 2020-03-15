using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public class Texture2Darray : MonoBehaviour
{

    public List<Texture2D> textures = new List<Texture2D>();

    // Start is called before the first frame update
    void Start()
    {

        Texture2D[] txtArr = textures.ToArray();
        Texture2DArray array = new Texture2DArray(txtArr[0].width, txtArr[0].height, txtArr.Length, txtArr[0].format, false);
        for (int i = 0; i < txtArr.Length; i++)
        {
            array.SetPixels(txtArr[i].GetPixels(), i);
        }
        array.Apply();
        AssetDatabase.CreateAsset(array, "Assets/Generated/TextureArray.asset");

    }

}
