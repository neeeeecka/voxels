using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Build : MonoBehaviour
{
    public TerrainGeneratorAsync terrain;
    public Controller controller;
    public LayerMask hitLayer;

    public float hitDistance = 4;

    private float calcHitDistance = 0;
    // Start is called before the first frame update
    void Start()
    {
        calcHitDistance = Mathf.Sqrt(Mathf.Pow(hitDistance, 2) + 4);
    }

    // Update is called once per frame
    void Update()
    {
        Ray ray = new Ray(controller.head.position, controller.head.forward);
        Debug.DrawRay(controller.head.position, controller.head.forward * calcHitDistance, Color.blue);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, calcHitDistance, hitLayer))
        {
            Vector3 hitDir = hit.normal;
            Vector3 worldPos = hit.point;

            worldPos -= hitDir / 2;

            int x = Mathf.FloorToInt(worldPos.x);
            int y = Mathf.FloorToInt(worldPos.y);
            int z = Mathf.FloorToInt(worldPos.z);

            if (Input.GetMouseButton(0))
            {
                terrain.EditWorld(x, y, z, 0);
            }
            if (Input.GetMouseButton(1))
            {
                terrain.EditWorld(x + (int)hitDir.x, y + (int)hitDir.y, z + (int)hitDir.z, 1);
            }
        }
    }
}
