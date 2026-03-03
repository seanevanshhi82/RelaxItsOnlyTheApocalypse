

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class PrefabInstanceReplacer : EditorWindow
{
    private GameObject oldPrefab;
    private GameObject newPrefab;
    private string statusMessage = "";

    [MenuItem("Tools/Prefab Instance Replacer")]
    public static void ShowWindow()
    {
        GetWindow<PrefabInstanceReplacer>("Prefab Replacer");
    }

    void OnGUI()
    {
        GUILayout.Label("Prefab Instance Replacer", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        oldPrefab = (GameObject)EditorGUILayout.ObjectField("Old Prefab (to replace)", oldPrefab, typeof(GameObject), false);
        newPrefab = (GameObject)EditorGUILayout.ObjectField("New Prefab (replacement)", newPrefab, typeof(GameObject), false);

        EditorGUILayout.Space(20);

        if (GUILayout.Button("REPLACE ALL INSTANCES IN SCENE", GUILayout.Height(40)))
        {
            ReplaceInstances();
        }

        EditorGUILayout.Space(10);

        if (!string.IsNullOrEmpty(statusMessage))
        {
            EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
        }

        EditorGUILayout.HelpBox("1. Drag old prefab\n2. Drag new prefab\n3. Click REPLACE\n\nReplaces all instances in current scene.\nUndo (Ctrl+Z) works!", MessageType.Info);
    }

    private void ReplaceInstances()
    {
        if (oldPrefab == null || newPrefab == null)
        {
            statusMessage = "Select both old and new prefabs.";
            return;
        }

        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
        int replacedCount = 0;

        foreach (GameObject go in allObjects)
        {
            if (PrefabUtility.GetCorrespondingObjectFromSource(go) == oldPrefab)
            {
                GameObject newInstance = PrefabUtility.InstantiatePrefab(newPrefab) as GameObject;
                if (newInstance == null) continue;

                newInstance.transform.SetParent(go.transform.parent);
                newInstance.transform.localPosition = go.transform.localPosition;
                newInstance.transform.localRotation = go.transform.localRotation;
                newInstance.transform.localScale = go.transform.localScale;
                newInstance.name = go.name;

                Undo.RegisterCreatedObjectUndo(newInstance, "Replace Prefab Instance");
                Undo.DestroyObjectImmediate(go);

                replacedCount++;
            }
        }

        statusMessage = $"Replaced {replacedCount} instances of {oldPrefab.name} with {newPrefab.name}.";
        Debug.Log(statusMessage);
        Repaint();
    }
}
