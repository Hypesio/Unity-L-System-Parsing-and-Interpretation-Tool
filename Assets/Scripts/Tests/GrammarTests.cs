using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Define = GrammarInterpretation.Define;
using Rule = GrammarInterpretation.Rule;
using RuleExtended = GrammarInterpretation.RuleExtended;

[ExecuteInEditMode]
public class GrammarTests : MonoBehaviour
{
    public bool test;
    // Update is called once per frame
    void Update()
    {
        #if UNITY_EDITOR
        if (test)
        {
            test = false;
            LaunchTest();
        }
        #endif
    }

    private void LaunchTest()
    {
        ExpressionTests();
        ParsingTests();
        ApplyGrammarTests();
    }

    private void ExpressionTests()
    {
        Debug.Log("[Test] Start ExpressionFunctionalTests tests");
        ExpressionFunctionalTest("1+2", 3);
        ExpressionFunctionalTest("1+2*3", 7);
        ExpressionFunctionalTest("(1+2)*3" ,9);
        ExpressionFunctionalTest("2 > 3" ,0);
        ExpressionFunctionalTest("2 < 3" ,1);
        ExpressionFunctionalTest("0 | 1" ,1);
        ExpressionFunctionalTest("1 | 1" ,1);
        ExpressionFunctionalTest("0 | 0" ,0);
        ExpressionFunctionalTest("1 & 1" ,1);
        ExpressionFunctionalTest("0 & 1" ,0);
        ExpressionFunctionalTest("2 = 2" ,1);
        ExpressionFunctionalTest("2 = 2.3" ,0);
        ExpressionFunctionalTest("!0" ,1);
        ExpressionFunctionalTest("!2.3" ,0);
        Debug.Log("[Test] End ExpressionFunctionalTests tests");
    }

    private void ApplyGrammarTests()
    {
        Debug.Log("[Test] Start ApplyGrammar tests");
        Define[] def =
        {
            new Define("a", 2),
            new Define("abon", 1.3f),
            new Define("b", 3.3f),
            new Define("nop", 4),
        };
        Rule[] rules =
        {
            new Rule("A", "B"),
            new Rule("A<B>C", "D"),
            new Rule("H(t)", "H(t+1)"),
            new Rule("I(t) : t > 2", "J(t, t)AA"),
            new Rule("I(t) : t < 2", "J(t * 2, t)BB"),
            new Rule("J(u, v) : *", "BAB(u,v)"),
            new Rule("K(u) : *", "K(u)&(12)/(5)"),
        };

        ApplyGrammarTest(rules, def, "AAAB", 1, "BBBB");
        ApplyGrammarTest(rules, def, "H(3)", 1, "H(4)");
        ApplyGrammarTest(rules, def, "I(0.5)", 1, "J(1,0.5)BB");
        ApplyGrammarTest(rules, def, "I(3)", 1, "J(3,3)AA");
        ApplyGrammarTest(rules, def, "J(1, 2)", 1, "BAB(1,2)");
        ApplyGrammarTest(rules, def, "K(2)", 2, "K(2)&(12)/(5)&(12)/(5)");

        Debug.Log("[Test] End ApplyGrammar tests");
    }
    private void ParsingTests()
    {
        // ------- Parse Parameters
        Debug.Log("[Test] Start parse parameters tests");
        List<string> resultWanted = new List<string>() { "t" };
        ParseParamatersTest("BBB", new List<string>());
        ParseParamatersTest("A < BTF > U", new List<string>());
        ParseParamatersTest("A(u,t) < B(s,v) > U", new List<string>(){"u", "t", "s", "v"});
        ParseParamatersTest("A(t)", resultWanted);
        ParseParamatersTest("BFU(t)VX", resultWanted);
        resultWanted = new List<string>() { "t", "u" };
        ParseParamatersTest("B(t,u)", resultWanted);
        resultWanted = new List<string>() { "t", "u", "v", "b" };
        ParseParamatersTest("B(t,u)A(v,b)", resultWanted);
        ParseParamatersTest("B(trio,ulu)A(voi,b)", new List<string>(){"trio", "ulu", "voi", "b"});
        Debug.Log("[Test] End parse parameters tests");

        // ------- Remove paramater
        Debug.Log("[Test] Start remove parameters tests");
        RemoveParameterTest("B(t,v)", "B");
        RemoveParameterTest("FCB(t,v)T", "FCBT");
        RemoveParameterTest("A((a+2), j)", "A");
        RemoveParameterTest("B(t,v)A((a+2), j)", "BA");
        RemoveParameterTest("A(u,t)<B(s, v)>U", "A<B>U");
        Debug.Log("[Test] End remove parameters tests");

        // ------- Replace Define
        Define[] def =
        {
            new Define("a", 2),
            new Define("abon", 1.3f),
            new Define("b", 3.3f),
            new Define("nop", 4),
        };
        Debug.Log("[Test] Start DefineReplace tests");
        DefineReplaceTest(def, "TH(a)B(abon)", "TH(2)B(1.3)");
        DefineReplaceTest(def, "H(a+1)", "H(2+1)");
        DefineReplaceTest(def, "TH(a,abon)", "TH(2,1.3)");
        DefineReplaceTest(def, "(nop,b)TH(f)B(abon)", "(4,3.3)TH(f)B(1.3)");
        Debug.Log("[Test] End DefineReplace tests");

        // ------- Extend rule
        Debug.Log("[Test] Start ExtendRule tests");
        Rule testR = new Rule("A", "B");
        RuleExtended result = new RuleExtended('A', "B");
        ExtendRuleTests(testR, def, result);

        testR = new Rule("A[0.2]", "BUAAD");
        result = new RuleExtended('A', "BUAAD", 0.2f);
        ExtendRuleTests(testR, def, result);

        testR = new Rule("A < A > TAA", "BUAAD");
        result = new RuleExtended('A', "BUAAD", 1, "A", "TAA");
        ExtendRuleTests(testR, def, result);

        testR = new Rule("J < A [0.8]", "TTIT");
        result = new RuleExtended('A', "TTIT", 0.8f, "J");
        ExtendRuleTests(testR, def, result);

        testR = new Rule("A(t,u)", "TTIT");
        result = new RuleExtended('A', "TTIT", 1, "", "", new List<string>(){"t", "u"});
        ExtendRuleTests(testR, def, result);

        testR = new Rule("V(j,o) < A(t,u) > D", "TTIT");
        result = new RuleExtended('A', "TTIT", 1, "V", "D", new List<string>(){"j", "o", "t", "u"});
        ExtendRuleTests(testR, def, result);

        testR = new Rule("A(t,u)", "T(u)!(abon)");
        result = new RuleExtended('A', "T(u)!(1.3)", 1, "", "", new List<string>(){"t", "u"});
        ExtendRuleTests(testR, def, result);
        Debug.Log("[Test] Start ExtendRule tests");


    }

    private void ParseParamatersTest(string str, List<string> resultWanted)
    {
        List<string> result = new List<string>();
        GrammarInterpretation.ParseParameters(result, str);
        if (!resultWanted.SequenceEqual(result))
        {
            string resStr = "{";
            foreach (var s in result)
            {
                resStr += s + ',';
            }
            resStr += "}";
            Debug.LogError("[Test] Parse Parameters error: " + str + " List get " + resStr);
        }
    }

    private void RemoveParameterTest(string str, string result)
    {
        string get = GrammarInterpretation.RemoveParameter(str);
        if (get != result)
            Debug.LogError("[Test] Remove Parameters error: Get: " + get + " | wanted: " + result);
    }

    private void ExpressionFunctionalTest(string str, float result, GrammarInterpretation.Define[] defines = null)
    {
        ExpressionParsing.ExpressionNode tree = ExpressionParsing.BuildExpressionTree(str);
        float get = ExpressionExecution.Execute(tree, defines);
        if (!Mathf.Approximately(get, result))
        {
            Debug.LogError("[Test] ExpressionFunctionalTest error: test:" + str +" Get: " + get + " | wanted: " + result);
        }
    }

    private void DefineReplaceTest(Define[] def, string str, string resultWanted)
    {
        string get = GrammarInterpretation.ReplaceDefine(def, str);
        if (get != resultWanted)
            Debug.LogError("[Test] DefineReplace error: Get: " + get + " | wanted: " + resultWanted);
    }

    private void ExtendRuleTests(Rule rule, Define[] def, RuleExtended resultWanted)
    {
        RuleExtended resExtend = GrammarInterpretation.ExtendRule(rule, def);
        string resStr = JsonUtility.ToJson(resExtend, true);
        string wantedStr = JsonUtility.ToJson(resultWanted, true);

        if (resStr != wantedStr)
        {
            Debug.LogError("[Test] ExtendRule error: " + rule.predecessor + " \n--- Get:" + resStr + "\n--- Wanted:" + wantedStr);
        }
    }

    private void ApplyGrammarTest(Rule[] rules, Define[] defines, string axiom, int nbIterations, string resultWanted)
    {
        string get = GrammarInterpretation.ApplyGrammar(rules, defines, axiom, nbIterations);
        if (get != resultWanted)
        {
            Debug.LogError("[Test] ApplyGrammar error: " + axiom + " \n--- Get:   " + get + " \n--- Wanted:" + resultWanted);
        }
    }
}