using UnityEditor;
using UnityEngine;

namespace TK.PhysicsBone
{
    [CustomPropertyDrawer(typeof(PhysicsBoneCurveRectAttribute))]
    public class PhysicsBoneRectDrawerEditor : PropertyDrawer
    {
        private PhysicsBoneCurveRectAttribute curveRectAttribute { get { return attribute as PhysicsBoneCurveRectAttribute; } }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            if (property.propertyType == SerializedPropertyType.AnimationCurve)
            {
                EditorGUI.CurveField(position, property, curveRectAttribute.color, curveRectAttribute.rect, label);
            }
            else
            {
                EditorGUI.HelpBox(position, typeof(PhysicsBoneCurveRectAttribute).Name + " used on a non-AnimationCurve field", MessageType.Warning);
            }
            EditorGUI.EndProperty();
        }
    }
}
