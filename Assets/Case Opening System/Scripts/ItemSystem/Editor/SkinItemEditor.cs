using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SkinItem))]
public class SkinItemEditor : Editor
{
    private SkinItem _skinItem;

    private void OnEnable()
    {
        _skinItem = (SkinItem)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField($"Item", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        _skinItem.Id = EditorGUILayout.IntField("ID", _skinItem.Id);
        if (GUILayout.Button("Define", GUILayout.Width(100)))
        {
            _skinItem.Id = ItemRegistry.GetFreeID(_skinItem);
        }
        EditorGUILayout.EndHorizontal();

        _skinItem.Name = EditorGUILayout.TextField("Name", _skinItem.Name);
        _skinItem.Icon = (Sprite)EditorGUILayout.ObjectField("Icon", _skinItem.Icon, typeof(Sprite), false);

        EditorGUILayout.LabelField($"Skin", EditorStyles.boldLabel);
        _skinItem.Weapon = (Weapons)EditorGUILayout.EnumPopup("Weapons", _skinItem.Weapon);
        _skinItem.SkinRarity = (SkinRarity)EditorGUILayout.EnumPopup("Skin Rarity", _skinItem.SkinRarity);

        int weaponIndex = (int)_skinItem.Weapon;

        if (weaponIndex >= 0 && weaponIndex <= 34)
        {
            _skinItem.SkinType = SkinType.Gun;
        }
        else if (weaponIndex >= 35 && weaponIndex <= 53)
        {
            _skinItem.SkinType = SkinType.Knife;
        }
        else if (weaponIndex >= 54)
        {
            _skinItem.SkinType = SkinType.Gloves;
        }

        Rect rarityRect = GUILayoutUtility.GetRect(3, 3);
        EditorGUI.DrawRect(rarityRect, RarityColors.GetSkinColor(_skinItem.SkinRarity));

        if (GUI.changed)
        {
            EditorUtility.SetDirty(_skinItem);
        }
        serializedObject.ApplyModifiedProperties();
    }
}