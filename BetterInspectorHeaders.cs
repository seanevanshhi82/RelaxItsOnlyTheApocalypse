
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MonoBehaviour), true, isFallback = true)]
public class BetterInspectorEditor : Editor
{
    private Dictionary<string, bool> foldouts = new Dictionary<string, bool>();

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;

        while (prop.NextVisible(enterChildren))
        {
            if (prop.name == "m_Script") continue;

            // Custom header support
            if (prop.isArray || prop.propertyType == SerializedPropertyType.ObjectReference)
            {
                if (prop.name.EndsWith("Foldout"))
                {
                    string groupName = prop.name.Replace("Foldout", "");
                    if (!foldouts.ContainsKey(groupName)) foldouts[groupName] = true;

                    foldouts[groupName] = EditorGUILayout.Foldout(foldouts[groupName], groupName, true);
                    EditorGUI.indentLevel++;
                    enterChildren = foldouts[groupName];
                }
                else
                {
                    EditorGUILayout.PropertyField(prop, true);
                    enterChildren = false;
                }
            }
            else
            {
                EditorGUILayout.PropertyField(prop, true);
                enterChildren = false;
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
