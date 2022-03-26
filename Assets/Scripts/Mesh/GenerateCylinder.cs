using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEngine;

public class GenerateCylinder
{
    public struct CylinderInfos
    {
        public List<int> triangles;
        public List<Vector3> vertices;
        public List<(Vector3, int)> topVertices;
    }

    public static int nbFaces = 2;

    private static List<int> newTriangles;
    private static List<Vector3> newVertices;

    // Top vertices are useful to "connect" a cylinder to another
    private static List<(Vector3, int)> newTopVertices;
    private static List<Vector3> previousTopVertices;

    private static Vector3 actualUp;
    private static Vector3 actualForward;
    private static float radiusBot;
    private static float radiusTop;
    private static bool buildingACone = false;

    // Spawn a cylinder
    // Centers of top and bottom have to be in local space coordinate
    public static CylinderInfos CreateCylinder(Vector3[] vertices, int[] triangles, Vector3 centerBot, Vector3 centerTop, float _radiusBot, float _radiusTop)
    {
        radiusBot = _radiusBot;
        radiusTop = _radiusTop;

        newTopVertices = new List<(Vector3, int)>();
        newVertices = vertices.ToList();
        newTriangles = triangles.ToList();

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

        /*if (buildingACone)
            Debug.Log("Cone generated: Start pos "  + centerBot + " End pos" + centerTop);
        else
            Debug.Log("Cylinder generated: Start pos "  + centerBot + " End pos" + centerTop);*/
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
        // TODO take into account previous vertices to diminue number of vertices created

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

        int[] trianglesPoints = new int[]{};
        if (previousTopVertices == null)
        {
            newVertices.Add((faceBotCenter + faceRightBot * faceWidthBot / 2));
            newVertices.Add((faceBotCenter - faceRightBot * faceWidthBot / 2));
            trianglesPoints = new int[] {verticeStartIndex, verticeStartIndex + 1,verticeStartIndex + 2, verticeStartIndex, verticeStartIndex + 2, verticeStartIndex + 3};
        }
        else
        {
            trianglesPoints = new int[] {verticeStartIndex, verticeStartIndex + 1,verticeStartIndex + 2, verticeStartIndex, verticeStartIndex + 2, verticeStartIndex + 3};
        }

        // 2. Create triangles
        if (buildingACone)
        {
            int topVertice = newTopVertices[0].Item2;
            trianglesPoints = new int[]
                {newTopVertices[0].Item2,  verticeStartIndex, verticeStartIndex + 1};
        }

        foreach (int index in trianglesPoints)
        {
            newTriangles.Add(index);
        }

    }

    /*private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, actualUp * 5);
        Gizmos.DrawRay(transform.position, actualForward * 5);

        Gizmos.color = Color.cyan;
        float angleToAdd = (float)360 / nbFaces;

        Vector3 face1Pos = transform.position + Quaternion.AngleAxis(angleToAdd, actualUp) * actualForward * radiusBot;
        Vector3 face1Left = -Vector3.Cross((face1Pos - transform.position), actualUp);
        Vector3 facePos = transform.position + actualForward * radiusBot;
        Vector3 faceLeft = -Vector3.Cross(actualUp, actualForward);

        Gizmos.DrawRay(face1Pos, face1Left * 3);
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(facePos, faceLeft * 3);

    }*/
}