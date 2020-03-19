using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{

    public GameObject playerPrefab;
    public List<Transform> spawnPoints;
    public TerrainGeneratorAsync terrain;

    void Start()
    {
        int randomIndex = Random.Range(0, spawnPoints.Count);
        GameObject playerInstance = Instantiate(playerPrefab, spawnPoints[randomIndex].position, spawnPoints[randomIndex].rotation);
        playerInstance.GetComponent<Build>().terrain = terrain;
        terrain.player = playerInstance.transform;
    }

}
