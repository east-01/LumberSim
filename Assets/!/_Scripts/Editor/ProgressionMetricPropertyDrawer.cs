using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ProgressionMetric))]
public class ProgressionMetricPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        float lh      = EditorGUIUtility.singleLineHeight;
        float vs      = EditorGUIUtility.standardVerticalSpacing;

        //—— First row: the "name" field ——//
        var nameProp  = property.FindPropertyRelative("name");
        Rect nameRect = new Rect(position.x, position.y, position.width, lh);

        // draw the property label and then the string field for name
        Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, lh);
        EditorGUI.LabelField(labelRect, label);
        Rect fieldRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, lh);
        EditorGUI.PropertyField(fieldRect, nameProp, GUIContent.none);

        //—— Second row: type + value inline ——//
        var typeProp  = property.FindPropertyRelative("type");
        var boolProp  = property.FindPropertyRelative("boolValue");
        var intProp   = property.FindPropertyRelative("intValue");
        var floatProp = property.FindPropertyRelative("floatValue");

        Rect rowRect  = new Rect(position.x, position.y + lh + vs, position.width, lh);
        float halfW   = rowRect.width * 0.5f;
        Rect typeRect = new Rect(rowRect.x, rowRect.y, halfW - 2, lh);
        Rect valueRect= new Rect(rowRect.x + halfW + 2, rowRect.y, halfW - 2, lh);

        EditorGUI.PropertyField(typeRect, typeProp, GUIContent.none);
        switch ((ProgressionMetric.MetricType)typeProp.enumValueIndex)
        {
            case ProgressionMetric.MetricType.Boolean:
                EditorGUI.PropertyField(valueRect, boolProp, GUIContent.none);
                break;
            case ProgressionMetric.MetricType.Integer:
                EditorGUI.PropertyField(valueRect, intProp, GUIContent.none);
                break;
            case ProgressionMetric.MetricType.Float:
                EditorGUI.PropertyField(valueRect, floatProp, GUIContent.none);
                break;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // two lines: name + (type+value)
        return EditorGUIUtility.singleLineHeight * 2
             + EditorGUIUtility.standardVerticalSpacing;
    }
}
