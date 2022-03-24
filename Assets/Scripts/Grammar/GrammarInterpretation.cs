using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class GrammarInterpretation : MonoBehaviour
{
    public static string ApplyGrammar( Dictionary<char, string> rules, string sentence, int nbIterations)
    {
        if (nbIterations == 0)
            return sentence;

        string newSentence = "";
        foreach (var c in sentence)
        {
            string toAdd = ApplyRule(c, rules);
            newSentence += toAdd;
        }

        string result = ApplyGrammar(rules, newSentence, --nbIterations);
        return result;
    }

    public static string ApplyRule(char letter, Dictionary<char, string> rules)
    {
        if (rules.TryGetValue(letter, out string ruleApplied))
        {
            return ruleApplied;
        }
        return letter + "";
    }
}