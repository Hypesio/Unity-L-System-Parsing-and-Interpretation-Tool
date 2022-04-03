using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Utils
{
    public static bool LineLineIntersection(out Vector3 intersection, Vector3 linePoint1,
        Vector3 lineVec1, Vector3 linePoint2, Vector3 lineVec2){

        Vector3 lineVec3 = linePoint2 - linePoint1;
        Vector3 crossVec1and2 = Vector3.Cross(lineVec1, lineVec2);
        Vector3 crossVec3and2 = Vector3.Cross(lineVec3, lineVec2);

        float s = Vector3.Dot(crossVec3and2, crossVec1and2)
                  / crossVec1and2.sqrMagnitude;
        intersection = linePoint1 + (lineVec1 * s);
        return true;
    }

    // Rotate a 2D vector according to the symbol used
    public static Vector3 rotate2DVector(Vector3 or, float angle, char symbol)
    {
        if (symbol == '-') // Rotate -theta (2D rotation)
        {
            angle = -angle;
        }

        Vector3 prevOr = or;
        float radAngle = angle * Mathf.Deg2Rad;

        or.x = or.x * Mathf.Cos(radAngle) - or.y * Mathf.Sin(radAngle);
        or.y = prevOr.x * Mathf.Sin(radAngle) + or.y * Mathf.Cos(radAngle);

        return or;
    }

    // Rotate a 3D Vector according to the symbol used
    public static Vector3[] rotate3DVector(Vector3[] hlu, float angle, char symbol)
    {
        //Debug.Log("Rotate " + angle);
        if (symbol == '$')
            return new[] {Vector3.up, Vector3.left, Vector3.forward};

        // Convert to radian
        angle = angle * Mathf.Deg2Rad;
        if (symbol == '-' || symbol == '∧' || symbol == '/' || symbol == '^')
            angle = -angle;

        float[,] matRot;
        if (symbol == '-' || symbol == '+' || symbol == '|')
        {
            angle = symbol == '|' ? 180 * Mathf.Deg2Rad : angle;
            matRot = new float[,]
            {
                {Mathf.Cos(angle), Mathf.Sin(angle), 0},
                {-Mathf.Sin(angle), Mathf.Cos(angle), 0},
                {0, 0, 1}
            };
        }
        else if (symbol == '&' || symbol == '∧' || symbol == '^')
        {
            matRot = new float[,]
            {
                {Mathf.Cos(angle), 0, -Mathf.Sin(angle)},
                {0, 1, 0},
                {Mathf.Sin(angle), 0, Mathf.Cos(angle)}
            };
        }
        else if (symbol == '\\' || symbol == '/')
        {
            matRot = new float[,]
            {
                {1, 0, 0},
                {0, Mathf.Cos(angle), -Mathf.Sin(angle)},
                {0, Mathf.Sin(angle), Mathf.Cos(angle)}
            };
        }
        else
        {
            Debug.LogWarning("Useless call to rotate3DVector with symbol: " + symbol);
            return hlu;
        }


        float[,] vectors =
        {
            {hlu[0].x, hlu[1].x, hlu[2].x},
            {hlu[0].y, hlu[1].y, hlu[2].y},
            {hlu[0].z, hlu[1].z, hlu[2].z}
        };
        float[,] rot = MultiplyMatrix(vectors, matRot);

        return new Vector3[]
        {
            new Vector3(rot[0, 0], rot[1, 0], rot[2, 0]).normalized,
            new Vector3(rot[0, 1], rot[1, 1], rot[2, 1]).normalized,
            new Vector3(rot[0, 2], rot[1, 2], rot[2, 2]).normalized,
        };
    }

    // Multiply 2 float matrix together
    public static float[,] MultiplyMatrix(float[,] a, float[,] b)
    {
        int sizeH = a.GetUpperBound(0) + 1;
        int sizeW = b.GetUpperBound(1) + 1;
        int communeSize = a.GetUpperBound(1) + 1;
        if (communeSize != b.GetUpperBound(0) + 1)
        {
            throw new Exception("Error bad matrix format for multiplication");
        }

        float[,] res = new float[sizeH, sizeW];
        for (int h = 0; h < sizeH; h++)
        {
            for (int w = 0; w < sizeW; w++)
            {
                float sum = 0;

                for (int i = 0; i < communeSize; i++)
                {
                    sum += a[h, i] * b[i, w];
                }

                res[h, w] = sum;
            }
        }

        return res;
    }


}