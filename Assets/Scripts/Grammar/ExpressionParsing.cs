using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class ExpressionParsing
{
    public class ExpressionNode
    {
        public string label;
        public float number;
        public ExpressionNode leftChild;
        public ExpressionNode rightChild;

        public ExpressionNode(string _label, ExpressionNode _leftChild = null, ExpressionNode _rightChild = null)
        {
            number = 0;
            label = _label;
            leftChild = _leftChild;
            rightChild = _rightChild;
        }

        public ExpressionNode(float _number, ExpressionNode _leftChild = null, ExpressionNode _rightChild = null)
        {
            number = _number;
            label = "";
            leftChild = _leftChild;
            rightChild = _rightChild;
        }
    }

    /*{ Operators and their priority
        ('+', 2), ('-', 2), ('*', 3), ('/', 3), ('^', 3),
        ('<', 1), ('>', 1), ('=', 1),
        ('!', 4), ('&', 1), ('|', 1)
    };*/
    public static char[] allOperators = {'+', '-', '*', '/', '^', '<', '>', '=', '!', '&', '|'};
    private static int[] operatorPriority = {2, 2, 3, 3, 3, 1, 1, 1, 4, 1, 1};


    // Build the expression tree from a string
    public static ExpressionNode BuildExpressionTree(string rawExpression)
    {
        int index = 0;
        Stack<char> charStack = new Stack<char>();
        Stack<ExpressionNode> nodeStack = new Stack<ExpressionNode>();
        string expression = Regex.Replace(rawExpression, " ", "");

        while (index < expression.Length)
        {
            char c = expression[index];
            if (c == '(')
            {
                charStack.Push('(');
            }
            else if (c == ')')
            {
                while (charStack.Count > 0 && charStack.Peek() != '(')
                {
                    char previousC = charStack.Pop();
                    nodeStack.Push(CreateOperatorNode(nodeStack, previousC, expression));
                }

                charStack.Pop(); // Pop the '('
            }
            else if (allOperators.Contains(c))
            {
                if (charStack.Count > 0)
                {
                    char previousC = charStack.Peek();
                    // Previous char is more important than actual one so we apply previous one
                    if (previousC != '(' && GetOperatorPriority(c) < GetOperatorPriority(previousC))
                    {
                        charStack.Pop();
                        nodeStack.Push(CreateOperatorNode(nodeStack, previousC, expression));
                    }
                }

                charStack.Push(c);
            }
            else if (c >= '0' && c <= '9')
            {
                float numParse = ParseNumber(expression, ref index);
                nodeStack.Push(new ExpressionNode(numParse));
            }
            else if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
            {
                string word = GetWord(expression, ref index);
                index--;
                nodeStack.Push(new ExpressionNode(word));
            }
            else
            {
                Debug.LogWarning("[Grammar] Unknown character '" + c + "' in expression: " + rawExpression);
            }

            index++;
        }

        while (nodeStack.Count > 1 || (charStack.Count != 0 && charStack.Peek() == '!'))
        {
            if (charStack.Count == 0)
            {
                throw new Exception("[Grammar] Invalid number of operators in: " + rawExpression);
            }

            nodeStack.Push(CreateOperatorNode(nodeStack, charStack.Pop(), expression));
        }

        return nodeStack.Pop();
    }

    // Return the priority of an operator
    private static int GetOperatorPriority(char c)
    {
        int index = Array.IndexOf(allOperators, c);
        return operatorPriority[index];
    }

    // Progress in the expression to return the whole word
    private static string GetWord(string expression, ref int index)
    {
        string word = "";
        while (index < expression.Length && ((expression[index] >= 'A' && expression[index] <= 'Z') ||
                                             (expression[index] >= 'a' && expression[index] <= 'z')))
        {
            word += expression[index];
            index++;
        }

        return word;
    }

    // Progress in the expression to get the whole number and parse it
    private static float ParseNumber(string expression, ref int index)
    {
        bool asPoint = false;
        string numberBuild = "";
        while (index < expression.Length &&
               ((expression[index] <= '9' && expression[index] >= '0') || expression[index] == '.'))
        {
            if (expression[index] == '.')
            {
                if (asPoint)
                    throw new Exception("[Grammar] Invalid number in expression" + expression);
                asPoint = true;
            }

            numberBuild += expression[index];
            index++;
        }

        index--;
        return float.Parse(numberBuild, CultureInfo.InvariantCulture);
    }

    // Add the children for a given operator
    private static ExpressionNode CreateOperatorNode(Stack<ExpressionNode> nodeStack, char c, string expression)
    {
        ExpressionNode parent = new ExpressionNode(c.ToString());
        try
        {
            if (c != '!')
                parent.rightChild = nodeStack.Pop();
            parent.leftChild = nodeStack.Pop();
        }
        catch
        {
            throw new Exception("[Grammar] Invalid expression: " + expression);
        }

        return parent;
    }
}