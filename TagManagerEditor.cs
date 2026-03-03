using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class TagManagerEditor : EditorWindow
{
    private string newTag = "";
    private string oldTag = "";
    private string assignTag = "";
    private Vector2 scrollPos;
    private string statusMessage = "";

    [MenuItem("Tools/Quick Tag Manager")]
    public static void ShowWindow()
    {
        GetWindow<TagManagerEditor>("Quick Tag Manager");
    }

    void OnGUI()
    {
        GUILayout.Label("Quick Tag Manager", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        GUILayout.Label("Add New Tag");
        newTag = EditorGUILayout.TextField("New Tag Name", newTag);
        if (GUILayout.Button("ADD TAG", GUILayout.Height(30)))
        {
            AddTag();
        }

        EditorGUILayout.Space(10);

        GUILayout.Label("Rename Existing Tag");
        oldTag = EditorGUILayout.TextField("Old Tag", oldTag);
        newTag = EditorGUILayout.TextField("New Name", newTag);
        if (GUILayout.Button("RENAME TAG", GUILayout.Height(30)))
        {
            RenameTag();
        }

        EditorGUILayout.Space(10);

        GUILayout.Label("Bulk Assign Tag to Selected Objects");
        assignTag = EditorGUILayout.TextField("Assign This Tag", assignTag);
        if (GUILayout.Button("ASSIGN TO SELECTED", GUILayout.Height(30)))
        {
            BulkAssignTag();
        }

        EditorGUILayout.Space(10);

        if (!string.IsNullOrEmpty(statusMessage))
        {
            EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
        }

        EditorGUILayout.HelpBox("Manage tags without opening Tag Manager window.\nChanges apply instantly.\nUndo (Ctrl+Z) works for bulk assign.", MessageType.Info);
    }

    private void AddTag()
    {
        if (string.IsNullOrEmpty(newTag))
        {
            statusMessage = "Enter a tag name.";
            return;
        }

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TagManager.asset"));
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        if (tagsProp != null)
        {
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == newTag)
                {
                    statusMessage = "Tag already exists.";
                    return;
                }
            }

            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            SerializedProperty newTagProp = tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1);
            newTagProp.stringValue = newTag;
            tagManager.ApplyModifiedProperties();
            statusMessage = $"Tag '{newTag}' added.";
        }
    }

    private void RenameTag()
    {
        if (string.IsNullOrEmpty(oldTag) || string.IsNullOrEmpty(newTag))
        {
            statusMessage = "Enter both old and new tag names.";
            return;
        }

        if (oldTag == newTag)
        {
            statusMessage = "New name same as old.";
            return;
        }

        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TagManager.asset"));
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        bool found = false;

        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == oldTag)
            {
                tagsProp.GetArrayElementAtIndex(i).stringValue = newTag;
                found = true;
                break;
            }
        }

        if (found)
        {
            tagManager.ApplyModifiedProperties();
            statusMessage = $"Renamed '{oldTag}' to '{newTag}'.";

            // Bulk update all GameObjects in scene
            GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
            int updated = 0;
            foreach (GameObject go in allObjects)
            {
                if (go.tag == oldTag)
                {
                    go.tag = newTag;
                    EditorUtility.SetDirty(go);
                    updated++;
                }
            }

            if (updated > 0)
            {
                statusMessage += $"\nUpdated {updated} objects in scene.";
            }
        }
        else
        {
            statusMessage = $"Tag '{oldTag}' not found.";
        }
    }

    private void BulkAssignTag()
    {
        if (string.IsNullOrEmpty(assignTag))
        {
            statusMessage = "Enter a tag to assign.";
            return;
        }

        GameObject[] selected = Selection.gameObjects;
        if (selected.Length == 0)
        {
            statusMessage = "No objects selected.";
            return;
        }

        int assigned = 0;
        foreach (GameObject go in selected)
        {
            if (go.tag != assignTag)
            {
                go.tag = assignTag;
                EditorUtility.SetDirty(go);
                assigned++;
            }
        }

        if (assigned > 0)
        {
            AssetDatabase.SaveAssets();
            statusMessage = $"Assigned '{assignTag}' to {assigned} selected objects.";
        }
        else
        {
            statusMessage = "No changes needed.";
        }
    }
}
