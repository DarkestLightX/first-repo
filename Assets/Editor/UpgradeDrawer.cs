using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(UpgradeManager.UpgradeDictionary), true)]
public class UpgradeDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty keys = property.FindPropertyRelative("keys");
        SerializedProperty values = property.FindPropertyRelative("values");

        EditorGUI.BeginProperty(position, label, property);

        position.height = EditorGUIUtility.singleLineHeight;
        property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label);

        if (property.isExpanded)
        {
            float half = position.width / 2f;
            EditorGUI.indentLevel++;

            for (int i = 0; i < keys.arraySize; i++)
            {
                position.y += EditorGUIUtility.singleLineHeight + 2;

                Rect keyRect = new Rect(position.x, position.y, half - 5, position.height);
                Rect valueRect = new Rect(position.x + half, position.y, half, position.height);

                EditorGUI.PropertyField(keyRect, keys.GetArrayElementAtIndex(i), GUIContent.none);
                EditorGUI.PropertyField(valueRect, values.GetArrayElementAtIndex(i), GUIContent.none);
            }

            position.y += EditorGUIUtility.singleLineHeight + 4;

            Rect addRect = new Rect(position.x, position.y, half - 5, position.height);
            Rect removeRect = new Rect(position.x + half, position.y, half, position.height);

            if (GUI.Button(addRect, "Add Upgrade"))
            {
                keys.arraySize++;
                values.arraySize++;
                Debug.Log(values.arraySize);
            }

            if (GUI.Button(removeRect, "Remove Upgrade"))
            {
                keys.arraySize--;
                values.arraySize--;
                Debug.Log(values.arraySize);
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;

        SerializedProperty keys = property.FindPropertyRelative("keys");
        int lines = keys.arraySize + 2;

        return lines * (EditorGUIUtility.singleLineHeight + 2);
    }
}
