using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace TK.BurstJob.DynamicBone
{
    [CustomPropertyDrawer(typeof(Curve01Attribute))]
    public class AnimationCurveEditor : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.AnimationCurve) return;

            var ranges = new Rect(0, 0, 1, 1);

            EditorGUI.CurveField
            (
             position: position,
             property: property,
             color: Color.cyan,
             ranges: ranges
            );
        }
    }

    [CustomPropertyDrawer(typeof(DynamicBoneEditorAttribute))]
    public class DynamicBoneBoneDrowerEditor : PropertyDrawer
    {
        private Editor nestedEditor;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            if (property.hasMultipleDifferentValues)
            {
                EditorGUI.PropertyField(position, property, label);
            }
            else
            {
                EditorGUI.PropertyField(position, property, label, true);
                if (property.objectReferenceValue != null)
                {
                    property.isExpanded = EditorGUI.Foldout(new Rect(position) { width = 0 }, property.isExpanded, GUIContent.none);
                    if (property.isExpanded)
                    {
                        EditorGUI.indentLevel++;
                        if (property.type == "PPtr<$Material>")
                        {
                            Editor.CreateCachedEditor(property.objectReferenceValue, typeof(MaterialEditor), ref nestedEditor);
                            (nestedEditor as MaterialEditor).PropertiesGUI();
                        }
                        else
                        {
                            Editor.CreateCachedEditor(property.objectReferenceValue, null, ref nestedEditor);
                            nestedEditor.OnInspectorGUI();
                        }
                        EditorGUI.indentLevel--;
                    }
                }
            }
            EditorGUI.EndProperty();
        }
    }
}
