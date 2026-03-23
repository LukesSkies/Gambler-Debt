using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CaseItem))]
public class CaseItemEditor : Editor
{
    private CaseItem _caseItem;

    private void OnEnable()
    {
        _caseItem = (CaseItem)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.LabelField($"Item", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        _caseItem.Id = EditorGUILayout.IntField("ID", _caseItem.Id);
        if (GUILayout.Button("Define", GUILayout.Width(100)))
        {
            _caseItem.Id = ItemRegistry.GetFreeID(_caseItem);
        }
        EditorGUILayout.EndHorizontal();

        _caseItem.Name = EditorGUILayout.TextField("Name", _caseItem.Name);
        _caseItem.Icon = (Sprite)EditorGUILayout.ObjectField("Icon", _caseItem.Icon, typeof(Sprite), false);

        EditorGUILayout.LabelField($"Key", EditorStyles.boldLabel);
        KeyItem newKey = (KeyItem)EditorGUILayout.ObjectField(_caseItem.RelatedKey, typeof(KeyItem), false);
        if (newKey != _caseItem.RelatedKey)
        {
            _caseItem.RelatedKey = newKey;
            EditorUtility.SetDirty(_caseItem);
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Drops", EditorStyles.boldLabel);

        if (GUILayout.Button("Sort"))
        {
            _caseItem.Drops.Sort((a, b) =>
            {
                if (a is SkinItem skinA && b is SkinItem skinB)
                {
                    return skinB.SkinRarity.CompareTo(skinA.SkinRarity);
                }

                if (a is SkinItem) return -1;
                if (b is SkinItem) return 1;

                return 0;
            });

            EditorUtility.SetDirty(_caseItem);
        }

        Rect blocksRect = EditorGUILayout.BeginVertical("box");
        for (int i = 0; i < _caseItem.Drops.Count; i++)
        {
            Rect blockRect = EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();

            SkinItem drop = _caseItem.Drops[i];

            if (drop != null)
            {
                Rect lineRect = new Rect(blockRect.x, blockRect.y, 5f, blockRect.height);
                EditorGUI.DrawRect(lineRect, RarityColors.GetSkinColor(drop.SkinRarity));
            }

            GUILayout.Space(5);

            if (GUILayout.Button("↑", GUILayout.Width(25)) && i > 0)
            {
                (_caseItem.Drops[i - 1], _caseItem.Drops[i]) = (_caseItem.Drops[i], _caseItem.Drops[i - 1]);
                EditorUtility.SetDirty(_caseItem);
            }

            if (GUILayout.Button("↓", GUILayout.Width(25)) && i < _caseItem.Drops.Count - 1)
            {
                (_caseItem.Drops[i + 1], _caseItem.Drops[i]) = (_caseItem.Drops[i], _caseItem.Drops[i + 1]);
                EditorUtility.SetDirty(_caseItem);
            }

            SkinItem newDrop = (SkinItem)EditorGUILayout.ObjectField(drop, typeof(SkinItem), false);
            if (newDrop != drop)
            {
                _caseItem.Drops[i] = newDrop;
                EditorUtility.SetDirty(_caseItem);
            }

            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                _caseItem.Drops.Remove(drop);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndVertical();

        if (GUILayout.Button("Add Drop"))
        {
            _caseItem.Drops.Add(null);
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(_caseItem);
        }
        serializedObject.ApplyModifiedProperties();
    }
}
