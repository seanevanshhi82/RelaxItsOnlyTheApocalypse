using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MissingScriptCleaner : EditorWindow
{
    private Vector2 scrollPos;
    private bool includePrefabs = true;
    private bool includeSceneObjects = true;
    private string statusMessage = "";

    [MenuItem("Tools/Clean Missing Scripts")]
    public static void ShowWindow()
    {
        GetWindow<MissingScriptCleaner>("Clean Missing Scripts");
    }

    void OnGUI()
    {
        GUILayout.Label("Clean Missing (Mono Script) Components", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        includePrefabs = EditorGUILayout.Toggle("Include Prefabs in Project", includePrefabs);
        includeSceneObjects = EditorGUILayout.Toggle("Include Scene Objects", includeSceneObjects);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("SCAN & CLEAN ALL MISSING SCRIPTS", GUILayout.Height(40)))
        {
            CleanMissingScripts();
        }

        EditorGUILayout.Space(10);

        if (!string.IsNullOrEmpty(statusMessage))
        {
            EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox("WARNING: This permanently removes components. Use Undo (Ctrl+Z) if something goes wrong!", MessageType.Warning);
    }

    private void CleanMissingScripts()
    {
        int removedCount = 0;
        int scannedCount = 0;

        // 1. Clean prefabs in project (if enabled)
        if (includePrefabs)
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                scannedCount++;

                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(path);
                Component[] components = prefabRoot.GetComponentsInChildren<Component>(true);

                bool changed = false;
                for (int i = components.Length - 1; i >= 0; i--)
                {
                    if (components[i] == null) // Missing script!
                    {
                        DestroyImmediate(components[i], true);
                        removedCount++;
                        changed = true;
                    }
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, path);
                    Debug.Log($"Cleaned missing scripts from prefab: {path}");
                }

                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        // 2. Clean objects in current open scenes (if enabled)
        if (includeSceneObjects)
        {
            GameObject[] sceneObjects = FindObjectsOfType<GameObject>(true);
            foreach (GameObject go in sceneObjects)
            {
                scannedCount++;

                Component[] components = go.GetComponentsInChildren<Component>(true);
                bool changed = false;

                for (int i = components.Length - 1; i >= 0; i--)
                {
                    if (components[i] == null) // Missing script!
                    {
                        DestroyImmediate(components[i], true);
                        removedCount++;
                        changed = true;
                    }
                }

                if (changed)
                {
                    EditorUtility.SetDirty(go);
                    Debug.Log($"Cleaned missing scripts from scene object: {go.name}");
                }
            }

            if (removedCount > 0)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
        }

        statusMessage = $"Scan complete! Removed {removedCount} missing script components from {scannedCount} objects.";
        Debug.Log(statusMessage);
    }
}