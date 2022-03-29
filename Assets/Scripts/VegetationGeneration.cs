using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class VegetationGeneration : MonoBehaviour
{
    [System.Serializable]
    public struct Rule
    {
        public char character;
        public string application;
    }

    public static char[] rotationChar = {'+', '-', '\\', '/', '|', '&', '∧'};


    public VegetationPreset vegetationPreset;

    public int nbIteration;

    [Header("Grammar")] [SerializeField] public Rule[] rules;
    public string startSentence;

    [Header("Mesh Options")] public bool orientation3D;
    public float angleTheta = 90;
    public float lengthPart = 2;
    public float lengthPolygon;
    public float radiusBranch = 0.3f;
    public int nbFacePerCylinder = 4;
    public float decrementRadiusMultiplier = 0.9f;

    [Header("Colors")] public Color32[] colors;

    [Header("Other options")] public float timeSpawnBranch = 0.2f;
    public GameObject meshHandlerPrefab;

    [HideInInspector]
    public MeshGestion actualMesh;

    private Transform treeParents;

    public void GenerateVegetation(bool newMesh = false)
    {
        if (newMesh || !actualMesh)
        {
            if (actualMesh)
            {
                if (!treeParents)
                    treeParents = Instantiate(new GameObject("TreeParent")).transform;
                actualMesh.transform.parent = treeParents;
            }

            if (vegetationPreset)
                meshHandlerPrefab.name = vegetationPreset.name;
            actualMesh = Instantiate(meshHandlerPrefab, transform.position, Quaternion.identity, this.transform).GetComponent<MeshGestion>();
        }

        var dicoRules = BuildDictionnary(rules);
        string grammarApplied = GrammarInterpretation.ApplyGrammar(dicoRules, startSentence, nbIteration);
        Debug.Log(grammarApplied);
        actualMesh.GenerateMeshFromSentence(grammarApplied, lengthPart, angleTheta, radiusBranch,
            timeSpawnBranch, nbFacePerCylinder, orientation3D, decrementRadiusMultiplier, colors, lengthPolygon);
    }

    Dictionary<char, string> BuildDictionnary(Rule[] _rules)
    {
        Dictionary<char, string> res = new Dictionary<char, string>();
        for (int i = 0; i < _rules.Length; i++)
        {
            res.Add(_rules[i].character, _rules[i].application);
        }

        return res;
    }

}