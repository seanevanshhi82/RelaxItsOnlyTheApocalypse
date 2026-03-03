using UnityEngine;
using UnityEditor;

public class ComponentRemover : EditorWindow
{
    private GameObject[] selectedObjects;
    private Vector2 scrollPos;

    [MenuItem("Tools/Component Remover")]
    public static void ShowWindow()
    {
        GetWindow<ComponentRemover>("Component Remover");
    }

    void OnGUI()
    {
        GUILayout.Label("Select GameObject(s) in Hierarchy, then click below:", EditorStyles.boldLabel);

        if (GUILayout.Button("Refresh Selected Objects", GUILayout.Height(30)))
        {
            selectedObjects = Selection.gameObjects;
        }

        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            EditorGUILayout.HelpBox("Select 1+ GameObjects in Hierarchy first.", MessageType.Info);
            return;
        }

        GUILayout.Label($"Found {selectedObjects.Length} object(s):", EditorStyles.boldLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        foreach (var obj in selectedObjects)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(obj, typeof(GameObject), true);
            EditorGUILayout.EndHorizontal();

            // List components on this object
            Component[] components = obj.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp == null) continue;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(comp, comp.GetType(), true, GUILayout.Width(300));
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    if (EditorUtility.DisplayDialog("Confirm Remove", $"Remove {comp.GetType()} from {obj.name}?", "Yes", "Cancel"))
                    {
                        DestroyImmediate(comp);
                        Debug.Log($"Removed {comp.GetType()} from {obj.name}");
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(10);
        if (GUILayout.Button("REMOVE ALL COMPONENTS FROM ALL SELECTED OBJECTS", GUILayout.Height(40), GUILayout.ExpandWidth(true)))
        {
            if (EditorUtility.DisplayDialog("DANGER ZONE", "This will DESTROY ALL components on selected objects (except Transform). Are you sure?", "YES DESTROY", "Cancel"))
            {
                foreach (var obj in selectedObjects)
                {
                    Component[] components = obj.GetComponents<Component>();
                    for (int i = components.Length - 1; i >= 0; i--) // Reverse to avoid index shifting
                    {
                        if (components[i] is Transform) continue; // Never remove Transform
                        DestroyImmediate(components[i]);
                    }
                    Debug.Log($"Removed ALL components from {obj.name}");
                }
                AssetDatabase.SaveAssets();
            }
        }

        GUILayout.Space(10);
        EditorGUILayout.HelpBox("WARNING: This destroys components permanently. Use Undo (Ctrl+Z) if you make a mistake!", MessageType.Warning);
    }
}
