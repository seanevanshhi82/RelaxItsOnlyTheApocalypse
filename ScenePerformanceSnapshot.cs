

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;

public class ScenePerformanceSnapshot : EditorWindow
{
    private Vector2 scrollPos;
    private string reportText = "Click 'Generate Report' to analyze the current scene.";
    private bool includeSuggestions = true;

    [MenuItem("Tools/Scene Performance Snapshot")]
    public static void ShowWindow()
    {
        GetWindow<ScenePerformanceSnapshot>("Scene Performance Snapshot");
    }

    void OnGUI()
    {
        GUILayout.Label("Scene Performance Snapshot", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        includeSuggestions = EditorGUILayout.Toggle("Include Optimization Suggestions", includeSuggestions);

        EditorGUILayout.Space(20);

        if (GUILayout.Button("GENERATE REPORT", GUILayout.Height(40)))
        {
            GenerateReport();
        }

        EditorGUILayout.Space(10);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        EditorGUILayout.TextArea(reportText, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox("Analyzes the current open scene.\nCounts draw calls, batches, triangles, active objects, lights, audio, etc.\nSuggestions are basic � use Profiler for deep dives.", MessageType.Info);
    }

    private void GenerateReport()
    {
        if (UnityEngine.SceneManagement.SceneManager.loadedSceneCount == 0)
        {
            reportText = "No scene is open. Open a scene first.";
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== SCENE PERFORMANCE SNAPSHOT ===\n");

        // 1. General stats
        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
        sb.AppendLine($"Total GameObjects (active + inactive): {allObjects.Length}");

        int activeCount = 0;
        int zombieCount = 0;
        int lightCount = 0;
        int audioCount = 0;

        foreach (GameObject go in allObjects)
        {
            if (go.activeInHierarchy) activeCount++;
            if (go.CompareTag("Infected") || go.name.ToLower().Contains("zombie")) zombieCount++;
            if (go.GetComponent<Light>() != null) lightCount++;
            if (go.GetComponent<AudioSource>() != null) audioCount++;
        }

        sb.AppendLine($"Active GameObjects: {activeCount}");
        sb.AppendLine($"Estimated Zombies (by tag/name): {zombieCount}");
        sb.AppendLine($"Lights: {lightCount}");
        sb.AppendLine($"Audio Sources: {audioCount}\n");

        // 2. Mesh / Renderer stats
        MeshRenderer[] renderers = FindObjectsOfType<MeshRenderer>(true);
        int triangleCount = 0;
        int vertexCount = 0;
        int materialCount = 0;

        foreach (MeshRenderer renderer in renderers)
        {
            MeshFilter mf = renderer.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                triangleCount += mf.sharedMesh.triangles.Length / 3;
                vertexCount += mf.sharedMesh.vertexCount;
            }
            materialCount += renderer.sharedMaterials.Length;
        }

        sb.AppendLine($"Mesh Renderers: {renderers.Length}");
        sb.AppendLine($"Total Triangles (approx): {triangleCount:N0}");
        sb.AppendLine($"Total Vertices (approx): {vertexCount:N0}");
        sb.AppendLine($"Materials used: {materialCount}\n");

        // 3. Basic draw call / batch estimate
        int estimatedBatches = renderers.Length; // very rough � real batches depend on materials/shaders
        sb.AppendLine($"Rough Estimated Batches (before static/dynamic batching): {estimatedBatches}");
        sb.AppendLine("(Note: Unity's batching can reduce this significantly. Use Frame Debugger for real numbers.)\n");

        // 4. Suggestions
        if (includeSuggestions)
        {
            sb.AppendLine("=== OPTIMIZATION SUGGESTIONS ===\n");

            if (lightCount > 10)
            {
                sb.AppendLine("� Too many lights (>10) � consider baking static lights or using fewer real-time ones.");
            }

            if (zombieCount > 50)
            {
                sb.AppendLine("� Large zombie count � LOD groups, occlusion culling, or pooling would help performance.");
            }

            if (triangleCount > 500000)
            {
                sb.AppendLine("� High triangle count � optimize meshes (reduce poly count) or use LODs.");
            }

            if (audioCount > 20)
            {
                sb.AppendLine("� Many AudioSources � pool them or disable when far away to save CPU.");
            }

            if (estimatedBatches > 300)
            {
                sb.AppendLine("� High batch count � combine meshes, use fewer materials, enable static/dynamic batching.");
            }
        }

        reportText = sb.ToString();
        Repaint();
    }
}