using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    struct TurtleInfos
    {
        public Vector3 position;
        public Vector3 orientation;

        public TurtleInfos(Vector3 _pos, Vector3 _or)
        {
            position = _pos;
            orientation = _or;
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        InitMesh();
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
    public void GenerateMeshFromSentence(string sentence, float lengthPart, float angleTheta, float radiusBranch, float timeBetweenBranch)
    {
        StopAllCoroutines();
        InitMesh();
        StartCoroutine(IGenerateMeshFromSentence(sentence, lengthPart, angleTheta, radiusBranch, timeBetweenBranch));
    }

    IEnumerator IGenerateMeshFromSentence(string sentence, float lengthPart, float angleTheta, float radiusBranch, float timeBetweenBranch)
    {
        Vector3 turtlePosition = Vector3.zero;
        Vector3 turtleOrientation = Vector3.up;
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
                turtleOrientation = newInfos.orientation;
            }
            else if (c == '+') // Rotate +theta (2D rotation)
            {
                turtleOrientation = rotate2DVector(turtleOrientation, angleTheta);
            }
            else if (c == '-') // Rotate -theta (2D rotation)
            {
                turtleOrientation = rotate2DVector(turtleOrientation, -angleTheta);
            }
            else
            {
                Vector3 startPoint = turtlePosition;
                turtlePosition += turtleOrientation.normalized * lengthPart;
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

    public Vector3 rotate2DVector(Vector3 or, float angle)
    {
        float radAngle = angle * Mathf.Deg2Rad;
        or.x = or.x * Mathf.Cos(radAngle) - or.y * Mathf.Sin(radAngle);
        or.y = or.x * Mathf.Sin(radAngle) + or.y * Mathf.Cos(radAngle);
        return or;
    }
}