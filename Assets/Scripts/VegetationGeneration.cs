using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[ExecuteInEditMode]
public class VegetationGeneration : MonoBehaviour
{
    public bool generateVegetation;

    public int nbIteration;

    [System.Serializable]
    public struct Rule
    {
        public char character;
        public string application;
    }

    [SerializeField]
    public Rule[] rules;

    public float angleTheta = 90;
    public float lengthPart = 2;
    public string startSentence;
    public float radiusBranch = 0.3f;
    public float timeSpawnBranch = 0.2f;

    // Update is called once per frame
    void Update()
    {
        #if UNITY_EDITOR
        if (generateVegetation)
        {
            var dicoRules = BuildDictionnary(rules);
            generateVegetation = false;
            string grammarApplied = GrammarInterpretation.ApplyGrammar(dicoRules, startSentence, nbIteration);
            Debug.Log(grammarApplied);
            MeshGestion.Instance.GenerateMeshFromSentence(grammarApplied, lengthPart, angleTheta, radiusBranch, timeSpawnBranch);
        }
        #endif
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