using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using UnityEngine.Rendering;
using Unity.VisualScripting;
using System.Collections;
using System;

public class ProceduralRoad : MonoBehaviour
{
    [Header("Player Tracking")]
    public Transform player;

    [Header("Road Settings")]
    public Material roadMaterial;
    public int segmentLength = 10;
    public float roadWidth = 6f;

    [Header("Terrain Settings")]
    public float noiseScale = 600;
    public float heightMultiplier = 1f;

    [Header("Turning Logic")]
    public float turnChance = 0.2f;
    public float maxTurnAngle = 20f;
    public float curveSmoothness = 0.1f;

    [Header("Render Distance")]
    public int segmentsAhead = 25;
    public int segmentsBehind = 5;

    [Header("Guard Rails")]
    public float railHeight = 1.2f;
    public float railThickness = 0.1f;
    public float railVerticalOffset = 0.05f;
    public Material railMaterial;

    [Header("Road Lines")]
    public bool oneWay = false;
    public int lanes = 1;
    public float linesVerticalOffset = 0.05f;
    public float lineThickness = 0.2f;
    public float middleDistance = 0.2f;
    public Material lineMaterial;

    [Header("AI")]
    public AIManager ai;
    public static event Action onGenerate;
    public static ProceduralRoad Instance;

    [Header("Chunking")]
    [SerializeField]
    private int chunkSize;

    //internal state
    private float currentAngle = 0f;
    public List<Vector3> roadPoints = new List<Vector3>();
    private Dictionary<int, GameObject> roadSegments = new Dictionary<int, GameObject>();
    public List<Vector3> pointNormals = new List<Vector3>();
    private NativeArray<float3> jobPointNormals;
    private NativeArray<float3> jobRoadPoints;
    private int closestRoadIndex;
    private Coroutine segmentCoroutine;
    private int roadIndexOffset = 0;
    public int neededPoints;

    //driving backwards is screwed dont try please :pray :pray

    void Awake()
    {
        Instance = this;
    }
    void Update()
    {
        UpdateClosestIndex();
        TrimRoadPoints();

        int localClosest = closestRoadIndex - roadIndexOffset;
        localClosest = Mathf.Clamp(localClosest, 0, roadPoints.Count);

        neededPoints = localClosest + segmentsAhead;

        while (roadPoints.Count < neededPoints + 1)
            AddNextRoadPoint();

        if (segmentCoroutine == null)
            segmentCoroutine = StartCoroutine(SmoothSegmentGeneration());

    }

    IEnumerator SmoothSegmentGeneration()
    {
        yield return null;

        jobRoadPoints = new NativeArray<float3>(roadPoints.Count, Allocator.TempJob);
        jobPointNormals = new NativeArray<float3>(roadPoints.Count, Allocator.TempJob);

        for (int i = 0; i < roadPoints.Count; i++)
            jobRoadPoints[i] = roadPoints[i];

        CalculatePointNormals calculatePoint = new CalculatePointNormals
        {
            nativePointNormals = jobPointNormals,
            nativeRoadPoints = jobRoadPoints,
            direction = new float3(0, 1, 0)
        };
        JobHandle handle = calculatePoint.Schedule(roadPoints.Count, 32);

        pointNormals.Clear();

        handle.Complete();

        for (int i = 0; i < jobPointNormals.Length; i++)
            pointNormals.Add(jobPointNormals[i]);

        jobPointNormals.Dispose();
        jobRoadPoints.Dispose();

        GenerateVisibleSegments();

        segmentCoroutine = null;
    }

    void AddNextRoadPoint()
    {
        Vector3 lastPoint = roadPoints.Count > 0 ? roadPoints[^1] : Vector3.zero;

        //currently the only codde that dictates direction of road, its just simple rng
        if (UnityEngine.Random.value < turnChance)
        {
            float turn = UnityEngine.Random.Range(-maxTurnAngle, maxTurnAngle);
            currentAngle = Mathf.Lerp(currentAngle, currentAngle + turn, curveSmoothness);
        }

        //direction in world space
        Vector3 direction = Quaternion.Euler(0, currentAngle, 0) * Vector3.forward;
        Vector3 nextPoint = lastPoint + direction * segmentLength;

        //uses p noise
        float height = HeightMap.Instance.TerrainHeight(new Vector2(nextPoint.x, nextPoint.z), heightMultiplier, noiseScale);

        nextPoint.y = height;

        roadPoints.Add(nextPoint);
        onGenerate?.Invoke();
    }
    void TrimRoadPoints()
    {
        int minIndex = closestRoadIndex - segmentsBehind - 2;
        int removeCount = minIndex - roadIndexOffset;

        if (removeCount <= 0)
            return;

        removeCount = Mathf.Min(removeCount, roadPoints.Count - 2);

        roadPoints.RemoveRange(0, removeCount);
        pointNormals.RemoveRange(0, removeCount);

        roadIndexOffset += removeCount;
        closestRoadIndex -= removeCount;
    }

    void GenerateVisibleSegments()
    {
        int playerIndex = closestRoadIndex - roadIndexOffset;

        for (int i = playerIndex - segmentsBehind; i < playerIndex + segmentsAhead; i++)
        {
            int worldIndex = i + roadIndexOffset;

            if (i < 0 || i >= roadPoints.Count - 1)
                continue;

            if (!roadSegments.ContainsKey(worldIndex))
            {
                GameObject segment = CreateRoadSegment(roadPoints[i], roadPoints[i + 1], worldIndex);
                roadSegments[worldIndex] = segment;
            }
        }

        //remove segments
        List<int> toRemove = new List<int>();
        foreach (var kvp in roadSegments)
        {
            if (kvp.Key < closestRoadIndex - segmentsBehind)
            {
                kvp.Value.SetActive(false);
                ObjectPool.instance.ReturnRoad(kvp.Value);
                toRemove.Add(kvp.Key);
            }
        }

        //needs to be done in a seperate loop due to coroutine timings. 
        //slower processors that take longer to finsish the coroutine in more than 4 frames may cause problems as the dictionary may be edited whils the coroutine is still running.
        //maybe add profiling to alter timings?

        foreach (int index in toRemove)
        {
            roadSegments.Remove(index);
        }
    }

    GameObject CreateRoadSegment(Vector3 a, Vector3 b, int index)
    {
        Vector3 leftA, rightA;
        int localIndex = index - roadIndexOffset;

        if (index > 0 && roadSegments.ContainsKey(index - 1))
        {
            // Stitch from previous segment
            Mesh prevMesh = roadSegments[index - 1].GetComponent<MeshFilter>().mesh;
            leftA = prevMesh.vertices[2]; 
            rightA = prevMesh.vertices[3];
        }
        else
        {
            //fallback if there is no previous segment. should ONLY be first segment
            Vector3 normalA = pointNormals[localIndex] * (roadWidth / 2f);
            leftA = a - normalA;
            rightA = a + normalA;
        }

        GameObject go = ObjectPool.instance.GetPooledRoad();
        go.transform.parent = transform;
        go.name = $"Road Segment {index}";

        RoadSegment segment = go.GetComponent<RoadSegment>();

        Vector3 normalB = pointNormals[localIndex + 1] * (roadWidth / 2f);
        Vector3 leftB = b - normalB;
        Vector3 rightB = b + normalB;
        Vector3 direction = (b - a).normalized;

        segment.ApplyMesh(leftA, rightA, leftB, rightB, roadMaterial);
        
        GameObject rRail = CreateGuardRail(leftA, leftB, direction, go.transform, "RightRail", true, index);
        GameObject lRail = CreateGuardRail(rightA, rightB, direction, go.transform, "LeftRail", false, index);
        GameObject rLine = CreateRoadLines(leftA, leftB, direction, go.transform, true, index);
        GameObject lLine = CreateRoadLines(rightA, rightB, direction, go.transform, false, index);

        List<GameObject> combine = new List<GameObject>(chunkSize * 2);

        combine.Add(rRail);
        combine.Add(lRail);

        //combine individual meshed into one per type per road segment. reduces batches not too expensive i think
        //CHUNKING NON-FUNCTIONAL batches with more than one group do not work

        if (combine.Count > chunkSize * 2)
        {
            GameObject rail = MeshCombineUtility.CombineMeshes(combine, "Rails", railMaterial, go.transform);
            rail.AddComponent<MeshCollider>();
        }

        combine.Clear();

        combine.Add(rLine);
        combine.Add(lLine);

        if (combine.Count > chunkSize * 2)
        {
            MeshCombineUtility.CombineMeshes(combine, "Lines", lineMaterial, go.transform);
        }
        
        go.SetActive(true);

        return go;

    }

    GameObject CreateGuardRail(Vector3 startA, Vector3 endA, Vector3 direction, Transform parent, string name, bool isRight, int index)
    {
        Vector3 up = Vector3.up * railHeight;
        Vector3 offset = Vector3.up * railVerticalOffset;
        Vector3 sideDir = Vector3.Cross(Vector3.up, direction).normalized;
        Vector3 thickness = sideDir * (isRight ? railThickness : -railThickness);

        Vector3 sideBottomStart;
        Vector3 sideBottomEnd;

        if (index > 0 && roadSegments.ContainsKey(index - 1))
        {
            //stitching
            GameObject lastRoad = roadSegments[index - 1];
            Mesh prevMesh;
            if (isRight)
            {
                Transform lastRail = lastRoad.transform.Find("RightRail");
                if (lastRail == null)
                    return null;
                prevMesh = lastRail.GetComponent<MeshFilter>().mesh;
                Destroy(lastRail.GameObject());
            }
            else
            {
                Transform lastRail = lastRoad.transform.Find("LeftRail");
                if (lastRail == null)
                    return null;
                prevMesh = lastRail.GetComponent<MeshFilter>().mesh;
                Destroy(lastRail.GameObject());
            }
            sideBottomStart = prevMesh.vertices[5];
        }
        else
        {
            //fallback
            sideBottomStart = startA + offset + thickness;
        }
        sideBottomEnd = endA + offset + thickness;
        Vector3 bottomStart = startA + offset;
        Vector3 bottomEnd = endA + offset;
        Vector3 topStart = bottomStart + up;
        Vector3 topEnd = bottomEnd + up;
        Vector3 sideTopStart = sideBottomStart + up;
        Vector3 sideTopEnd = sideBottomEnd + up;


        Mesh mesh = new Mesh();
        mesh.name = $"{name}_Mesh";


        //tris are winded manually here bc i made this before the AddQuad method :/

        mesh.vertices = new Vector3[]
        {
            bottomStart, bottomEnd, topStart, topEnd, sideBottomStart, sideBottomEnd, sideTopStart, sideTopEnd
        };
        if (isRight)
        {
            mesh.triangles = new int[]
            {
                2, 0, 1,
                3, 2, 1,
                3, 7, 2,
                2, 7, 6,
                6, 5, 4,
                7, 5, 6
            };
        }
        else
        {
            mesh.triangles = new int[]
            {
                1, 0, 2,
                1, 2, 3,
                2, 7, 3,
                6, 7, 2,
                4, 5, 6,
                6, 5, 7
            };
        }

        mesh.RecalculateNormals();

        GameObject rail = new GameObject(name);
        rail.transform.parent = parent;

        var mf = rail.AddComponent<MeshFilter>();
        var mr = rail.AddComponent<MeshRenderer>();
        var mc = rail.AddComponent<MeshCollider>();

        mf.mesh = mesh;
        mc.sharedMesh = mesh;
        mr.shadowCastingMode = ShadowCastingMode.Off;

        rail.isStatic = true;
        return rail;
    }

    GameObject CreateRoadLines(Vector3 startA, Vector3 endA, Vector3 direction, Transform parent, bool isRight, int index)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        int vertexIndex = 0;

        Vector3 offset = Vector3.up * linesVerticalOffset;
        Vector3 sideDir = Vector3.Cross(Vector3.up, direction).normalized;
        Vector3 thickness = sideDir * (isRight ? lineThickness : -lineThickness);
        Vector3 position = sideDir * (isRight ? -roadWidth / 2 + middleDistance : roadWidth / 2 - middleDistance);

        Vector3 sideBottomStart;
        Vector3 bottomStart;

        if (index > 0 && roadSegments.ContainsKey(index - 1))
        {
            //stitxh
            GameObject lastRoad = roadSegments[index - 1];
            Mesh prevMesh;
            if (isRight)
            {
                Transform lastRail = lastRoad.transform.Find("RightCombinedLines");

                prevMesh = lastRail.GetComponent<MeshFilter>().mesh;
                Destroy(lastRail.GameObject());
            }
            else
            {
                Transform lastRail = lastRoad.transform.Find("LeftCombinedLines");
                prevMesh = lastRail.GetComponent<MeshFilter>().mesh;
                Destroy(lastRail.GameObject());
            }

            sideBottomStart = prevMesh.vertices[3];
            bottomStart = prevMesh.vertices[1];
        }
        else
        {
            //fallback
            sideBottomStart = startA + offset + thickness + position;
            bottomStart = startA + offset + position;
        }

        Vector3 sideBottomEnd = endA + offset + thickness + position;
        Vector3 bottomEnd = endA + offset + position;

        AddQuad(bottomStart, bottomEnd, sideBottomStart, sideBottomEnd, isRight, ref vertexIndex, vertices, triangles);

        //more lanes!
        if (lanes > 1)
        {
            for (int i = 0; i < lanes - 1; i++)
            {
                if (index % 2 == 0)
                {
                    float laneOffset = roadWidth / 2f / lanes * (i + 1);
                    position = sideDir * (isRight ? -laneOffset : laneOffset);

                    sideBottomEnd = endA + offset + thickness + position;
                    bottomEnd = endA + offset + position;
                    sideBottomStart = startA + offset + thickness + position;
                    bottomStart = startA + offset + position;

                    AddQuad(bottomStart, bottomEnd, sideBottomStart, sideBottomEnd, isRight, ref vertexIndex, vertices, triangles);
                }
            }
        }

        // Create final mesh
        Mesh mesh = new Mesh();
        mesh.name = "CombinedRoadLines";
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();

        GameObject linesObject = new GameObject(isRight ? "RightCombinedLines" : "LeftCombinedLines");
        linesObject.transform.parent = parent;
        linesObject.isStatic = true;

        var mf = linesObject.AddComponent<MeshFilter>();
        var mr = linesObject.AddComponent<MeshRenderer>();

        mf.mesh = mesh;
        mr.shadowCastingMode = ShadowCastingMode.Off;

        linesObject.isStatic = true;
        return linesObject;
    }

    //tool to wind a quad
    void AddQuad(Vector3 bl, Vector3 br, Vector3 tl, Vector3 tr, bool isRight, ref int index, List<Vector3> verts, List<int> tris)
    {
        verts.Add(bl); // 0
        verts.Add(br); // 1
        verts.Add(tl); // 2
        verts.Add(tr); // 3

        if (isRight)
        {
            tris.Add(index + 0);
            tris.Add(index + 1);
            tris.Add(index + 2);
            tris.Add(index + 3);
            tris.Add(index + 2);
            tris.Add(index + 1);
        }
        else
        {
            tris.Add(index + 0);
            tris.Add(index + 2);
            tris.Add(index + 3);
            tris.Add(index + 1);
            tris.Add(index + 0);
            tris.Add(index + 3);
        }

        index += 4;
    }


    void UpdateClosestIndex()
    {
        int localIndex = closestRoadIndex - roadIndexOffset;
        localIndex = Mathf.Clamp(localIndex, 0, roadPoints.Count - 1);

        while (localIndex + 1 < roadPoints.Count &&
            Vector3.SqrMagnitude(roadPoints[localIndex + 1] - player.position) <
            Vector3.SqrMagnitude(roadPoints[localIndex] - player.position))
        {
            localIndex++;
        }

        closestRoadIndex = localIndex + roadIndexOffset;
    }
    [BurstCompile]

    //i dont remember why i made a job to calculate normals, but its porbably faster and it works soooooo
    private struct CalculatePointNormals : IJobParallelFor
    {
        [ReadOnly] public NativeArray<float3> nativeRoadPoints;
        [ReadOnly] public float3 direction;
        [WriteOnly] public NativeArray<float3> nativePointNormals;

        public void Execute(int index)
        {
            float3 dir;
            if (index == 0)
                dir = math.normalize(nativeRoadPoints[index + 1] - nativeRoadPoints[index]);
            else if (index == nativeRoadPoints.Length - 1)
                dir = math.normalize(nativeRoadPoints[index] - nativeRoadPoints[index - 1]);
            else
            {
                float3 forward = math.normalize(nativeRoadPoints[index + 1] - nativeRoadPoints[index]);
                float3 back = math.normalize(nativeRoadPoints[index] - nativeRoadPoints[index - 1]);
                dir = math.normalize((forward + back) * 0.5f);
            }

            float3 normal = math.cross(dir, direction);
            nativePointNormals[index] = normal;
        }
    }
    /*
    private NativeArray<float3> EnsureArray(NativeArray<float3> array, int length)
    {
        if (array.IsCreated)
        {
            if (array.Length != length)
            {
                array.Dispose();
                array = new NativeArray<float3>(length, Allocator.Persistent);
            }
        }
        else
        {   
            array = new NativeArray<float3>(length, Allocator.Persistent);
        }
        return array;
    }
    */
    void OnDestroy()
    {
        if (jobPointNormals.IsCreated)
            jobPointNormals.Dispose();
        if (jobRoadPoints.IsCreated)
            jobRoadPoints.Dispose();
    }
}

//literally just copies all the vertices from a list of meshes into a new one
public static class MeshCombineUtility
{
    public static GameObject CombineMeshes(List<GameObject> parts, string name, Material mat, Transform parent)
    {
        List<CombineInstance> combine = new List<CombineInstance>(parts.Count);

        foreach (var part in parts)
        {
            var mf = part.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            CombineInstance ci = new CombineInstance
            {
                mesh = mf.sharedMesh,
                transform = mf.transform.localToWorldMatrix
            };
            combine.Add(ci);
        }

        if (combine.Count == 0) return null;

        Mesh mesh = new Mesh();
        mesh.CombineMeshes(combine.ToArray(), true, true);

        GameObject combined = new GameObject(name);
        combined.transform.SetParent(parent, false);
        var filter = combined.AddComponent<MeshFilter>();
        var renderer = combined.AddComponent<MeshRenderer>();
        filter.mesh = mesh;
        renderer.material = mat;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        combined.isStatic = true;

        return combined;
    }


}

