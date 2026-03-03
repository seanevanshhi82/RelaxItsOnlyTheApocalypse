using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class BulkMaterialReplacer : EditorWindow
{
    private Material oldMaterial;
    private Material newMaterial;
    private bool includeChildren = true;
    private Vector2 scrollPos;
    private List<Renderer> affectedRenderers = new List<Renderer>();
    private string statusMessage = "";

    [MenuItem("Tools/Bulk Material Replacer")]
    public static void ShowWindow()
    {
        GetWindow<BulkMaterialReplacer>("Bulk Material Replacer");
    }

    void OnGUI()
    {
        GUILayout.Label("Bulk Material Replacer", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        oldMaterial = (Material)EditorGUILayout.ObjectField("Old Material (to replace)", oldMaterial, typeof(Material), false);
        newMaterial = (Material)EditorGUILayout.ObjectField("New Material (replacement)", newMaterial, typeof(Material), false);

        includeChildren = EditorGUILayout.Toggle("Include Child Objects", includeChildren);

        EditorGUILayout.Space(20);

        if (GUILayout.Button("SCAN SELECTED OBJECTS", GUILayout.Height(30)))
        {
            ScanSelectedObjects();
        }

        EditorGUILayout.Space(10);

        if (affectedRenderers.Count > 0)
        {
            GUILayout.Label($"Found {affectedRenderers.Count} renderers using '{oldMaterial?.name}'", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
            foreach (var renderer in affectedRenderers)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(renderer, typeof(Renderer), true);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            if (GUILayout.Button("REPLACE ALL MATERIALS", GUILayout.Height(40)))
            {
                ReplaceMaterials();
            }
        }

        EditorGUILayout.Space(10);

        if (!string.IsNullOrEmpty(statusMessage))
        {
            EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
        }

        EditorGUILayout.HelpBox("1. Select prefabs or objects in Hierarchy/Project\n2. Drag old material\n3. Drag new material\n4. Click SCAN\n5. Click REPLACE\n\nUndo (Ctrl+Z) works if something goes wrong!", MessageType.Info);
    }

    private void ScanSelectedObjects()
    {
        if (oldMaterial == null)
        {
            statusMessage = "Select the old material first.";
            return;
        }

        affectedRenderers.Clear();
        statusMessage = "";

        GameObject[] selected = Selection.gameObjects;
        if (selected.Length == 0)
        {
            statusMessage = "Select at least one object in Hierarchy or Project.";
            return;
        }

        foreach (GameObject go in selected)
        {
            Renderer[] renderers = includeChildren ? go.GetComponentsInChildren<Renderer>(true) : go.GetComponents<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (renderer.sharedMaterial == oldMaterial || renderer.sharedMaterials.Any(m => m == oldMaterial))
                {
                    affectedRenderers.Add(renderer);
                }
            }
        }

        statusMessage = $"Found {affectedRenderers.Count} renderers using '{oldMaterial.name}'.";
        Repaint();
    }

    private void ReplaceMaterials()
    {
        if (oldMaterial == null || newMaterial == null)
        {
            statusMessage = "Select both old and new materials.";
            return;
        }

        if (affectedRenderers.Count == 0)
        {
            statusMessage = "No renderers found. Scan first.";
            return;
        }

        int changedCount = 0;

        foreach (Renderer renderer in affectedRenderers)
        {
            Undo.RecordObject(renderer, "Bulk Material Replace");

            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == oldMaterial)
                {
                    materials[i] = newMaterial;
                    changedCount++;
                }
            }

            renderer.sharedMaterials = materials;
            EditorUtility.SetDirty(renderer);
        }

        statusMessage = $"Replaced material on {changedCount} slots across {affectedRenderers.Count} renderers.";
        Debug.Log(statusMessage);
        Repaint();
    }
}