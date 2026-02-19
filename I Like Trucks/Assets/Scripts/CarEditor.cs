#if (UNITY_EDITOR) 
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CarManager))]
public class CarEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Sync"))
        {
            CarManager.SyncTires();
        }
    }
}
#endif
