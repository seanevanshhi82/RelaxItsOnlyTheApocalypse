
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ComponentCopier : EditorWindow
{
    private GameObject sourceObject;
    private GameObject[] targetObjects;
    private Vector2 scrollPos;
    private bool copyAll = true;
    private string statusMessage = "";

    [MenuItem("Tools/Quick Component Copier")]
    public static void ShowWindow()
    {
        GetWindow<ComponentCopier>("Component Copier");
    }

    void OnGUI()
    {
        GUILayout.Label("Quick Component Copier", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        sourceObject = (GameObject)EditorGUILayout.ObjectField("Source Object", sourceObject, typeof(GameObject), true);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Grab Selected as Targets", GUILayout.Height(30)))
        {
            targetObjects = Selection.gameObjects;
            if (targetObjects.Length == 0)
            {
                statusMessage = "No objects selected in Hierarchy.";
            }
            else
            {
                statusMessage = $"Selected {targetObjects.Length} target objects.";
            }
        }

        EditorGUILayout.Space(10);

        copyAll = EditorGUILayout.Toggle("Copy ALL components (except Transform)", copyAll);

        EditorGUILayout.Space(10);

        if (sourceObject != null && targetObjects != null && targetObjects.Length > 0 && GUILayout.Button("COPY COMPONENTS TO TARGETS", GUILayout.Height(40)))
        {
            CopyComponents();
        }

        EditorGUILayout.Space(10);

        if (!string.IsNullOrEmpty(statusMessage))
        {
            EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
        }

        EditorGUILayout.HelpBox("1. Select source object (with components you want to copy)\n2. Select target objects in Hierarchy\n3. Click 'Grab Selected as Targets'\n4. Hit the big button to copy", MessageType.Info);
    }

    private void CopyComponents()
    {
        if (sourceObject == null)
        {
            statusMessage = "No source object selected.";
            return;
        }

        if (targetObjects == null || targetObjects.Length == 0)
        {
            statusMessage = "No target objects selected.";
            return;
        }

        int copiedCount = 0;

        Component[] sourceComponents = sourceObject.GetComponents<Component>();
        foreach (GameObject target in targetObjects)
        {
            if (target == sourceObject) continue; // Skip self

            foreach (Component sourceComp in sourceComponents)
            {
                if (sourceComp == null) continue;
                if (sourceComp is Transform) continue; // Never copy Transform

                if (copyAll || EditorUtility.DisplayDialog("Confirm Copy", $"Copy {sourceComp.GetType().Name} to {target.name}?", "Yes", "Skip"))
                {
                    UnityEditorInternal.ComponentUtility.CopyComponent(sourceComp);
                    UnityEditorInternal.ComponentUtility.PasteComponentAsNew(target);
                    copiedCount++;
                    Debug.Log($"Copied {sourceComp.GetType().Name} to {target.name}");
                }
            }
        }

        AssetDatabase.SaveAssets();
        statusMessage = $"Copied {copiedCount} components to {targetObjects.Length} objects!";
        Repaint();
    }
}
