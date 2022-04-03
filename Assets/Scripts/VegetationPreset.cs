using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "VegetationPreset", menuName = "ScriptableObjects/VegetationPreset", order = 1)]
public class VegetationPreset : ScriptableObject
{
    public int nbIteration;

    [Header("Grammar")] public string axiom;
    [SerializeField] public GrammarInterpretation.Rule[] rules;
    public GrammarInterpretation.Define[] defines;

    [Header("Mesh Options")] public bool orientation3D;
    public float angleTheta = 90;
    public float lengthPart = 2;
    public float lengthPolygon;
    public float radiusBranch = 0.3f;
    public int nbFacePerCylinder = 4;
    public float decrementRadiusMultiplier = 0.9f;

    [Header("Colors")] public Color32[] colors;

    [Header("Other options")] public float timeSpawnBranch = 0.2f;
}