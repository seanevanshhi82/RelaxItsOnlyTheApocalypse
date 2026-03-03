using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class HierarchySearch : EditorWindow
{
    private string searchText = "";
    private string searchMode = "Name";
    private Vector2 scrollPos;
    private List<GameObject> searchResults = new List<GameObject>();
    private string statusMessage = "";

    private static readonly string[] searchModes = { "Name", "Tag", "Component Type", "Layer" };

    [MenuItem("Tools/Hierarchy Search & Select")]
    public static void ShowWindow()
    {
        GetWindow<HierarchySearch>("Hierarchy Search");
    }

    void OnGUI()
    {
        GUILayout.Label("Hierarchy Search & Select", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        searchText = EditorGUILayout.TextField("Search For", searchText);

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Search By:", GUILayout.Width(80));
        searchMode = searchModes[EditorGUILayout.Popup(searchModes.ToList().IndexOf(searchMode), searchModes)];
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        if (GUILayout.Button("SEARCH SCENE", GUILayout.Height(40)))
        {
            PerformSearch();
        }

        EditorGUILayout.Space(10);

        if (searchResults.Count > 0)
        {
            GUILayout.Label($"Found {searchResults.Count} objects:", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
            foreach (var go in searchResults)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(go, typeof(GameObject), true);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            if (GUILayout.Button("SELECT ALL RESULTS", GUILayout.Height(30)))
            {
                Selection.objects = searchResults.ToArray();
                EditorUtility.FocusProjectWindow();
                statusMessage = $"{searchResults.Count} objects selected in Hierarchy.";
            }
        }

        EditorGUILayout.Space(10);

        if (!string.IsNullOrEmpty(statusMessage))
        {
            EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
        }

        EditorGUILayout.HelpBox("Search current open scene by:\n- Name (contains text)\n- Tag\n- Component Type (e.g., 'Rigidbody')\n- Layer (by name, e.g., 'Infected')", MessageType.Info);
    }

    private void PerformSearch()
    {
        searchResults.Clear();
        statusMessage = "";

        if (string.IsNullOrWhiteSpace(searchText))
        {
            statusMessage = "Enter search text.";
            return;
        }

        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
        List<GameObject> matches = new List<GameObject>();

        foreach (GameObject go in allObjects)
        {
            if (go == null) continue;

            bool match = false;

            switch (searchMode)
            {
                case "Name":
                    match = go.name.ToLower().Contains(searchText.ToLower());
                    break;

                case "Tag":
                    match = go.tag.ToLower().Contains(searchText.ToLower());
                    break;

                case "Component Type":
                    match = go.GetComponents<Component>().Any(c => c != null && c.GetType().Name.ToLower().Contains(searchText.ToLower()));
                    break;

                case "Layer":
                    match = LayerMask.LayerToName(go.layer).ToLower().Contains(searchText.ToLower());
                    break;
            }

            if (match)
            {
                matches.Add(go);
            }
        }

        searchResults = matches;
        statusMessage = $"Found {searchResults.Count} matching objects.";
        Repaint();
    }
}

