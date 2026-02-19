using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool instance;
    public MeshGenerator meshGenerator;
    public ProceduralRoad proceduralRoad;
    public GameObject terrainPrefab;
    public GameObject roadPrefab;
    private List<GameObject> pooledTerrain = new List<GameObject>();
    private List<GameObject> pooledRoad = new List<GameObject>();
    void Awake()
    {
        if (instance == null) instance = this;
        PrepareTerrain(meshGenerator.viewDistance);
        PrepareRoads(proceduralRoad.segmentsAhead + proceduralRoad.segmentsBehind + 10);
    }

    void PrepareTerrain(int viewDistance)
    {
        int instantiations = (int)Mathf.Pow((viewDistance * 2), 2) + viewDistance * 4;

        for (int i = 0; i <= instantiations; i++)
        {
            GameObject go = Instantiate(terrainPrefab, transform);
            go.SetActive(false);
            pooledTerrain.Add(go);
        }
    }
    void PrepareRoads(int instantiations)
    {
        for (int i = 0; i <= instantiations; i++)
        {
            GameObject go = Instantiate(roadPrefab, transform);
            go.SetActive(false);
            pooledRoad.Add(go);
        }
    }

    public GameObject GetPooledTerrain()
    {
        for (int i = 0; i < pooledTerrain.Count; i++)
        {
            if (pooledTerrain[i].activeInHierarchy == false)
            {
                pooledTerrain[i].SetActive(true);
                return pooledTerrain[i];
            }
        }
        Debug.LogError("Terrain Pool empty!");
        return Instantiate(terrainPrefab, transform);
    }
    public GameObject GetPooledRoad()
    {
        for (int i = 0; i < pooledRoad.Count; i++)
        {
            if (pooledRoad[i].activeInHierarchy == false)
            {
                return pooledRoad[i];
            }
        }
        Debug.LogError("Road Pool empty!");
        return Instantiate(roadPrefab, transform);;
    }

    public void ReturnRoad(GameObject road)
    {
        road.transform.parent = transform;

        for (int i = road.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(road.transform.GetChild(i).gameObject);
        }
        road.SetActive(false);
    }
}
