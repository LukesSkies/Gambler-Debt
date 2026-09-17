using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(KeyItem))]
public class KeyItemEditor : Editor
{
    private KeyItem _keyItem;

    private void OnEnable()
    {
        _keyItem = (KeyItem)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.LabelField($"Item", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        _keyItem.Id = EditorGUILayout.IntField("ID", _keyItem.Id);
        if (GUILayout.Button("Define", GUILayout.Width(100)))
        {
            _keyItem.Id = ItemRegistry.GetFreeID(_keyItem);
        }
        EditorGUILayout.EndHorizontal();

        _keyItem.Name = EditorGUILayout.TextField("Name", _keyItem.Name);
        _keyItem.Icon = (Sprite)EditorGUILayout.ObjectField("Icon", _keyItem.Icon, typeof(Sprite), false);
        
        if (GUI.changed)
        {
            EditorUtility.SetDirty(_keyItem);
        }
        serializedObject.ApplyModifiedProperties();
    }
}
