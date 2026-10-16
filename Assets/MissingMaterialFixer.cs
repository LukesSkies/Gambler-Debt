using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MissingMaterialFixer : EditorWindow
{
    private Material defaultHdrpMaterial;

    [MenuItem("Tools/HDRP/Fix Empty Materials")]
    public static void ShowWindow()
    {
        GetWindow<MissingMaterialFixer>("Material Fixer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Assign Default HDRP Material", EditorStyles.boldLabel);

        // Let user assign their preferred HDRP standard material
        defaultHdrpMaterial = (Material)EditorGUILayout.ObjectField(
            "Default HDRP Material",
            defaultHdrpMaterial,
            typeof(Material),
            false
        );

        GUILayout.Space(15);

        if (GUILayout.Button("Scan & Fix Active Scene"))
        {
            if (defaultHdrpMaterial == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a default HDRP material first!", "OK");
                return;
            }

            FixEmptySceneMaterials();
        }
    }

    private void FixEmptySceneMaterials()
    {
        // Find every single MeshRenderer in the active scene, including inactive GameObjects
        MeshRenderer[] renderers = FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int fixedCount = 0;

        foreach (MeshRenderer renderer in renderers)
        {
            // Use .sharedMaterials to avoid accidentally creating instanced runtime material duplicates in the editor
            Material[] mats = renderer.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < mats.Length; i++)
            {
                // Checks for empty material slots (null)
                if (mats[i] == null)
                {
                    mats[i] = defaultHdrpMaterial;
                    changed = true;
                }
            }

            if (changed)
            {
                // Register the modification to Unity's Undo history system so you can Ctrl+Z if needed
                Undo.RecordObject(renderer, "Assigned Default HDRP Material");
                renderer.sharedMaterials = mats;

                // Marks scene dirty so Unity knows there are unsaved changes
                EditorUtility.SetDirty(renderer.gameObject);
                fixedCount++;
            }
        }

        EditorUtility.DisplayDialog("Success", $"Scan complete! Fixed empty slots on {fixedCount} Mesh Renderers.", "Awesome");
    }
}