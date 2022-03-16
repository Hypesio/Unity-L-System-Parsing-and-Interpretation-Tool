using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[ExecuteInEditMode]
public class GenerateCylinder : MonoBehaviour
{
    public bool spawnCylinder = false;
    public Transform centerTopTransform;
    public float radiusBottom = 0.3f;
    public float radiusTop = 0.3f;
    public int nbFaces = 10;

    private Mesh meshGenerated;
    private MeshFilter meshFilter;
    private List<int> newTriangles;
    private List<Vector3> newVertices;

    private Vector3 actualUp;
    private Vector3 actualForward;
    // Start is called before the first fram update
    void Start()
    {
        meshGenerated = new Mesh ();
        meshFilter = gameObject.GetComponent<MeshFilter> ();
        meshFilter.mesh = meshGenerated;
    }

    // Update is called once per frame
    void Update()
    {
        #if UNITY_EDITOR
        if (!meshGenerated)
        {
            meshGenerated = new Mesh ();
            meshFilter = gameObject.GetComponent<MeshFilter> ();
            meshFilter.mesh = meshGenerated;
        }
        if (spawnCylinder)
        {
            spawnCylinder = false;
            SpawnCylinder(transform.position, centerTopTransform.position, radiusBottom, radiusTop);
        }
        #endif
    }

    void SpawnCylinder(Vector3 centerBot, Vector3 centerTop, float radiusBot, float radiusTop)
    {
        newVertices = new List<Vector3>();
        newTriangles = new List<int>();

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

        meshGenerated.Clear();
        meshGenerated.vertices = newVertices.ToArray();
        meshGenerated.triangles = newTriangles.ToArray();
        meshFilter = gameObject.GetComponent<MeshFilter> ();
        meshFilter.mesh = meshGenerated;
        Debug.Log("Mesh generated");
    }

    // Return the length of a face of the cylinder
    float GetLengthOfFaces(float nbFaces, float radius, Vector3 center)
    {
        float angleToAdd = (float)360 / nbFaces;

        // Get the length of a face
        Vector3 face1Pos = center + Quaternion.AngleAxis(angleToAdd, actualUp) * actualForward * radius;
        Vector3 face1Left = -Vector3.Cross((face1Pos - center), actualUp);
        Vector3 facePos = center + actualForward * radius;
        if (Utils.LineLineIntersection(out Vector3 intersectPoint, facePos, -Vector3.Cross(actualUp, actualForward),
            face1Pos, face1Left))
        {
            return (intersectPoint - face1Pos).magnitude * 2;
        }

        Debug.LogError("Impossible to compute facewidth! Around point: " + center, this);
        return 0;
    }

    // Add vertices and triangles to the mesh for one face of the cylinder
    void SpawnCylinderFace(Vector3 centerBot, Vector3 centerTop, float angleToAdd, float faceWidthBot, float faceWidthTop)
    {
        // TODO take into account previous vertices to diminue number of vertices

        Vector3 faceBotCenter = centerBot + Quaternion.AngleAxis(angleToAdd, actualUp) * actualForward * radiusBottom;
        Vector3 faceTopCenter = centerTop + Quaternion.AngleAxis(angleToAdd, actualUp) * actualForward * radiusTop;

        Vector3 faceRightBot = Vector3.Cross((faceBotCenter - centerBot), actualUp).normalized;
        Vector3 faceRightTop = Vector3.Cross((faceTopCenter - centerTop), actualUp).normalized;
        int verticeStartIndex = newVertices.Count;

        // 1. Create vertices
        newVertices.Add(faceTopCenter - faceRightTop * faceWidthTop / 2);
        newVertices.Add(faceTopCenter + faceRightTop * faceWidthTop / 2);
        newVertices.Add(faceBotCenter + faceRightBot * faceWidthBot / 2);
        newVertices.Add(faceBotCenter - faceRightBot * faceWidthBot / 2);

        // 2. Create triangles
        for (int i = 0; i < 3; i++)
        {
            newTriangles.Add(verticeStartIndex + i);
        }

        newTriangles.Add(verticeStartIndex);
        newTriangles.Add(verticeStartIndex + 2);
        newTriangles.Add(verticeStartIndex + 3);

    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, actualUp * 5);
        Gizmos.DrawRay(transform.position, actualForward * 5);

        Gizmos.color = Color.cyan;
        float angleToAdd = (float)360 / nbFaces;

        Vector3 face1Pos = transform.position + Quaternion.AngleAxis(angleToAdd, actualUp) * actualForward * radiusBottom;
        Vector3 face1Left = -Vector3.Cross((face1Pos - transform.position), actualUp);
        Vector3 facePos = transform.position + actualForward * radiusBottom;
        Vector3 faceLeft = -Vector3.Cross(actualUp, actualForward);

        Gizmos.DrawRay(face1Pos, face1Left * 3);
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(facePos, faceLeft * 3);

    }
}