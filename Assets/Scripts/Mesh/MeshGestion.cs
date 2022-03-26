using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using CylinderInfos = GenerateCylinder.CylinderInfos;

[ExecuteInEditMode]
public class MeshGestion : MonoBehaviour
{
    public static MeshGestion Instance;

    public bool spawnMesh;
    public bool cleanMesh;
    public List<Transform> points;
    public float startRadius = 0.3f;
    public int cylinderNbFaces = 15;


    private Mesh meshGenerated;
    private MeshFilter meshFilter;
    private bool spawn3DShape = false;

    struct TurtleInfos
    {
        public Vector3 position;
        // Heading, Left, Up
        public Vector3[] hlu;

        public TurtleInfos(Vector3 _pos, Vector3[] _hlu)
        {
            position = _pos;
            hlu = _hlu;
        }
    }

    // Update is called once per frame
    void Update()
    {
        #if UNITY_EDITOR
        Instance = this;

        if (!meshGenerated || cleanMesh)
        {
            InitMesh();
            cleanMesh = false;
        }

        if (spawnMesh)
        {
            spawnMesh = false;

            GenerateCylinder.nbFaces = cylinderNbFaces;

            float totalDistance = 0;
            for (int i = 1; i < points.Count; i++)
            {
                totalDistance += (points[i - 1].position - points[i].position).magnitude;
            }

            float decreaseByUnit = startRadius / totalDistance;
            Debug.Log("Total dist :" + totalDistance +  " DecreaseForce " + decreaseByUnit);
            float actualDistance = 0;
            for (int i = 1; i < points.Count; i++)
            {
                float dist = (points[i - 1].position - points[i].position).magnitude;
                float radiusBot = startRadius - actualDistance * decreaseByUnit;
                actualDistance += dist;
                float radiusTop = startRadius - actualDistance * decreaseByUnit;
                CylinderInfos newCylinder = GenerateCylinder.CreateCylinder(meshGenerated.vertices, meshGenerated.triangles, points[i - 1].position, points[i].position, radiusBot, radiusTop);

                meshGenerated.vertices = newCylinder.vertices.ToArray();
                meshGenerated.triangles = newCylinder.triangles.ToArray();
                meshFilter = gameObject.GetComponent<MeshFilter>();
                meshFilter.mesh = meshGenerated;
            }
        }
        #endif
    }

    private void InitMesh()
    {
        GenerateCylinder.nbFaces = cylinderNbFaces;
        if (meshGenerated)
            meshGenerated.Clear();
        else
            meshGenerated = new Mesh ();
        meshFilter = gameObject.GetComponent<MeshFilter> ();
        meshFilter.mesh = meshGenerated;
    }

    // Generate the mesh using DOL-bracket method
    public void GenerateMeshFromSentence(string sentence, float lengthPart, float angleTheta, float radiusBranch, float timeBetweenBranch, int _cylinderNbFaces = 4, bool orientation3D = true)
    {
        cylinderNbFaces = _cylinderNbFaces;
        spawn3DShape = orientation3D;
        StopAllCoroutines();
        InitMesh();
        StartCoroutine(IGenerateMeshFromSentence(sentence, lengthPart, angleTheta, radiusBranch, timeBetweenBranch));
    }

    IEnumerator IGenerateMeshFromSentence(string sentence, float lengthPart, float angleTheta, float radiusBranch, float timeBetweenBranch)
    {
        Vector3 turtlePosition = Vector3.zero;
        Vector3[] turtleOrientation = new[] {Vector3.forward, Vector3.left, Vector3.right};
        if (!spawn3DShape)
        {
            turtleOrientation[0] = Vector3.up;
        }
        Stack<TurtleInfos> turtleStack = new Stack<TurtleInfos>();

        foreach (var c in sentence)
        {
            if (c == '[') // Push information on the stack
            {
                turtleStack.Push(new TurtleInfos(turtlePosition, turtleOrientation));
            }
            else if (c == ']') // Unstack position to change
            {
                TurtleInfos newInfos = turtleStack.Pop();
                turtlePosition = newInfos.position;
                turtleOrientation = newInfos.hlu;
            }
            else if (VegetationGeneration.rotationChar.Contains(c))
            {
                // Rotate the vector. Change only the heading if in 2D
                if (spawn3DShape)
                    turtleOrientation = Utils.rotate3DVector(turtleOrientation, angleTheta, c);
                else
                    turtleOrientation[0] = Utils.rotate2DVector(turtleOrientation[0], angleTheta, c);
            }
            else
            {
                Vector3 startPoint = turtlePosition;
                turtlePosition += turtleOrientation[0].normalized * lengthPart;
                Vector3 endPoint = turtlePosition;

                CylinderInfos newCylinder = GenerateCylinder.CreateCylinder(meshGenerated.vertices, meshGenerated.triangles, startPoint, endPoint, radiusBranch, radiusBranch);

                meshGenerated.vertices = newCylinder.vertices.ToArray();
                meshGenerated.triangles = newCylinder.triangles.ToArray();
                meshFilter = gameObject.GetComponent<MeshFilter>();
                meshFilter.mesh = meshGenerated;

                if (timeBetweenBranch > 0 && Application.isPlaying) // Wait only out of editor mode
                {
                    yield return new WaitForSeconds(timeBetweenBranch);
                }
            }
        }
    }


}