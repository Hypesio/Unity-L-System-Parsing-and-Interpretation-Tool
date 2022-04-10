using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class GenerateCylinder
{
    [System.Serializable]
    public class CylinderInfos
    {
        public List<int> triangles;
        public List<Vector3> vertices;
        public List<(Vector3, int)> topVertices;
    }

    public static int nbFaces = 2;

    private static List<int> newTriangles;
    private static List<Vector3> allMeshVertices;
    private static List<Vector3> cylinderVertices;

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

        int startTrianglesNb = triangles.Count;
        int startVerticesNb = vertices.Count;

        allMeshVertices = vertices;
        newTriangles = triangles;
        newTopVertices = new List<(Vector3, int)>();
        cylinderVertices = new List<Vector3>();

        buildingACone = Mathf.Approximately(radiusTop, 0);
        if (buildingACone)
        {
            newTopVertices.Add((centerTop, allMeshVertices.Count));
            allMeshVertices.Add(centerTop);
            cylinderVertices.Add(centerTop);
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

        var cylinderTriangles = newTriangles.GetRange(startTrianglesNb, newTriangles.Count - startTrianglesNb);

        return new CylinderInfos() {triangles = cylinderTriangles, vertices = cylinderVertices, topVertices = newTopVertices};
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

        Debug.LogError("Impossible to compute facewidth! Around point: " + center +  " with radius " + radius);
        return 0;
    }

    // Add vertices and triangles to the mesh for one face of the cylinder
    static void SpawnCylinderFace(Vector3 centerBot, Vector3 centerTop, float angleToAdd, float faceWidthBot, float faceWidthTop)
    {
        Vector3 faceBotCenter = centerBot + Quaternion.AngleAxis(angleToAdd, actualUp) * actualForward * radiusBot;
        Vector3 faceTopCenter = centerTop + Quaternion.AngleAxis(angleToAdd, actualUp) * actualForward * radiusTop;

        Vector3 faceRightBot = Vector3.Cross((faceBotCenter - centerBot), actualUp).normalized;
        Vector3 faceRightTop = Vector3.Cross((faceTopCenter - centerTop), actualUp).normalized;
        int verticeStartIndex = allMeshVertices.Count;

        Vector3 top1 = (faceTopCenter - faceRightTop * faceWidthTop / 2);
        Vector3 top2 = (faceTopCenter + faceRightTop * faceWidthTop / 2);
        int closestTop1 = verticeStartIndex;
        int closestTop2 = verticeStartIndex + 1;
        // 1. Create vertices
        if (!buildingACone)
        {
            GetClosestVertex(top1, previousCylinder,out closestTop1, 0.001f, true);
            GetClosestVertex(top2, previousCylinder,out closestTop2, 0.001f, true);
            newTopVertices.Add((top1, closestTop1));
            newTopVertices.Add((top2, closestTop2));
        }

        Vector3 bottom1 = (faceBotCenter + faceRightBot * faceWidthBot / 2);
        Vector3 bottom2 = (faceBotCenter - faceRightBot * faceWidthBot / 2);
        int[] trianglesPoints;
        if (previousCylinder == null)
        {
            GetClosestVertex(bottom1, previousCylinder, out int closestBottom1, 0.001f, true);
            GetClosestVertex(bottom2, previousCylinder, out int closestBottom2, 0.001f, true);
            trianglesPoints = new int[] {closestTop1,closestTop2, closestBottom1, closestTop1, closestBottom1, closestBottom2};
        }
        else
        {
            GetClosestVertex(bottom1, previousCylinder, out int closestBottom1);
            GetClosestVertex(bottom2, previousCylinder, out int closestBottom2);
            trianglesPoints = new int[] {closestTop1, closestTop2, closestBottom1, closestTop1, closestBottom1, closestBottom2};
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

        newTriangles.AddRange(trianglesPoints);
    }

    // Get the closest vertex from previous cylinder. Return true if already in the shape
    private static bool GetClosestVertex(Vector3 vertex, CylinderInfos refCylinder, out int closestVertex, float maxDistance = 50f, bool addToAllVertice = false)
    {
        closestVertex = -1;
        float minDistance = int.MaxValue;
        bool inShape = false;
        if (maxDistance < 1)
        {
            for (int i = 0; i < cylinderVertices.Count; i++)
            {
                float dist = Vector3.Distance(vertex, cylinderVertices[i]);
                if (dist < maxDistance && dist < minDistance)
                {
                    inShape = true;
                    minDistance = dist;
                    closestVertex = allMeshVertices.FindIndex(v => v == cylinderVertices[i]);
                }
            }
        }

        if (refCylinder != null)
        {
            for (int i = 0; i < refCylinder.topVertices.Count; i++)
            {
                float dist = Vector3.Distance(vertex, refCylinder.topVertices[i].Item1);
                if (dist < maxDistance && dist < minDistance)
                {
                    inShape = false;
                    minDistance = dist;
                    closestVertex = refCylinder.topVertices[i].Item2;
                }
            }
        }

        if (closestVertex == -1)
        {
            allMeshVertices.Add(vertex);
            closestVertex = allMeshVertices.Count - 1;
            cylinderVertices.Add(allMeshVertices[closestVertex]);
        }
        else if (!cylinderVertices.Contains(allMeshVertices[closestVertex]))
        {
            cylinderVertices.Add(allMeshVertices[closestVertex]);
        }

        return inShape;
    }

    // Add a face at the top of the cylinder
    public static void CloseTopCylinder(List<int> triangles, CylinderInfos cylinder)
    {
        if (cylinder == null)
            return;
        for (int i = 2; i < cylinder.topVertices.Count ; i+=2)
        {
            int[] triangle = new int[]
                {cylinder.topVertices[i + 1].Item2, cylinder.topVertices[i].Item2, cylinder.topVertices[0].Item2};
            cylinder.triangles.AddRange(triangle);
            triangles.AddRange(triangle);
        }
    }

    public static CylinderInfos CreateFace(List<Vector3> vertices, List<int> triangles, Vector3 startPos, Vector3 endPos, float widthFace, CylinderInfos previousFace)
    {
        allMeshVertices = vertices;
        newTriangles = triangles;
        newTopVertices = new List<(Vector3, int)>();
        cylinderVertices = new List<Vector3>();

        Vector3 planeOrientation = Vector3.Cross(endPos - startPos, Vector3.forward).normalized;

        int verticeStartIndex = vertices.Count;
        int trianglesStartIndex = triangles.Count;

        Vector3 top1 = endPos + planeOrientation.normalized * (widthFace / 2);
        Vector3 top2 = endPos - planeOrientation.normalized * (widthFace / 2);
        allMeshVertices.Add(top1);
        allMeshVertices.Add(top2);
        cylinderVertices.Add(top1);
        cylinderVertices.Add(top2);

        newTopVertices.Add((top1, verticeStartIndex));
        newTopVertices.Add((top2, verticeStartIndex + 1));

        Vector3 bottom1 = startPos + planeOrientation.normalized * (widthFace / 2);
        Vector3 bottom2 = startPos - planeOrientation.normalized * (widthFace / 2);
        int[] trianglesPoints;

        allMeshVertices.Add(bottom1);
        allMeshVertices.Add(bottom2);
        cylinderVertices.Add(bottom1);
        cylinderVertices.Add(bottom2);
        trianglesPoints = new int[] {verticeStartIndex, verticeStartIndex + 1,verticeStartIndex + 2, verticeStartIndex + 3, verticeStartIndex + 2, verticeStartIndex + 1};

        triangles.AddRange(trianglesPoints);

        if (previousFace != null) // Generate a cube for smoother face transitions
        {
            CreateFace(vertices, triangles, startPos - planeOrientation * (widthFace / 2), startPos + planeOrientation * (widthFace / 2), widthFace, null);
        }

        var cylinderTriangles = newTriangles.GetRange(trianglesStartIndex, 6);
        return new CylinderInfos() {triangles = cylinderTriangles, vertices = cylinderVertices, topVertices = newTopVertices};
    }

}