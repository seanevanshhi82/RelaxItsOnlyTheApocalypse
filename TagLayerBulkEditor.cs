using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class TagLayerBulkEditor : EditorWindow
{
    private string currentTag = "Untagged";
    private string newTag = "Untagged";
    private int currentLayer = 0;
    private int newLayer = 0;
    private bool changeTag = true;
    private bool changeLayer = false;
    private Vector2 scrollPos;
    private List<GameObject> selectedObjects = new List<GameObject>();
    private string statusMessage = "";

    [MenuItem("Tools/Bulk Tag & Layer Editor")]
    public static void ShowWindow()
    {
        GetWindow<TagLayerBulkEditor>("Bulk Tag/Layer Editor");
    }

    void OnGUI()
    {
        GUILayout.Label("Bulk Tag & Layer Editor", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        changeTag = EditorGUILayout.ToggleLeft("Change Tag", changeTag, GUILayout.Width(120));
        changeLayer = EditorGUILayout.ToggleLeft("Change Layer", changeLayer, GUILayout.Width(120));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        if (changeTag)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Current Tag:", GUILayout.Width(100));
            currentTag = EditorGUILayout.TextField(currentTag);
            GUILayout.Label("New Tag:", GUILayout.Width(80));
            newTag = EditorGUILayout.TextField(newTag);
            EditorGUILayout.EndHorizontal();
        }

        if (changeLayer)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Current Layer:", GUILayout.Width(100));
            currentLayer = EditorGUILayout.IntField(currentLayer);
            GUILayout.Label("New Layer:", GUILayout.Width(80));
            newLayer = EditorGUILayout.IntField(newLayer);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Grab Selected Objects", GUILayout.Height(30)))
        {
            selectedObjects = Selection.gameObjects.ToList();
            statusMessage = $"Selected {selectedObjects.Count} objects.";
        }

        EditorGUILayout.Space(10);

        if (selectedObjects.Count > 0)
        {
            GUILayout.Label($"Objects to change ({selectedObjects.Count}):", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(150));
            foreach (var go in selectedObjects)
            {
                EditorGUILayout.LabelField(go.name);
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            if (GUILayout.Button("APPLY CHANGES", GUILayout.Height(40)))
            {
                ApplyBulkChanges();
            }
        }

        EditorGUILayout.Space(10);

        if (!string.IsNullOrEmpty(statusMessage))
        {
            EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
        }

        EditorGUILayout.HelpBox("1. Select objects in Hierarchy\n2. Click 'Grab Selected Objects'\n3. Set new tag/layer\n4. Hit 'APPLY CHANGES'\n\nUndo (Ctrl+Z) works!", MessageType.Info);
    }

    private void ApplyBulkChanges()
    {
        if (selectedObjects.Count == 0)
        {
            statusMessage = "No objects selected.";
            return;
        }

        int changedCount = 0;

        foreach (GameObject go in selectedObjects)
        {
            bool changed = false;

            if (changeTag && go.tag == currentTag && !string.IsNullOrEmpty(newTag))
            {
                go.tag = newTag;
                changed = true;
            }

            if (changeLayer && go.layer == currentLayer)
            {
                go.layer = newLayer;
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(go);
                changedCount++;
            }
        }

        if (changedCount > 0)
        {
            AssetDatabase.SaveAssets();
            statusMessage = $"Changed {changedCount} objects successfully!";
        }
        else
        {
            statusMessage = "No changes applied (check current values).";
        }

        Repaint();
    }
}