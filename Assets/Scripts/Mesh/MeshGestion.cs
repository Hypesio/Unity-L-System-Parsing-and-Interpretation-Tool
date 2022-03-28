using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using CylinderInfos = GenerateCylinder.CylinderInfos;

[ExecuteInEditMode]
public class MeshGestion : MonoBehaviour
{
    public static MeshGestion Instance;

    public bool cleanMesh;
    public int cylinderNbFaces = 15;


    private Mesh meshGenerated;
    private MeshFilter meshFilter;
    private bool spawn3DShape = false;

    private List<Vector3> meshVertices;
    private List<int> meshTriangles;
    private List<Color32> meshColors;

    struct TurtleInfos
    {
        public Vector3 position;
        // Heading, Left, Up
        public Vector3[] hlu;
        public float radius;
        public int indexColor;
        public CylinderInfos previousCylinder;

        public TurtleInfos(Vector3 _pos, Vector3[] _hlu, float _radius, int _indexColor, CylinderInfos _previousCylinder)
        {
            position = _pos;
            hlu = _hlu;
            radius = _radius;
            indexColor = _indexColor;
            previousCylinder = _previousCylinder;
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
    public void GenerateMeshFromSentence(string sentence, float lengthPart, float angleTheta, float radiusBranch, float timeBetweenBranch, int _cylinderNbFaces, bool orientation3D, float decrementRadiusMultiplier, Color32[] colors)
    {
        cylinderNbFaces = _cylinderNbFaces;
        spawn3DShape = orientation3D;
        StopAllCoroutines();
        InitMesh();
        StartCoroutine(IGenerateMeshFromSentence(sentence, lengthPart, angleTheta, radiusBranch, timeBetweenBranch, decrementRadiusMultiplier, colors));
    }

    IEnumerator IGenerateMeshFromSentence(string sentence, float lengthPart, float angleTheta, float radiusBranch, float timeBetweenBranch, float decrementRadiusMultiplier, Color32[] colors)
    {
        meshTriangles = new List<int>();
        meshVertices = new List<Vector3>();
        meshColors = new List<Color32>();

        Vector3 turtlePosition = transform.position;
        Vector3[] turtleOrientation = new[] {Vector3.up, Vector3.left, Vector3.forward};
        float turtleRadius = radiusBranch;
        int indexColor = 0;
        CylinderInfos previousCylinder = null;

        Stack<TurtleInfos> turtleStack = new Stack<TurtleInfos>();

        foreach (var c in sentence)
        {
            if (c == '[') // Push information on the stack
            {
                turtleStack.Push(new TurtleInfos(turtlePosition, turtleOrientation, turtleRadius, indexColor, previousCylinder));
            }
            else if (c == ']') // Unstack position to change
            {
                TurtleInfos newInfos = turtleStack.Pop();
                turtlePosition = newInfos.position;
                turtleOrientation = newInfos.hlu;
                turtleRadius = newInfos.radius;
                indexColor = newInfos.indexColor;
                previousCylinder = newInfos.previousCylinder;
            }
            else if (VegetationGeneration.rotationChar.Contains(c))
            {
                //Debug.DrawRay(turtlePosition, turtleOrientation[0], Color.green, 2);
                // Rotate the vector. Change only the heading if in 2D
                if (spawn3DShape)
                    turtleOrientation = Utils.rotate3DVector(turtleOrientation, angleTheta, c);
                else
                    turtleOrientation[0] = Utils.rotate2DVector(turtleOrientation[0], angleTheta, c);

                //Debug.DrawRay(turtlePosition, turtleOrientation[0], Color.cyan, 2);
            }
            else if (c == '!') // Decrement segment radius
            {
                turtleRadius *= decrementRadiusMultiplier;
            }
            else if (c == '\'')
            {
                if (colors.Length == 0)
                {
                    Debug.LogError("[MeshGestion] You have to specify a color list!");
                    continue;
                }
                indexColor++;
                if (indexColor >= colors.Length)
                    indexColor = 0;
            }
            else
            {
                Vector3 startPoint = turtlePosition;
                turtlePosition += turtleOrientation[0].normalized * lengthPart;
                Vector3 endPoint = turtlePosition;

                int oldVerticeCount = meshVertices.Count;
                previousCylinder = GenerateCylinder.CreateCylinder(meshVertices, meshTriangles, startPoint, endPoint, turtleRadius,turtleRadius, previousCylinder);

                meshColors.AddRange(Enumerable.Repeat(colors[indexColor], meshVertices.Count - oldVerticeCount));

                if (timeBetweenBranch > 0 && Application.isPlaying) // Way to build the mesh progressively
                {
                    meshGenerated.vertices = meshVertices.ToArray();
                    meshGenerated.triangles = meshTriangles.ToArray();
                    meshFilter.mesh = meshGenerated;
                    yield return new WaitForSeconds(timeBetweenBranch);
                }
            }
        }

        // Apply the mesh to make it visible
        meshGenerated.vertices = meshVertices.ToArray();
        meshGenerated.triangles = meshTriangles.ToArray();
        meshGenerated.colors32 = meshColors.ToArray();
        meshGenerated.RecalculateNormals();
        meshFilter.mesh = meshGenerated;
    }


}