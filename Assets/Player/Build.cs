using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Build : MonoBehaviour
{
    public TerrainGeneratorAsync terrain;
    public Controller controller;
    public LayerMask hitLayer;

    public Transform eyes;

    public float hitDistance = 4;

    private float calcHitDistance = 0;

    public float blocksPerSecond = 3;

    public bool canPut = false;

    public float minBuildDist = 1;

    void Start()
    {
        calcHitDistance = Mathf.Sqrt(Mathf.Pow(hitDistance, 2) + 4);
    }

    IEnumerator bpsTimer()
    {
        canPut = false;
        yield return new WaitForSeconds(1 / blocksPerSecond);
        canPut = true;
    }

    Vector3 half = new Vector3(0.5f, 0.5f, 0.5f);

    // Update is called once per frame
    void Update()
    {
        if (canPut)
        {
            Ray ray = new Ray(eyes.position, eyes.forward);
            Debug.DrawRay(eyes.position, eyes.forward * calcHitDistance, Color.blue);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, calcHitDistance, hitLayer))
            {
                Vector3 hitDir = hit.normal;
                Vector3 worldPos = hit.point;

                worldPos -= hitDir / 2;

                int x = Mathf.FloorToInt(worldPos.x);
                int y = Mathf.FloorToInt(worldPos.y);
                int z = Mathf.FloorToInt(worldPos.z);

                Vector3 floored = new Vector3(x, y, z);
                
                if (Input.GetMouseButton(0))
                {
                    terrain.EditWorld(x, y, z, 0);
                    StartCoroutine(bpsTimer());
                }
                if ((transform.position - half - (floored + hitDir)).magnitude >= minBuildDist)
                {
                    if (Input.GetMouseButton(1))
                    {
                        terrain.EditWorld(x + (int)hitDir.x, y + (int)hitDir.y, z + (int)hitDir.z, 1);
                        StartCoroutine(bpsTimer());

                    }
                }

            }
        }
    }
}