using System;
using UnityEngine;
using UnityEditor;

public class AIManager : MonoBehaviour
{
    [SerializeField] private int amount;
    [SerializeField] private int endBuffer;
    [SerializeField] private int startBuffer;
    [SerializeField] [Tooltip("Chance for every fixed update tick to spawn a car")] private float spawnChance;
    [SerializeField] private float minDist;
    private float curDist;
    [HideInInspector] public GameObject cars;
    public ProceduralRoad road;
    //public int[] weight;
    public static event Action assignValues;

    void Awake()
    {
        curDist = 0;
        endBuffer /= road.segmentLength;
        startBuffer /= road.segmentLength;
    }
    void FixedUpdate()
    {
        curDist -= road.segmentLength;

        if (UnityEngine.Random.value < spawnChance && road.pointNormals.Count > endBuffer + 2 && curDist <= 0)
        {
            bool forward;

            Vector3 point;
            Vector3 normal;

            int pos; 

            float rand = UnityEngine.Random.value;

            //3 in this case is used as a standard y offset

            if (rand > 0.5)
            {
                pos = road.roadPoints.Count - Mathf.RoundToInt(UnityEngine.Random.Range(endBuffer / 10, endBuffer));

                forward = false;

                float inc = road.roadWidth / (road.lanes * 4);
                int lane = UnityEngine.Random.Range(1, road.lanes);

                point = road.roadPoints[pos] - (road.pointNormals[pos] * (inc * (lane * 2) - inc));
                normal = road.pointNormals[pos];

                CreateCar(cars, new Vector3(point.x, point.y + 3, point.z), Quaternion.FromToRotation(Vector3.back, normal), pos, forward, lane);
            }
            else
            {
                if (rand < 0.25)
                    pos = startBuffer;
                else
                    pos = road.roadPoints.Count - Mathf.RoundToInt(UnityEngine.Random.Range(endBuffer / 10, endBuffer));
                
                forward = true;

                float inc = road.roadWidth / (road.lanes * 4);
                int lane = UnityEngine.Random.Range(1, road.lanes);

                point = road.roadPoints[pos] + (road.pointNormals[pos] * (inc * (lane * 2) - inc));
                normal = road.pointNormals[pos];

                CreateCar(cars, new Vector3(point.x, point.y + 3, point.z), Quaternion.FromToRotation(Vector3.forward, normal), pos, forward, lane);
            }

            curDist = minDist;
        }
    }
    public void CreateCar(GameObject car, Vector3 position, Quaternion rotation, int pos = 0, bool forward = false, int lane = 1)
    {
        GameObject go = Instantiate(car, position, rotation);
        go.transform.SetParent(transform, true);
        AICar aICar = go.GetComponent<AICar>();

        aICar.targetIndex = pos;
        aICar.forward = forward;
        aICar.lane = lane;

        Debug.Log("Instantiating Car...");

        assignValues?.Invoke();
    }
}

#if UNITY_EDITOR

[CustomEditor(typeof(AIManager))]
public class AIEditor : Editor
{
    private SerializedProperty _car;
    private SerializedProperty _amount;

    private void OnEnable()
    {
        _amount = serializedObject.FindProperty("amount");
        _car = serializedObject.FindProperty("cars");
    }
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        AIManager aIManager = target as AIManager;
        int amount = _amount.intValue;
        GameObject car = _car.objectReferenceValue as GameObject;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(_car);

        if (GUILayout.Button("Instantiate Car", GUILayout.Width(180f)))
        {
            for (int i = 0; i < amount; i++)
                aIManager.CreateCar(car, new Vector3(0, 0, i * 3), Quaternion.identity);
            
        }

        EditorGUILayout.EndHorizontal();
        
        serializedObject.ApplyModifiedProperties();
    }
}

#endif
