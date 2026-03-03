using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ComponentListRemover : EditorWindow
{
    private Vector2 scrollPos;
    private string statusMessage = "";

    [MenuItem("Tools/Component List & Remove")]
    public static void ShowWindow()
    {
        GetWindow<ComponentListRemover>("Component List & Remove");
    }

    void OnGUI()
    {
        GUILayout.Label("Component List & Remove", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        if (Selection.gameObjects.Length == 0)
        {
            EditorGUILayout.HelpBox("Select 1 or more GameObjects in Hierarchy first.", MessageType.Info);
            return;
        }

        GUILayout.Label($"Selected {Selection.gameObjects.Length} object(s):", EditorStyles.boldLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        foreach (GameObject go in Selection.gameObjects)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(go.name, EditorStyles.boldLabel);

            Component[] components = go.GetComponents<Component>();
            foreach (Component comp in components)
            {
                if (comp == null) continue;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(comp, comp.GetType(), true, GUILayout.Width(300));
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    if (EditorUtility.DisplayDialog("Confirm Remove", $"Remove {comp.GetType().Name} from {go.name}?", "Yes", "Cancel"))
                    {
                        DestroyImmediate(comp);
                        Debug.Log($"Removed {comp.GetType().Name} from {go.name}");
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(5);

            if (GUILayout.Button($"Remove ALL (except Transform) from {go.name}", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Confirm Bulk Remove", $"Remove ALL components from {go.name} (except Transform)?", "Yes", "Cancel"))
                {
                    for (int i = components.Length - 1; i >= 0; i--)
                    {
                        if (components[i] is Transform) continue;
                        DestroyImmediate(components[i]);
                    }
                    Debug.Log($"Removed ALL components from {go.name}");
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(10);

        if (!string.IsNullOrEmpty(statusMessage))
        {
            EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
        }

        EditorGUILayout.HelpBox("Select objects in Hierarchy ? window auto-updates.\nRemove one by one or all at once.\nUndo (Ctrl+Z) works!", MessageType.Info);
    }
}
