using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Concurrent;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.Diagnostics;

public class MeshGenerator : MonoBehaviour
{
    public ProceduralRoad proceduralRoad;
    public Transform player;
    public int viewDistance = 3;
    public int chunkSize = 16;

    [Header("Generation")]
    public float noiseScale = 20f;
    public float heightMultiplier = 5f;
    public float maxHeight = 10;
    public float minHeight = -10;
    public float flattenCoefficient;
    public Material chunkMaterial;

    private Dictionary<Vector2Int, GameObject> activeChunks;
    private HashSet<Vector2Int> chunksBeingGenerated;
    private bool generating;
    private Queue<Vector2Int> chunksToGenerate = new Queue<Vector2Int>();
    private HashSet<Vector2Int> updatedChunks;
    private List<Vector2Int> toRemove;
    private List<Vector3> roadPoints => proceduralRoad.roadPoints;
    static readonly List<Vector3> roadPointsBuffer = new List<Vector3>();
    private HashSet<Vector2Int> queuedChunks;
    private bool isQuiting;
    private List<Vector2Int> candidateChunks = new List<Vector2Int>();
    private Stopwatch stopwatch= new Stopwatch();
    [SerializeField] private float targetFPS = 120f;
    [SerializeField] private float budget;


    void Awake()
    {
        Application.targetFrameRate = Mathf.RoundToInt(targetFPS);

        //0.5 means half of frame time will be allocated to terrain generation
        budget = 0.5f / targetFPS;

        int maxVisibleChunks = (viewDistance * 2 + 1);
        maxVisibleChunks *= maxVisibleChunks;

        activeChunks = new Dictionary<Vector2Int, GameObject>(maxVisibleChunks);
        updatedChunks = new HashSet<Vector2Int>(maxVisibleChunks);
        chunksBeingGenerated = new HashSet<Vector2Int>(maxVisibleChunks);
        queuedChunks = new HashSet<Vector2Int>(maxVisibleChunks);
        toRemove = new List<Vector2Int>(maxVisibleChunks);
    }

    void Update()
    {
        generating = true;

        stopwatch.Restart();

        if (isQuiting)
            return;

        //continues generating terrain unitl there is no more or budget esceeded
        while (stopwatch.Elapsed.TotalSeconds < budget && generating)
            GenerateTerrain();
        
        stopwatch.Stop();
    }

    void GenerateTerrain()
    {
        Vector2Int playerChunk = new Vector2Int(Mathf.FloorToInt(player.position.x / chunkSize), Mathf.FloorToInt(player.position.z / chunkSize));

        candidateChunks.Clear();

        for (int x = -viewDistance; x <= viewDistance; x++)
        {
            for (int z = -viewDistance; z <= viewDistance; z++)
            {
                Vector2Int offset = new Vector2Int(x, z);
                if (offset.sqrMagnitude > viewDistance * viewDistance)
                    continue;

                Vector2Int coord = new Vector2Int(playerChunk.x + x, playerChunk.y + z);
                updatedChunks.Add(coord);

                if (!activeChunks.ContainsKey(coord) && !chunksBeingGenerated.Contains(coord) && !queuedChunks.Contains(coord))
                {
                    candidateChunks.Add(coord);
                }
            }
        }

        candidateChunks.Sort((a, b) =>
        {
            float da = (a - playerChunk).sqrMagnitude;
            float db = (b - playerChunk).sqrMagnitude;
            return da.CompareTo(db);
        });

        foreach (var coord in candidateChunks)
        {
            chunksBeingGenerated.Add(coord);
            queuedChunks.Add(coord);
            chunksToGenerate.Enqueue(coord);
        }


        foreach (var kvp in activeChunks)
        {
            if (!updatedChunks.Contains(kvp.Key))
            {
                kvp.Value.SetActive(false);
                toRemove.Add(kvp.Key);
            }
        }

        foreach (var key in toRemove)
        {
            activeChunks.Remove(key);
        }

        if (chunksToGenerate.Count > 0)
        {
            Vector2Int coord = chunksToGenerate.Dequeue();
            queuedChunks.Remove(coord);

            roadPointsBuffer.Clear();
            roadPointsBuffer.AddRange(roadPoints);

            var meshData = GenerateMeshDataJob(coord, roadPointsBuffer);
            CreateChunkObject(coord, meshData);
            chunksBeingGenerated.Remove(coord);
            meshData.Dispose();
        }
        else
        {
            generating = false;
        }

        updatedChunks.Clear();
        toRemove.Clear();
    }

    MeshData GenerateMeshDataJob(Vector2Int coord, List<Vector3> roadPointsBuffer)
    {
        int resolution = chunkSize + 1;
        var meshData = new MeshData(resolution);

        NativeArray<float3> nativeRoadPoints = new NativeArray<float3>(roadPointsBuffer.Count, Allocator.TempJob);
        for (int i = 0; i < roadPointsBuffer.Count; i++)
            nativeRoadPoints[i] = roadPointsBuffer[i];

        Vector2 chunkOrigin = new Vector2(coord.x * chunkSize, coord.y * chunkSize);
        NativeArray<float> heights = HeightMap.Instance.ChunkTerrainHeight(heightMultiplier, noiseScale, resolution, chunkOrigin);

        var terrainJob = new TerrainJob
        {
            chunkSize = chunkSize,
            resolution = resolution,
            railThickness = proceduralRoad.railThickness,
            roadWidth = proceduralRoad.roadWidth,
            chunkOrigin = new float3(chunkOrigin.x, 0, chunkOrigin.y),
            perlinHeights = heights,
            roadPoints = nativeRoadPoints,
            vertices = meshData.vertices.Reinterpret<float3>(),
            maxHeight = maxHeight,
            minHeight = minHeight,
            flattenCoefficient = flattenCoefficient
        };

        JobHandle handle = terrainJob.Schedule(resolution * resolution, 32);


        int triIndex = 0;
        for (int z = 0; z < chunkSize; z++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                int i = z * resolution + x;

                meshData.triangles[triIndex++] = i;
                meshData.triangles[triIndex++] = i + resolution;
                meshData.triangles[triIndex++] = i + 1;

                meshData.triangles[triIndex++] = i + 1;
                meshData.triangles[triIndex++] = i + resolution;
                meshData.triangles[triIndex++] = i + resolution + 1;
            }
        }

        handle.Complete();

        heights.Dispose();
        nativeRoadPoints.Dispose();

        return meshData;
    }

    void CreateChunkObject(Vector2Int coord, MeshData data)
    {
        GameObject chunk = ObjectPool.instance.GetPooledTerrain();
        chunk.transform.name = $"Chunk {coord.x} {coord.y}";
        chunk.transform.position = new Vector3(coord.x * chunkSize, 0, coord.y * chunkSize);

        MeshRenderer mr = chunk.GetComponent<MeshRenderer>();
        MeshFilter mf = chunk.GetComponent<MeshFilter>();
        mr.material = chunkMaterial;

        Mesh mesh = new Mesh();

        mesh.SetVertices(data.vertices, 0, data.vertices.Length);
        mesh.triangles = data.triangles;
        mesh.RecalculateNormals();

        mf.mesh = mesh;
        activeChunks[coord] = chunk;
    }

    void OnApplicationQuit()
    {
        chunksToGenerate.Clear();
    }


    [BurstCompile]
    public struct TerrainJob : IJobParallelFor
    {
        [ReadOnly] public int chunkSize;
        [ReadOnly] public int resolution;
        [ReadOnly] public float3 chunkOrigin;
        [ReadOnly] public NativeArray<float> perlinHeights;
        [ReadOnly] public NativeArray<float3> roadPoints;
        [ReadOnly] public float maxHeight;
        [ReadOnly] public float roadWidth;
        [ReadOnly] public float railThickness;
        [ReadOnly] public float flattenCoefficient;
        [ReadOnly] public float minHeight;
        [WriteOnly] public NativeArray<float3> vertices;

        public void Execute(int index)
        {
            int x = index % resolution;
            int z = index / resolution;

            float worldX = chunkOrigin.x + x;
            float worldZ = chunkOrigin.z + z;

            float3 worldPos = new float3(worldX, perlinHeights[index], worldZ);

            float3 closest = GetClosestRoadPoint(worldPos);
            float dist = math.distance(worldPos.xz, closest.xz) - (roadWidth / 2) - railThickness - 1;
            float flattenAmount = math.clamp(1f - dist / flattenCoefficient, 0f, 1f);
            float finalHeight = math.lerp(perlinHeights[index], closest.y, flattenAmount) - 0.5f;

            math.clamp(finalHeight, minHeight, maxHeight);

            vertices[index] = new float3(x, finalHeight, z);
        }

        float3 GetClosestRoadPoint(float3 pos)
        {
            float minDist = float.MaxValue;
            float3 closest = float3.zero;

            //sooo this is really bad programming bc it is a nested loop where every terrain point itterates over every road point
            //since this is multithreaded its not too expensive but will scale very poorly with larger render distances
            //1ms rander distance of 30 14600kf
            //idk a fix at the moment. will likely require a large refactor

            for (int i = 0; i < roadPoints.Length; i++)
            {
                float d = math.distance(pos.xz, roadPoints[i].xz);
                if (d < minDist)
                {
                    minDist = d;
                    closest = roadPoints[i];
                }
            }

            return closest;
        }
    }
}
