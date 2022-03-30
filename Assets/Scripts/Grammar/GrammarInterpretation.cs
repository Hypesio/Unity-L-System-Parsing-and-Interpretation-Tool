using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;
using Random = UnityEngine.Random;
using Rule = VegetationGeneration.Rule;

[ExecuteInEditMode]
public class GrammarInterpretation : MonoBehaviour
{
    public class RuleExtended
    {
        public char elementToReplace = ' ';
        public string application;
        public float probability = 1;
        public string leftContext;
        public string rightContext;
    }

    public static string ApplyGrammar(Rule[] rules, string sentence, int nbIterations)
    {
        if (nbIterations == 0)
            return sentence;

        RuleExtended[] rulesExtended = ExtendRules(rules);

        PrintRules(rulesExtended);

        string result = ApplyGrammar(rulesExtended, sentence, nbIterations);
        Debug.Log("[Grammar result] " + result);
        return result;
    }

    private static string ApplyGrammar(RuleExtended[] rules, string sentence, int nbIterations)
    {
        if (nbIterations == 0)
            return sentence;

        string newSentence = "";

        for(int i = 0; i < sentence.Length; i ++)
        {
            char c = sentence[i];
            string toAdd = ApplyRule(c, rules, sentence, i);
            newSentence += toAdd;
        }

        string result = ApplyGrammar(rules, newSentence, --nbIterations);
        return result;
    }

    // Prepare rules to be easier to use latter
    private static RuleExtended[] ExtendRules(Rule[] rules)
    {
        RuleExtended[] extendeds = new RuleExtended[rules.Length];
        for (int i = 0; i < rules.Length; i++)
        {
            extendeds[i] = ExtendRule(rules[i]);
        }

        return extendeds;
    }

    // Prepare rule to be easier to use latter
    private static RuleExtended ExtendRule(Rule rule)
    {
        string actualString = "";
        RuleExtended extended = new RuleExtended();
        string prod = Regex.Replace(rule.prod, " ", "");
        extended.application = Regex.Replace(rule.application, " ", "");
        bool inProb = false;
        bool contextualRight = false;

        // Not parametrical grammar
        for (int i = 0; i < prod.Length; i++)
        {
            char actualC = prod[i];
            if (actualC == '<')
            {
                if (extended.leftContext != null)
                    Debug.LogWarning("[Grammar] '<' present twice in rule: " + rule.prod);
                extended.leftContext = actualString;
                actualString = "";
            }
            else if (actualC == '>')
            {
                if (extended.elementToReplace != ' ')
                    Debug.LogWarning("[Grammar] '>' present twice in rule: " + rule.prod);
                extended.elementToReplace = prod[i-1];
                actualString = "";
                contextualRight = true;
            }
            else if (actualC == '(')
            {
                if (extended.elementToReplace == ' ')
                    extended.elementToReplace = prod[i-1];
                else
                {
                    extended.rightContext = actualString;
                    contextualRight = false;
                }

                inProb = true;
                actualString = "";
            }
            else if (actualC == ')')
            {
                if (!inProb)
                    Debug.LogWarning("[Grammar] Alone ')' in rule: " + rule.prod);
                else
                {
                    extended.probability = float.Parse(actualString, CultureInfo.InvariantCulture);
                }
            }
            else
                actualString += actualC;
        }

        if (contextualRight)
            extended.rightContext = actualString;

        if (extended.elementToReplace == ' ')
        {
            extended.elementToReplace = actualString[0];
            if (actualString.Length > 1)
                Debug.LogWarning("[Grammar] Error in parsing of rule: " + rule.prod);
        }

        return extended;
    }

    // Return the production of the corresponding rule
    public static string ApplyRule(char letter, RuleExtended[] rules, string sentence, int index)
    {
        // Step 1 : Find all rules corresponding to the letter
        List<RuleExtended> letterRule = new List<RuleExtended>();
        foreach (var r in rules)
        {
            if (r.elementToReplace == letter)
                letterRule.Add(r);
        }

        if (letterRule.Count == 0)
            return letter + "";

        // Step 2 : check the context
        for(int i = 0; i < letterRule.Count; i ++)
        {
            if (!ValidContext(letterRule[i], sentence, index))
            {
                letterRule.RemoveAt(i);
                i--;
            }
        }

        if (letterRule.Count == 1)
            return letterRule[0].application;

        if (letterRule.Count == 0)
            return letter + "";

        // Step 3 : Choose the good rule using proba
        float[] cumulativeArray = new float[letterRule.Count];
        cumulativeArray[0] = letterRule[0].probability;
        for(int i = 1; i < letterRule.Count; i ++)
        {
            cumulativeArray[i] = cumulativeArray[i - 1] + letterRule[i].probability;
        }

        float ran = Random.value * cumulativeArray.Last();
        for (int i = 0; i < letterRule.Count; i++)
        {
            if (cumulativeArray[i] > ran)
                return letterRule[i].application;
        }

        return letter + "";
    }

    // Check if the surrounding context of a letter is valid
    private static bool ValidContext(RuleExtended rule, string sentence, int index)
    {
        if (!String.IsNullOrEmpty(rule.leftContext))
        {
            int lenContext = rule.leftContext.Length;
            int indexStartContext = index - lenContext;
            if (indexStartContext < 0 ||
                String.Compare(rule.leftContext, 0, sentence, indexStartContext, lenContext) != 0)
                return false;
        }

        if (!String.IsNullOrEmpty(rule.rightContext))
        {
            int lenContext = rule.rightContext.Length;
            if (index + 1 >= sentence.Length ||
                String.Compare(rule.rightContext, 0, sentence, index + 1, lenContext) != 0)
                return false;
        }

        return true;
    }

    private static void PrintRules(RuleExtended[] rules)
    {
        foreach (var r in rules)
        {
            Debug.Log("[Grammar] Rule details: " + r.elementToReplace + " " + JsonUtility.ToJson(r, true));
        }
    }
}