using UnityEditor;
using UnityEngine;
using UnityEngine.Windows;

[CustomEditor(typeof(VegetationGeneration))]
public class VegetationGenerationEditor : Editor
{
    private VegetationGeneration script;
    private string meshName = "";
    private void OnEnable()
    {
        script = target as VegetationGeneration;
    }

    public override void OnInspectorGUI()
    {
        if (script.actualMesh == null && GUILayout.Button("Generate mesh"))
        {
            script.GenerateVegetation(true);
        }
        else if (script.actualMesh != null && GUILayout.Button("Re-generate mesh"))
        {
            script.GenerateVegetation(false);
        }

        if (script.actualMesh != null)
        {
            GUILayout.Space(10);
            GUILayout.Label("Mesh name:");

            meshName = GUILayout.TextField(meshName, 25);
            if (GUILayout.Button("Set object as static"))
            {
                SaveMesh(meshName);
            }
            if (GUILayout.Button("Save as prefab"))
            {
                SaveMeshAsPrefab(meshName);
            }
        }


        if (script.vegetationPreset != null)
        {
            GUILayout.Space(10);
            if (GUILayout.Button("Load Preset"))
                LoadPreset();
            if (GUILayout.Button("Override Preset Values"))
                SavePreset();
        }

        base.OnInspectorGUI();
    }

    // Call to apply the preset -> to change value in the inspector
    private void LoadPreset()
    {
        script.nbIteration = script.vegetationPreset.nbIteration;
        script.rules = script.vegetationPreset.rules;
        script.defines = script.vegetationPreset.defines;
        script.axiom = script.vegetationPreset.axiom;
        script.orientation3D = script.vegetationPreset.orientation3D;
        script.angleTheta = script.vegetationPreset.angleTheta;
        script.lengthPart = script.vegetationPreset.lengthPart;
        script.lengthPolygon = script.vegetationPreset.lengthPolygon;
        script.radiusBranch = script.vegetationPreset.radiusBranch;
        script.nbFacePerCylinder = script.vegetationPreset.nbFacePerCylinder;
        script.decrementRadiusMultiplier = script.vegetationPreset.decrementRadiusMultiplier;
        script.colors = script.vegetationPreset.colors;
        script.timeSpawnBranch = script.vegetationPreset.timeSpawnBranch;
    }

    // Call to save the actual preset to the scriptable Object
    private void SavePreset()
    {
        script.vegetationPreset.nbIteration = script.nbIteration;
        script.vegetationPreset.rules = script.rules;
        script.vegetationPreset.axiom = script.axiom;
        script.vegetationPreset.defines = script.defines;
        script.vegetationPreset.orientation3D = script.orientation3D;
        script.vegetationPreset.angleTheta = script.angleTheta;
        script.vegetationPreset.lengthPart = script.lengthPart;
        script.vegetationPreset.lengthPolygon = script.lengthPolygon;
        script.vegetationPreset.radiusBranch = script.radiusBranch;
        script.vegetationPreset.nbFacePerCylinder = script.nbFacePerCylinder;
        script.vegetationPreset.decrementRadiusMultiplier = script.decrementRadiusMultiplier;
        script.vegetationPreset.colors = script.colors;
        script.vegetationPreset.timeSpawnBranch = script.timeSpawnBranch;
        EditorUtility.SetDirty(script.vegetationPreset);
    }

    // Save the mesh generated to keep it for build
    private void SaveMesh(string meshName)
    {
        GameObject go = script.actualMesh.gameObject;
        if (meshName == "")
            meshName = go.name;

        if (!Directory.Exists("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");

        if (!Directory.Exists("Assets/Resources/Meshes"))
            AssetDatabase.CreateFolder("Resources", "Meshes");

        // Save the mesh and apply it to the object
        Mesh mesh = script.actualMesh.GetComponent<MeshFilter>().sharedMesh;
        string meshPath = "Assets/Resources/Meshes/" + meshName + ".asset";
        meshPath = AssetDatabase.GenerateUniqueAssetPath(meshPath);
        AssetDatabase.CreateAsset(mesh, meshPath);
        AssetDatabase.Refresh();

        mesh = Resources.Load<Mesh>("Meshes/" + meshName);
        if (!mesh)
            Debug.LogWarning("Can't load mesh back in prefab. Prefab won't be linked with mesh");
        script.actualMesh.GetComponent<MeshFilter>().sharedMesh = mesh;
        script.actualMesh.MakeTreeStatic();
        script.GenerateVegetation(true);
    }

    // Save a prefab in the asset database to re-use later
    private void SaveMeshAsPrefab(string meshName)
    {
        GameObject go = script.actualMesh.gameObject;
        if (meshName == "")
            meshName = go.name;

        if (!Directory.Exists("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        if (!Directory.Exists("Assets/Prefabs/LSystem"))
            AssetDatabase.CreateFolder("Prefabs", "LSystem");

        SaveMesh(meshName);

        // Save the prefab
        string localPath = "Assets/Prefabs/LSystem/" + meshName + ".prefab";
        localPath = AssetDatabase.GenerateUniqueAssetPath(localPath);

        PrefabUtility.SaveAsPrefabAssetAndConnect(go, localPath, InteractionMode.UserAction);
        Debug.Log("Prefab saved here: " + localPath);

    }


}