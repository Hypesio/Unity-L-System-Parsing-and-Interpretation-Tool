using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEngine;

public class GenerateCylinder
{
    public class CylinderInfos
    {
        public List<int> triangles;
        public List<Vector3> vertices;
        public List<(Vector3, int)> topVertices;
    }

    public static int nbFaces = 2;

    private static List<int> newTriangles;
    private static List<Vector3> newVertices;

    private static List<(Vector3, int)> newTopVertices;

    private static Vector3 actualUp;
    private static Vector3 actualForward;
    private static float radiusBot;
    private static float radiusTop;
    private static bool buildingACone = false;
    private static CylinderInfos previousCylinder;
    private static int indexTopPreviousCylinder;

    // Spawn a cylinder
    // Centers of top and bottom have to be in local space coordinate
    public static CylinderInfos CreateCylinder(List<Vector3> vertices, List<int> triangles, Vector3 centerBot, Vector3 centerTop, float _radiusBot, float _radiusTop, CylinderInfos previousCylinder)
    {
        GenerateCylinder.previousCylinder = previousCylinder;
        indexTopPreviousCylinder = 0;
        radiusBot = _radiusBot;
        radiusTop = _radiusTop;

        newVertices = vertices;
        newTriangles = triangles;
        newTopVertices = new List<(Vector3, int)>();

        buildingACone = radiusTop < 0.0001f;
        if (buildingACone)
        {
            newTopVertices.Add((centerTop, newVertices.Count));
            newVertices.Add(centerTop);
        }

        actualUp = (centerTop - centerBot).normalized;
        if (actualUp == Vector3.up || actualUp == Vector3.down)
            actualForward = Vector3.forward;
        else
            actualForward = Vector3.Cross(actualUp, Vector3.up).normalized;

        float faceWidthTop = GetLengthOfFaces(nbFaces, radiusTop, centerTop);
        float faceWidthBot = GetLengthOfFaces(nbFaces, radiusBot, centerBot);

        float angleToAdd = (float)360 / nbFaces;

        for (int i = 0; i < nbFaces; i++)
        {
            SpawnCylinderFace(centerBot, centerTop, angleToAdd * i, faceWidthBot, faceWidthTop);
        }

        return new CylinderInfos() {triangles = newTriangles, vertices = newVertices, topVertices = newTopVertices};
    }

    // Return the length of a face of the cylinder
    static float GetLengthOfFaces(float nbFaces, float radius, Vector3 center)
    {
        float angleToAdd = (float)360 / nbFaces;

        if (buildingACone && Mathf.Approximately(radius, radiusTop))
            return 0;

        // Get the length of a face by getting the distance between the intersect point
        // created by 2 lines colinear to their own face
        Vector3 face1Pos = center + Quaternion.AngleAxis(angleToAdd, actualUp) * actualForward * radius;
        Vector3 face1Left = -Vector3.Cross((face1Pos - center), actualUp);
        Vector3 facePos = center + actualForward * radius;
        if (Utils.LineLineIntersection(out Vector3 intersectPoint, facePos, -Vector3.Cross(actualUp, actualForward),
            face1Pos, face1Left))
        {
            return (intersectPoint - face1Pos).magnitude * 2;
        }

        Debug.LogError("Impossible to compute facewidth! Around point: " + center);
        return 0;
    }

    // Add vertices and triangles to the mesh for one face of the cylinder
    static void SpawnCylinderFace(Vector3 centerBot, Vector3 centerTop, float angleToAdd, float faceWidthBot, float faceWidthTop)
    {
        Vector3 faceBotCenter = centerBot + Quaternion.AngleAxis(angleToAdd, actualUp) * actualForward * radiusBot;
        Vector3 faceTopCenter = centerTop + Quaternion.AngleAxis(angleToAdd, actualUp) * actualForward * radiusTop;

        Vector3 faceRightBot = Vector3.Cross((faceBotCenter - centerBot), actualUp).normalized;
        Vector3 faceRightTop = Vector3.Cross((faceTopCenter - centerTop), actualUp).normalized;
        int verticeStartIndex = newVertices.Count;

        // 1. Create vertices
        if (!buildingACone)
        {
            Vector3 top1 = (faceTopCenter - faceRightTop * faceWidthTop / 2);
            Vector3 top2 = (faceTopCenter + faceRightTop * faceWidthTop / 2);
            newTopVertices.Add((top1, verticeStartIndex));
            newTopVertices.Add((top2, verticeStartIndex + 1));
            newVertices.Add(top1);
            newVertices.Add(top2);
        }

        Vector3 bottom1 = (faceBotCenter + faceRightBot * faceWidthBot / 2);
        Vector3 bottom2 = (faceBotCenter - faceRightBot * faceWidthBot / 2);
        int[] trianglesPoints;
        if (previousCylinder == null)
        {
            newVertices.Add(bottom1);
            newVertices.Add(bottom2);
            trianglesPoints = new int[] {verticeStartIndex, verticeStartIndex + 1,verticeStartIndex + 2, verticeStartIndex, verticeStartIndex + 2, verticeStartIndex + 3};
        }
        else
        {
            trianglesPoints = new int[] {verticeStartIndex, verticeStartIndex + 1, GetClosestTopVertex(bottom1), verticeStartIndex, GetClosestTopVertex(bottom1), GetClosestTopVertex(bottom2)};
            indexTopPreviousCylinder += 2;
        }

        // 2. Create triangles
        if (buildingACone)
        {
            if (previousCylinder != null)
            {
                trianglesPoints = new int[]
                {
                    newTopVertices[0].Item2, previousCylinder.topVertices[indexTopPreviousCylinder].Item2,
                    previousCylinder.topVertices[indexTopPreviousCylinder + 1].Item2
                };
                indexTopPreviousCylinder++;
            }
            else
                trianglesPoints = new int[]
                    {newTopVertices[0].Item2,  verticeStartIndex, verticeStartIndex + 1};
        }

        foreach (int index in trianglesPoints)
        {
            newTriangles.Add(index);
        }

    }

    // Get the closest vertex from previous cylinder
    private static int GetClosestTopVertex(Vector3 vertex)
    {
        int closestOne = 0;
        float minDistance = int.MaxValue;
        for (int i = 0; i < previousCylinder.topVertices.Count; i++)
        {
            float dist = Vector3.Distance(vertex, previousCylinder.topVertices[i].Item1);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestOne = previousCylinder.topVertices[i].Item2;
            }
        }

        return closestOne;
    }

}