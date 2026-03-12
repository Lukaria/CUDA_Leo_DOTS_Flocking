using System;
using UnityEditor;
using UnityEngine;

namespace CustomEditor
{

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class RuntimeReadonlyAttribute : PropertyAttribute {}
    
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(RuntimeReadonlyAttribute))]
    public class RuntimeReadonlyPropertyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
        
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var previousGUIState = GUI.enabled;
            if (Application.isPlaying)
            {
                GUI.enabled = false;
                EditorGUI.PropertyField(position, property, label, true);
                GUI.enabled = previousGUIState;
            }
            else
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
            
        }
    }
#endif
}