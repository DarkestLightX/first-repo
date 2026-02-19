using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

public class HeightMap : MonoBehaviour
{
    public static HeightMap Instance;

    [Header("Settings for Terrain Generation")]
    public int octaves = 4;
    public float lacunarity = 2;
    public float persistence = 0.5f;

    public float TerrainHeight(Vector2 xz, float heightMultiplier, float noiseScale)
    {
        float height = 0f;
        for (int o = 0; o < octaves; o++)
        {
            float frequency = math.pow(lacunarity, o);
            float amplitude = math.pow(persistence, o);
            float2 sample = xz / (noiseScale * frequency);
            float noiseVal = noise.cnoise(sample);
            height += noiseVal * amplitude;
        }
        return height * heightMultiplier;
}


    public NativeArray<float> ChunkTerrainHeight(float heightMultiplier, float noiseScale, int resolution, Vector2 chunkOrigin)
    {
        NativeArray<float> jobHeights = new NativeArray<float>(resolution * resolution, Allocator.TempJob);

        GetTerrainHeight job = new GetTerrainHeight
        {
            lacunarity = lacunarity,
            persistence = persistence,
            scale = noiseScale,
            multiplier = heightMultiplier,
            octaves = octaves,
            resolution = resolution,
            chunkOrigin = chunkOrigin,
            heights = jobHeights
        };

        JobHandle handle = job.Schedule(resolution * resolution, 64);
        handle.Complete();

        return jobHeights;
    }

    [BurstCompile]
    public struct GetTerrainHeight : IJobParallelFor
    {
        [ReadOnly] public float persistence;
        [ReadOnly] public float lacunarity;
        [ReadOnly] public float2 scale;
        [ReadOnly] public float multiplier;
        [ReadOnly] public int octaves;
        [ReadOnly] public int resolution;
        [ReadOnly] public float2 chunkOrigin;
        [WriteOnly] public NativeArray<float> heights;

        public void Execute(int index)
        {
            int x = index % resolution;
            int y = index / resolution;

            float2 worldPos = chunkOrigin + new float2(x, y);

            float height = 0f;
            for (int o = 0; o < octaves; o++)
            {
                float frequency = math.pow(lacunarity, o);
                float amplitude = math.pow(persistence, o);
                float2 sample = worldPos / (scale * frequency);
                float noiseVal = noise.cnoise(sample);
                height += noiseVal * amplitude;
            }

            heights[index] = height * multiplier;
        }
    }

    void Awake()
    {
        Instance = this;
    }
}
