using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        GUI.enabled = false;  // Disable the GUI (make it read-only)
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = true;   // Re-enable the GUI
    }
}

