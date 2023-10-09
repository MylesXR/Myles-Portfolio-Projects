using UnityEditor;
using UnityEngine;

[System.Serializable()]
public struct VolcapTimeline
{
    [SerializeField()]
    private float _maxValue;
    [SerializeField()]
    private float _value;

    public float MaxValue
    {
        get { return _maxValue; }
        set
        {
            _maxValue = value;
            _value = Mathf.Clamp(_value, 0, _maxValue);
        }
    }

    public float Value
    {
        get { return _value; }
        set { _value = Mathf.Clamp(value, 0, _maxValue); }
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(VolcapTimeline))]
public class IntRangeInspector : PropertyDrawer
{

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight * 1f;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        int minValue = 0;
        float maxValue = property.FindPropertyRelative("_maxValue").floatValue;

        var valueProp = property.FindPropertyRelative("_value");
        valueProp.floatValue = Mathf.Clamp(valueProp.floatValue, minValue, maxValue);

        EditorGUILayout.Slider(valueProp, minValue, maxValue, GUIContent.none);
    }
}
#endif 
