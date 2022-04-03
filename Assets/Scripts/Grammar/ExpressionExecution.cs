using System;
using System.Linq;
using UnityEngine;
using ExpressionNode = ExpressionParsing.ExpressionNode;
using Define = GrammarInterpretation.Define;

public class ExpressionExecution
{
    // Execute an expression tree and return numerical result
    public static float Execute(ExpressionNode node, Define[] defines)
    {
        if (node == null)
        {
            throw new Exception("[Grammar] Problem in the shape of the expression tree"); // Should never come to a 'null' node
        }

        // If we are on an operator
        if (node.label.Length == 1 && ExpressionParsing.allOperators.Contains(node.label[0]))
        {
            float leftValue = Execute(node.leftChild, defines);
            float rightValue = 0;
            if (node.label[0] != '!')
                rightValue = Execute(node.rightChild, defines);
            return ApplyOperand(node.label[0], leftValue, rightValue);
        }

        if (!String.IsNullOrEmpty(node.label)) // If we are on a word
        {
            // Change the word by the value pass in paramater
            if (FindDefineValue(defines, node.label, out float conversion))
                node.number = conversion;
            else
                throw new Exception("[Grammar] Word not defined: " + node.label);
        }

        return node.number;
    }

    // Return true or false if find a value. out the value found
    private static bool FindDefineValue(Define[] defines, string word, out float conversion)
    {
        conversion = 0;

        if (defines == null)
            return false;

        foreach (var d in defines)
        {
            if (d.word == word)
            {
                conversion = d.value;
                return true;
            }
        }

        return false;
    }

    // Apply the operand effect on its children
    private static float ApplyOperand(char operand, float leftValue, float rightValue)
    {
        // {'+', '-', '*', '/', '^', '<', '>', '=', '!', '&', '|'};
        bool left = !Mathf.Approximately(leftValue, 0);
        bool right = !Mathf.Approximately(rightValue, 0);

        switch (operand)
        {
            case '+' :
                return leftValue + rightValue;
            case '-' :
                return leftValue - rightValue;
            case '*' :
                return leftValue * rightValue;
            case '/' :
                if (Mathf.Approximately(rightValue, 0))
                    throw new Exception("[Grammar] Try to divide by 0!");
                return leftValue / rightValue;
            case '^' :
                return Mathf.Pow(leftValue, rightValue);
            case '<' :
                return leftValue < rightValue ? 1 : 0;
            case '>' :
                return leftValue > rightValue ? 1 : 0;
            case '=' :
                return Mathf.Approximately(leftValue, rightValue) ? 1 : 0;
            case '!' :
                return Mathf.Approximately(leftValue, 0) ? 1 : 0;
            case '&' :
                return left && right ? 1 : 0;
            case '|' :
                return left || right ? 1 : 0;
        }

        return 0;
    }
}