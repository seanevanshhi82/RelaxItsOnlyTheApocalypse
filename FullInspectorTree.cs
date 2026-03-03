using UnityEngine;
using UnityEditor;
using System.Reflection;
using System.Collections.Generic;

public class FullInspectorTree : EditorWindow
{
    private Object selected;
    private Vector2 scroll;

    [MenuItem("Tools/Full Inspector Tree (Selected Object)")]
    static void Init()
    {
        GetWindow<FullInspectorTree>("Full Inspector Tree");
    }

    void OnInspectorUpdate()
    {
        Object newSelected = Selection.activeObject;
        if (newSelected != selected)
        {
            selected = newSelected;
            Repaint();
        }
    }

    void OnGUI()
    {
        if (selected == null)
        {
            GUILayout.Label("Select any GameObject or asset in Project/Hierarchy first.");
            return;
        }

        GUILayout.Label($"Inspecting: {selected.name} ({selected.GetType().Name})", EditorStyles.boldLabel);
        scroll = EditorGUILayout.BeginScrollView(scroll);

        DrawObjectFields(selected);

        EditorGUILayout.EndScrollView();
    }

    private void DrawObjectFields(object obj, int depth = 0)
    {
        if (obj == null) return;

        System.Type type = obj.GetType();
        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        foreach (FieldInfo field in type.GetFields(flags))
        {
            object value = field.GetValue(obj);
            string label = new string(' ', depth * 2) + field.Name + " = ";

            if (value == null)
            {
                GUILayout.Label(label + "null");
            }
            else if (value is Object unityObj)
            {
                EditorGUILayout.ObjectField(label, unityObj, typeof(Object), true);
            }
            else if (value.GetType().IsPrimitive || value is string)
            {
                GUILayout.Label(label + value);
            }
            else
            {
                GUILayout.Label(label + value.GetType().Name);
                EditorGUI.indentLevel++;
                DrawObjectFields(value, depth + 1);
                EditorGUI.indentLevel--;
            }
        }
    }
}


