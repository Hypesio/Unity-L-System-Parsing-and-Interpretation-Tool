using System;
using UnityEngine;
using Rule = GrammarInterpretation.Rule;
using Define = GrammarInterpretation.Define;
[ExecuteInEditMode]
public class VegetationGeneration : MonoBehaviour
{
    public static char[] rotationChar = {'+', '-', '\\', '/', '|', '&', '∧', '^', '$'};

    public VegetationPreset vegetationPreset;

    public int nbIteration;

    [Header("Grammar")] public string axiom;
    [SerializeField] public Rule[] rules;
    public Define[] defines;

    [Header("Mesh Options")] public bool orientation3D;
    [Tooltip("The mesh will be made of plane. Face length = radius")]
    public bool flatShape;
    public float angleTheta = 90;
    public float lengthPart = 2;
    public float lengthPolygon;
    public float radiusBranch = 0.3f;
    public int nbFacePerCylinder = 4;
    public float decrementRadiusMultiplier = 0.9f;

    [Header("Colors")] public Color32[] colors;

    [Header("Other options")] public float timeSpawnBranch = 0.2f;
    public GameObject meshHandlerPrefab;
    public MeshGestion actualMesh;

    private Transform treeParents;

    public void GenerateVegetation(bool newMesh = false)
    {
        PrepareMesh(newMesh);
        string grammarApplied = ApplyGrammar(axiom, nbIteration);
        GenerateMesh(grammarApplied);
    }

    public string ApplyGrammar(string _axiom, int _nbIteration)
    {
       return GrammarInterpretation.ApplyGrammar(rules, defines, _axiom, _nbIteration);
    }

    public Mesh GenerateMesh(string sentence)
    {
        PrepareMesh();
        actualMesh.GenerateMeshFromSentence(sentence, lengthPart, angleTheta, radiusBranch,
            timeSpawnBranch, nbFacePerCylinder, orientation3D, decrementRadiusMultiplier, colors, lengthPolygon, flatShape);
        return actualMesh.meshGenerated;
    }

    private void PrepareMesh(bool newMesh = false)
    {
        if (newMesh || !actualMesh)
        {
            if (actualMesh)
            {
                if (!treeParents)
                {
                    treeParents = GameObject.Find("TreeParent")?.transform;
                    if (!treeParents)
                        treeParents = GameObject.Find("TreeParent(Clone)")?.transform;
                    if (!treeParents)
                        treeParents = Instantiate(new GameObject("TreeParent")).transform;
                }

                actualMesh.transform.parent = treeParents;
            }

            if (vegetationPreset)
                meshHandlerPrefab.name = vegetationPreset.name;
            actualMesh = Instantiate(meshHandlerPrefab, transform.position, Quaternion.identity, this.transform).GetComponent<MeshGestion>();
        }
    }
}