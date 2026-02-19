using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UpgradeManager))]
public class UpgradeEditor : Editor
{
    private int _index;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        UpgradeManager upgradeManager = target as UpgradeManager;

        EditorGUILayout.BeginHorizontal();
        _index = EditorGUILayout.IntField(_index);

        if (GUILayout.Button("Apply Upgrade", GUILayout.Width(180f)))
        {
            upgradeManager.ChooseUpgrade(_index);
        }

        EditorGUILayout.EndHorizontal();
    }
}