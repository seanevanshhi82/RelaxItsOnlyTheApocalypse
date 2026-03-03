using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class BatchPrefabRenamer : EditorWindow
{
    private string searchString = "";
    private string replaceString = "";
    private string folderPath = "Assets/Prefabs";
    private Vector2 scrollPos;
    private List<string> foundPrefabs = new List<string>();
    private string statusMessage = "";

    [MenuItem("Tools/Batch Rename Prefabs")]
    public static void ShowWindow()
    {
        GetWindow<BatchPrefabRenamer>("Batch Rename Prefabs");
    }

    void OnGUI()
    {
        GUILayout.Label("Batch Rename Prefabs", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        folderPath = EditorGUILayout.TextField("Folder Path", folderPath);

        EditorGUILayout.BeginHorizontal();
        searchString = EditorGUILayout.TextField("Find", searchString);
        replaceString = EditorGUILayout.TextField("Replace With", replaceString);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        if (GUILayout.Button("SCAN PREFABS", GUILayout.Height(30)))
        {
            ScanPrefabs();
        }

        EditorGUILayout.Space(10);

        if (foundPrefabs.Count > 0)
        {
            GUILayout.Label($"Found {foundPrefabs.Count} prefabs:", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
            foreach (var path in foundPrefabs)
            {
                EditorGUILayout.LabelField(Path.GetFileName(path));
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            if (GUILayout.Button("RENAME ALL", GUILayout.Height(40)))
            {
                RenamePrefabs();
            }
        }

        EditorGUILayout.Space(10);

        if (!string.IsNullOrEmpty(statusMessage))
        {
            EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
        }

        EditorGUILayout.HelpBox("Scans all .prefab files in the folder.\nReplaces text in filenames (case-sensitive).\nExample: Find 'Zombie' ? Replace 'Zed' ? Zombie_Base becomes Zed_Base", MessageType.Info);
    }

    private void ScanPrefabs()
    {
        foundPrefabs.Clear();
        statusMessage = "";

        if (!Directory.Exists(folderPath))
        {
            statusMessage = $"Folder not found: {folderPath}";
            return;
        }

        string[] prefabPaths = Directory.GetFiles(folderPath, "*.prefab", SearchOption.AllDirectories);
        foreach (string path in prefabPaths)
        {
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (fileName.Contains(searchString))
            {
                foundPrefabs.Add(path);
            }
        }

        statusMessage = $"Found {foundPrefabs.Count} prefabs containing '{searchString}'";
        Repaint();
    }

    private void RenamePrefabs()
    {
        if (foundPrefabs.Count == 0)
        {
            statusMessage = "Nothing to rename.";
            return;
        }

        if (EditorUtility.DisplayDialog("Confirm Rename", $"Rename {foundPrefabs.Count} prefabs?\nThis cannot be undone without version control!", "Yes, Rename", "Cancel"))
        {
            int renamedCount = 0;

            foreach (string oldPath in foundPrefabs)
            {
                string oldName = Path.GetFileNameWithoutExtension(oldPath);
                string newName = oldName.Replace(searchString, replaceString);
                string newPath = Path.Combine(Path.GetDirectoryName(oldPath), newName + ".prefab");

                if (AssetDatabase.RenameAsset(oldPath, newName) == "")
                {
                    renamedCount++;
                    Debug.Log($"Renamed: {oldPath} ? {newPath}");
                }
                else
                {
                    Debug.LogWarning($"Failed to rename: {oldPath}");
                }
            }

            AssetDatabase.Refresh();
            statusMessage = $"Renamed {renamedCount} prefabs successfully!";
            foundPrefabs.Clear();
            Repaint();
        }
    }
}