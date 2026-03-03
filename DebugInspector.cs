#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Reflection;

[CustomEditor(typeof(MonoBehaviour), true, isFallback = true)]
public class DebugInspector : Editor
{
    private bool showDebug = false;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        showDebug = EditorGUILayout.Foldout(showDebug, "Debug (Private & Runtime)", true);
        if (showDebug)
        {
            EditorGUI.indentLevel++;

            System.Type type = target.GetType();
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            foreach (FieldInfo field in type.GetFields(flags))
            {
                if (field.IsStatic) continue;

                object value = field.GetValue(target);
                string label = field.Name + " (" + field.FieldType.Name + ")";

                if (value == null)
                {
                    EditorGUILayout.LabelField(label, "null");
                }
                else if (value is Object obj)
                {
                    EditorGUILayout.ObjectField(label, obj, typeof(Object), true);
                }
                else
                {
                    EditorGUILayout.LabelField(label, value.ToString());
                }
            }

            EditorGUI.indentLevel--;
        }
    }
}
#endif
