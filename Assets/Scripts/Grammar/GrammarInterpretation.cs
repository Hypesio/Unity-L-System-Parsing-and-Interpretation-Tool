using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.VisualScripting;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;
using Random = UnityEngine.Random;
using ExpressionNode = ExpressionParsing.ExpressionNode;

[ExecuteInEditMode]
public class GrammarInterpretation : MonoBehaviour
{
    [System.Serializable]
    public struct Rule
    {
        public string predecessor;
        public string application;

        public Rule(string pred, string app)
        {
            predecessor = pred;
            application = app;
        }
    }

    [System.Serializable]
    public struct Define
    {
        public string word;
        public float value;

        public Define(string _word, float _value)
        {
            word = _word;
            value = _value;
        }
    }

    public class RuleExtended
    {
        public char elementToReplace;
        public string application;
        public float probability;

        public string leftContext;
        public string rightContext;

        public List<string> parameters;
        public ExpressionNode condition = null;

        public RuleExtended(char elementToReplace = ' ', string application = "", float probability = 1, string leftContext = null, string rightContext = null, List<string> parameters = null, ExpressionNode condition = null)
        {
            this.elementToReplace = elementToReplace;
            this.application = application;
            this.probability = probability;
            this.leftContext = leftContext;
            this.rightContext = rightContext;
            this.parameters = parameters;
            this.condition = condition;
        }
    }

    public static string ApplyGrammar(Rule[] rules, Define[] defines, string sentence, int nbIterations)
    {
        if (nbIterations == 0)
            return sentence;

        RuleExtended[] rulesExtended = ExtendRules(rules, defines);

        //PrintRules(rulesExtended);

        string result = ApplyGrammar(rulesExtended, defines, sentence, nbIterations);
        Debug.Log("[Grammar result] " + result);
        return result;
    }

    private static string ApplyGrammar(RuleExtended[] rules, Define[] defines, string sentence, int nbIterations)
    {
        if (nbIterations == 0)
            return sentence;

        string newSentence = "";

        for (int i = 0; i < sentence.Length; i++)
        {
            char c = sentence[i];
            string toAdd = "";
            if (i < sentence.Length - 1 && sentence[i + 1] == '(')
            {
                i += 2;
                toAdd = ApplyParametricalRule(rules, defines, sentence, c, ref i);
            }
            else
                toAdd = ApplyRule(c, rules, sentence, i, null);

            newSentence += toAdd;
        }

        string result = ApplyGrammar(rules, defines, newSentence, --nbIterations);
        return result;
    }

    // Prepare rules to be easier to use latter
    private static RuleExtended[] ExtendRules(Rule[] rules, Define[] defines)
    {
        RuleExtended[] extendeds = new RuleExtended[rules.Length];
        for (int i = 0; i < rules.Length; i++)
        {
            extendeds[i] = ExtendRule(rules[i], defines);
        }

        return extendeds;
    }

    // Prepare rule to be easier to use latter
    public static RuleExtended ExtendRule(Rule rule, Define[] defines)
    {
        RuleExtended extended = new RuleExtended();
        string predecessor = Regex.Replace(rule.predecessor, " ", "");
        extended.application = Regex.Replace(rule.application, " ", "");
        extended.application = ReplaceDefine(defines, extended.application);

        // Split if it is a parametrical rule
        string[] res = predecessor.Split(':');
        string pred = res[0];

        if (res.Length == 2)
        {
            if (res[1] == "*")
                extended.condition = new ExpressionNode(1);
            else
                extended.condition = ExpressionParsing.BuildExpressionTree(res[1]);
        }
        else if (res.Length > 2)
            throw new Exception("[Grammar] Several ':' in rule: " + rule.predecessor);

        extended.parameters = new List<string>();
        ParseParameters(extended.parameters, pred);
        pred = RemoveParameter(pred); // remove parameters to easily context


        // Simple context sensitive (don't take branch into account)
        string actualWord = "";
        int index = 0;
        while (index < pred.Length)
        {
            actualWord = GetWordUntilChar(pred, ref index, new[] {'<', '['});
            if (index >= pred.Length)
                break;
            char actualC = pred[index];
            index++;
            if (actualC == '<')
            {
                if (extended.leftContext != null)
                    Debug.LogWarning("[Grammar] '<' present twice in rule: " + rule.predecessor);
                extended.leftContext = actualWord;

                string centerWord = GetWordUntilChar(pred, ref index, new []{'>', '['});
                if (pred[index] == '>')
                {
                    index++;
                    extended.rightContext = GetWordUntilChar(pred, ref index, new[] {'['});
                }
                extended.elementToReplace = centerWord[0];
            }
            else if (actualC == '[')
            {
                if (extended.elementToReplace == ' ')
                    extended.elementToReplace = actualWord[0];
                else
                    extended.rightContext = actualWord;

                string wordToParse = GetWordUntilChar(pred, ref index, new[] {']'});
                if (pred[index] != ']')
                    Debug.LogWarning("[Grammar] Missing closing bracket in predecessor: " + rule.predecessor);

                if (float.TryParse(wordToParse, NumberStyles.Float, CultureInfo.InvariantCulture, out float number))
                    extended.probability = number;
                else
                    throw new Exception("[Grammar] Invalid probability in rule: " + rule.predecessor);

            }
        }

        if (extended.elementToReplace == ' ')
        {
            extended.elementToReplace = actualWord[0];
            if (actualWord.Length > 1)
                Debug.LogWarning("[Grammar] Error in parsing of rule: " + rule.predecessor);
        }

        return extended;
    }

    // Return the production of the corresponding rule
    public static string ApplyRule(char letter, RuleExtended[] rules, string sentence, int index, float[] parametersValue, string original = "")
    {
        // Step 1: Find all rules corresponding to the letter
        List<RuleExtended> letterRule = new List<RuleExtended>();
        foreach (var r in rules)
        {
            if (r.elementToReplace == letter)
                letterRule.Add(r);
        }

        if (letterRule.Count == 0)
        {
            return letter + original;
        }

        // Step 2: Check the context
        for(int i = 0; i < letterRule.Count; i ++)
        {
            if (!ValidContext(letterRule[i], sentence, index))
            {
                letterRule.RemoveAt(i);
                i--;
            }
        }

        if (letterRule.Count == 0)
            return letter + original;

        // Step 3: Check the condition
        for (int i = 0; i < letterRule.Count; i++)
        {
            if (letterRule[i].condition == null || letterRule[i].parameters.Count == 0)
                continue;
            if (!BuildDefineArray(letterRule[i].parameters.ToArray(), parametersValue, out Define[] def))
            {
                letterRule.RemoveAt(i);
                i--;
                continue;
            }
            if (Mathf.Approximately(ExpressionExecution.Execute(letterRule[i].condition, def), 0))
            {
                letterRule.RemoveAt(i);
                i--;
            }
        }

        if (letterRule.Count == 0)
            return letter + original;

        // Step 4: Choose the good rule using proba
        RuleExtended choosenRule = null;
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
            {
                choosenRule = letterRule[i];
                break;
            }
        }

        if (choosenRule == null)
            return letter + original;

        // Step 5: Prepare define and put them in application
        BuildDefineArray(choosenRule.parameters.ToArray(), parametersValue, out Define[] defines);
        string res = ReplaceDefine(defines, choosenRule.application);
        res = ExecuteExpressionInString(res);

        return res;
    }

    // Check if the surrounding context of a letter is valid
    private static bool ValidContext(RuleExtended rule, string sentence, int index)
    {
        if (!String.IsNullOrEmpty(rule.leftContext))
        {
            int lenContext = rule.leftContext.Length;
            if (index - lenContext < 0)
                return false;

            string possibleLeftContext = SkipParams(sentence, index - 1, -lenContext);
            if (rule.leftContext != possibleLeftContext)
                return false;
        }

        if (!String.IsNullOrEmpty(rule.rightContext))
        {
            int lenContext = rule.rightContext.Length;
            if (index - lenContext > sentence.Length)
                return false;
            string possibleRightContext = SkipParams(sentence, index + 1, lenContext);
            if (rule.rightContext != possibleRightContext)
                return false;
        }

        return true;
    }

    // Extract parameters in string like 'A(t, u)' or 'A(t,v)B(a, t)'. Return false if error
    public static bool ParseParameters(List<string> parameters, string str)
    {
        int index = 0;
        while (index < str.Length)
        {
            GetWordUntilChar(str, ref index, new []{'('});
            if (index == str.Length)
                return true;
            index++;
            string paramGroup = GetWordUntilChar(str, ref index, new[] {')'});
            index++;
            string[] paramStr = paramGroup.Split(',');
            parameters.AddRange(paramStr);
        }

        return true;
    }

    // Remove parameter and operations in string like 'A((a+1), b, c)BF' => 'ABF'
    public static string RemoveParameter(string str)
    {
        int index = 0;
        string result = "";
        while (index < str.Length)
        {
            result += GetWordUntilChar(str, ref index, new []{'('});
            index++;
            int parenthDepth = 1;
            while (parenthDepth >= 1 && index < str.Length) // Handle multiple depth parenthesis
            {
                GetWordUntilChar(str, ref index, new[] {')', '('});
                if (index < str.Length && str[index] == '(')
                    parenthDepth += 1;
                else if (index < str.Length && str[index] == ')')
                    parenthDepth -= 1;
                else
                    throw new Exception("[Grammar] Missing closing parenthesis in: " + str);
                index++;
            }
        }

        return result;
    }

    private static string ApplyParametricalRule(RuleExtended[] rules, Define[] define, string sentence, char letterRule, ref int index)
    {

        string[] splited = sentence.Substring(index).Split(')');
        string original = "(" + splited[0] + ")";
        if (splited.Length == 1 && sentence[sentence.Length-1] != ')') // No closing parenthesis at the end
            Debug.LogWarning("[Grammar] Missing parenthesis on axiom or application of a rule.");

        index += splited[0].Length;
        string[] strExpressions = splited[0].Split(',');
        float[] parameters = new float[strExpressions.Length];
        int i = 0;
        foreach (var str in strExpressions)
        {
            ExpressionNode exp = ExpressionParsing.BuildExpressionTree(str);
            if (exp == null)
                throw new Exception("[Grammar] Error while parsing expressions in sentence: " + sentence);

            parameters[i] = ExpressionExecution.Execute(exp, define);
            i++;
        }

        return ApplyRule(letterRule, rules, sentence, index, parameters, original);
    }

    public static string ReplaceDefine(Define[] defines, string successor)
    {
        int index = 0;
        string res = "";
        while (index < successor.Length)
        {
            res += GetWordUntilChar(successor, ref index, new []{'('});
            if (index < successor.Length)
                res += '(';

            index++;
            int parentDepth = 1;
            while (index < successor.Length && parentDepth > 0)
            {
                char charStop = ' ';
                char[] stoppers = ExpressionParsing.allOperators.Concat(new char[] {'(', ')', ','}).ToArray();
                string potentialWord = GetWordUntilChar(successor, ref index, stoppers);
                if (index < successor.Length)
                    charStop = successor[index];

                string defined = SearchDefinedElement(defines, potentialWord);

                if (defined == "")
                    res += potentialWord;
                else
                    res += defined;

                if (index < successor.Length)
                    res += charStop;

                if (index < successor.Length && successor[index] == '(')
                    parentDepth++;
                else if (index < successor.Length && successor[index] == ')')
                    parentDepth++;
                index++;
            }
        }

        return res;
    }

    private static string SearchDefinedElement(Define[] defines, string word)
    {
        foreach (var d in defines)
        {
            if (d.word == word)
                return d.value.ToString(CultureInfo.InvariantCulture);
        }

        return "";
    }

    public static string GetWordUntilChar(string sentence, ref int index, char[] stoppers, bool backward = false)
    {
        string word = "";
        while (index >= 0 && index < sentence.Length && !stoppers.Contains(sentence[index]))
        {
            word += sentence[index];
            if (backward)
                index--;
            else
                index++;
        }

        if (backward) // Reverse the string
        {
            string res = String.Empty;
            foreach (var c in word)
            {
                res = c + res;
            }

            return res;
        }

        return word;
    }

    // Create a string of length 'lengthToAdd'. Ignore parameters in the string
    private static string SkipParams(string sentence, int startIndex, int lengthToAdd)
    {
        (char, char) forward = ('(', ')');
        (char, char) backward = (')', '(');
        (char, char) toUse = lengthToAdd < 0 ? backward : forward;
        int toAdd = lengthToAdd < 0 ? -1 : 1;

        int index = startIndex;
        string res = "";
        while (index != startIndex + lengthToAdd && index >= 0 && index < sentence.Length)
        {
            if (sentence[index] == toUse.Item1) // We enter a parameter (parenthesis)
            {
                int parenthDepth = 1;
                index += toAdd;
                while (parenthDepth >= 1 && index < sentence.Length && index >= 0) // Handle multiple depth parenthesis
                {
                    GetWordUntilChar(sentence, ref index, new[] {toUse.Item1, toUse.Item2}, lengthToAdd < 0);
                    if (index >= 0 && index < sentence.Length && sentence[index] == toUse.Item1)
                        parenthDepth += 1;
                    else if (index >= 0 && index < sentence.Length && sentence[index] == toUse.Item2)
                        parenthDepth -= 1;
                    else
                        throw new Exception("[Grammar] Missing closing parenthesis in: " + sentence);
                    index += toAdd;
                }
            }

            if (lengthToAdd < 0)
                res = sentence[index] + res;
            else
                res += sentence[index];

            index += toAdd;
        }

        return res;
    }

    private static bool BuildDefineArray(string[] words, float[] values, out Define[] defines)
    {

        defines = new Define[words.Length];

        if (words == null || values == null)
            return true;

        if (words.Length != values.Length)
            return false;

        for (int i = 0; i < words.Length; i++)
        {
            defines[i] = new Define(words[i], values[i]);
        }

        return true;
    }

    private static string ExecuteExpressionInString(string sentence)
    {
        int index = 0;
        string res = String.Empty;
        while (index < sentence.Length)
        {
            res += GetWordUntilChar(sentence, ref index, new char[]{'('});
            if (index >= sentence.Length)
                break;

            res += sentence[index];
            index++;
            int depth = 1;
            string toExecute = "";
            while (depth >= 1)
            {
                toExecute += GetWordUntilChar(sentence, ref index, new char[]{'(', ',', ')'});

                if (index >= sentence.Length)
                    break;

                if (depth == 1 && (new[] {')', ','}.Contains(sentence[index])))
                {
                    ExpressionNode exp = ExpressionParsing.BuildExpressionTree(toExecute);
                    res += ExpressionExecution.Execute(exp, null).ToString(CultureInfo.InvariantCulture);
                    toExecute = "";
                }

                if (sentence[index] == ')')
                {
                    depth--;
                }
                else if (sentence[index] == '(')
                {
                    depth++;
                }

                res += sentence[index];

                index++;
            }
        }

        return res;
    }

    public static void PrintRules(RuleExtended[] rules)
    {
        foreach (var r in rules)
        {
            Debug.Log("[Grammar] Rule details: " + r.elementToReplace + " " + JsonUtility.ToJson(r, true));
        }
    }
}