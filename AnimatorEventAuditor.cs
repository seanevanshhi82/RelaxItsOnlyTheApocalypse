
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class AnimatorEventAuditor : EditorWindow
{
    private Vector2 scrollPos;
    private string statusMessage = "";
    private bool scanSelectedOnly = true;
    private bool showMissingHandlers = true;
    private bool showDuplicates = true;
    private bool showAllEvents = true;

    private List<EventInfo> allEvents = new List<EventInfo>();

    private class EventInfo
    {
        public string ClipName;
        public AnimationEvent Event;
        public string FunctionName => Event.functionName;
        public float Time => Event.time;
        public Object ObjectParameter => Event.objectReferenceParameter;
        public string StringParameter => Event.stringParameter;
        public float FloatParameter => Event.floatParameter;
        public int IntParameter => Event.intParameter;
        public string SourcePath;
    }

    [MenuItem("Tools/Animator Event Auditor")]
    public static void ShowWindow()
    {
        GetWindow<AnimatorEventAuditor>("Animator Event Auditor");
    }

    void OnGUI()
    {
        GUILayout.Label("Animator Event Auditor", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        scanSelectedOnly = EditorGUILayout.Toggle("Scan Selected Objects/Prefabs Only", scanSelectedOnly);
        showAllEvents = EditorGUILayout.Toggle("Show All Events", showAllEvents);
        showMissingHandlers = EditorGUILayout.Toggle("Highlight Missing Handlers", showMissingHandlers);
        showDuplicates = EditorGUILayout.Toggle("Highlight Duplicate Event Names", showDuplicates);

        EditorGUILayout.Space(20);

        if (GUILayout.Button("SCAN ANIMATIONS", GUILayout.Height(40)))
        {
            ScanAnimations();
        }

        EditorGUILayout.Space(10);

        if (allEvents.Count > 0)
        {
            GUILayout.Label($"Found {allEvents.Count} animation events:", EditorStyles.boldLabel);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(300));
            foreach (var evt in allEvents)
            {
                EditorGUILayout.BeginHorizontal();

                string label = $"{evt.ClipName} @ {evt.Time:F2}s ? {evt.FunctionName}";
                if (showMissingHandlers && string.IsNullOrEmpty(evt.FunctionName))
                    label += " (MISSING FUNCTION!)";
                if (showDuplicates && allEvents.Count(e => e.FunctionName == evt.FunctionName) > 1)
                    label += " (DUPLICATE)";

                EditorGUILayout.LabelField(label);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            if (GUILayout.Button("EXPORT REPORT TO CONSOLE", GUILayout.Height(30)))
            {
                ExportReport();
            }
        }

        EditorGUILayout.Space(10);

        if (!string.IsNullOrEmpty(statusMessage))
        {
            EditorGUILayout.HelpBox(statusMessage, MessageType.Info);
        }

        EditorGUILayout.HelpBox("Scans Animator Controllers or Animation Clips on selected objects (or all in project).\nHighlights missing event handlers and duplicates.\nGreat for checking attack events (EnableHandColliders, etc.)", MessageType.Info);
    }

    private void ScanAnimations()
    {
        allEvents.Clear();
        statusMessage = "";

        List<AnimatorController> controllers = new List<AnimatorController>();
        List<AnimationClip> clips = new List<AnimationClip>();

        if (scanSelectedOnly)
        {
            foreach (GameObject go in Selection.gameObjects)
            {
                Animator anim = go.GetComponent<Animator>();
                if (anim != null && anim.runtimeAnimatorController is AnimatorController controller)
                {
                    controllers.Add(controller);
                }

                AnimationClip[] goClips = go.GetComponentsInChildren<AnimationClip>(true);
                clips.AddRange(goClips);
            }
        }
        else
        {
            // Scan whole project (can be slow — use with caution)
            string[] guids = AssetDatabase.FindAssets("t:AnimatorController");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
                if (controller != null) controllers.Add(controller);
            }

            string[] clipGuids = AssetDatabase.FindAssets("t:AnimationClip");
            foreach (string guid in clipGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip != null) clips.Add(clip);
            }
        }

        // Process controllers
        foreach (AnimatorController controller in controllers)
        {
            foreach (AnimatorStateMachine stateMachine in controller.layers.Select(l => l.stateMachine))
            {
                ProcessStateMachine(stateMachine);
            }
        }

        // Process loose clips
        foreach (AnimationClip clip in clips)
        {
            ProcessClip(clip, "Loose Clip");
        }

        statusMessage = $"Scanned {controllers.Count} controllers + {clips.Count} clips. Found {allEvents.Count} events.";
        Repaint();
    }

    private void ProcessStateMachine(AnimatorStateMachine stateMachine)
    {
        foreach (AnimatorState state in stateMachine.states.Select(s => s.state))
        {
            if (state.motion is AnimationClip clip)
            {
                ProcessClip(clip, state.name);
            }
        }

        foreach (AnimatorStateMachine child in stateMachine.stateMachines.Select(s => s.stateMachine))
        {
            ProcessStateMachine(child);
        }
    }

    private void ProcessClip(AnimationClip clip, string sourceName)
    {
        if (clip == null) return;

        AnimationEvent[] events = AnimationUtility.GetAnimationEvents(clip);
        foreach (AnimationEvent evt in events)
        {
            allEvents.Add(new EventInfo
            {
                ClipName = clip.name,
                Event = evt,
                SourcePath = sourceName
            });
        }
    }

    private void ExportReport()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Animator Event Audit Report");
        sb.AppendLine("============================");

        var grouped = allEvents.GroupBy(e => e.ClipName);
        foreach (var group in grouped)
        {
            sb.AppendLine($"\nClip: {group.Key}");
            foreach (var evt in group)
            {
                sb.AppendLine($"  - {evt.Time:F2}s ? {evt.FunctionName} (obj: {evt.ObjectParameter?.name}, str: {evt.StringParameter}, float: {evt.FloatParameter}, int: {evt.IntParameter})");
            }
        }

        Debug.Log(sb.ToString());
        statusMessage = "Full report copied to Console (Ctrl+Shift+C to open).";
        Repaint();
    }
}
