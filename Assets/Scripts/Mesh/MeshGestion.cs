using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using CylinderInfos = GenerateCylinder.CylinderInfos;

[ExecuteInEditMode]
public class MeshGestion : MonoBehaviour
{

    public bool cleanMesh;
    public int cylinderNbFaces = 15;

    [HideInInspector] public Mesh meshToLoadOnStart;

    private Mesh meshGenerated;
    private MeshFilter meshFilter;
    private bool spawn3DShape = false;

    private List<Vector3> meshVertices;
    private List<int> meshTriangles;
    private List<Color32> meshColors;
    private float lengthPolygon;
    private Color32[] colors;

    class TurtleInfos
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

    class Polygon
    {
        public List<int> vertices;

        public Polygon()
        {
            vertices = new List<int>();
        }
    }

    void Start()
    {
        if (meshToLoadOnStart)
        {
            gameObject.GetComponent<MeshFilter>().sharedMesh = meshToLoadOnStart;
            meshGenerated = meshToLoadOnStart;
        }
    }

    // Update is called once per frame
    void Update()
    {
        #if UNITY_EDITOR
        if (!meshToLoadOnStart && cleanMesh)
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
    public void GenerateMeshFromSentence(string sentence, float lengthPart, float angleTheta, float radiusBranch, float timeBetweenBranch, int _cylinderNbFaces, bool orientation3D, float decrementRadiusMultiplier, Color32[] colors, float _lengthPolygon)
    {
        cylinderNbFaces = _cylinderNbFaces;
        spawn3DShape = orientation3D;
        lengthPolygon = _lengthPolygon;
        StopAllCoroutines();
        InitMesh();
        StartCoroutine(IGenerateMeshFromSentence(sentence, lengthPart, angleTheta, radiusBranch, timeBetweenBranch, decrementRadiusMultiplier, colors));
    }

    // Coroutine to generate the mesh (will be progressive on play mode)
    IEnumerator IGenerateMeshFromSentence(string sentence, float lengthPart, float angleTheta, float radiusBranch, float timeBetweenBranch, float decrementRadiusMultiplier, Color32[] colors)
    {
        meshTriangles = new List<int>();
        meshVertices = new List<Vector3>();
        meshColors = new List<Color32>();
        this.colors = colors;
        Vector3[] hlu = new[] {Vector3.up, Vector3.left, Vector3.forward};
        TurtleInfos turtle = new TurtleInfos(Vector3.zero, hlu, radiusBranch, 0, null);
        int leafNumber = 0;

        Stack<TurtleInfos> turtleStack = new Stack<TurtleInfos>();
        Polygon actualPolygon = null;
        Stack<Polygon> polygons = new Stack<Polygon>();

        for (int i = 0; i < sentence.Length; i ++)
        {
            char c = sentence[i];
            if (c == '(') // Skip useless parenthesis number
            {
                GrammarInterpretation.GetWordUntilChar(sentence, ref i, new[] {')'});
            }
            if (c == '[') // Push information on the stack
            {
                turtleStack.Push(turtle);
                turtle = new TurtleInfos(turtle.position, turtle.hlu, turtle.radius, turtle.indexColor,
                    turtle.previousCylinder);
            }
            else if (c == ']') // Unstack position to change
            {
                TurtleInfos newInfos = turtleStack.Pop();
                turtle = newInfos;
            }
            else if (VegetationGeneration.rotationChar.Contains(c))
            {
                float angle = GetNumberInParenthesis(sentence, ref i, angleTheta);
                // Rotate the vector. Change only the heading if in 2D
                if (spawn3DShape)
                    turtle.hlu = Utils.rotate3DVector(turtle.hlu, angle, c);
                else
                    turtle.hlu[0] = Utils.rotate2DVector(turtle.hlu[0], angle, c);

            }
            else if (c == '!') // Decrement segment radius
            {
                float radius = GetNumberInParenthesis(sentence, ref i, decrementRadiusMultiplier);
                if (Mathf.Approximately(radius, decrementRadiusMultiplier))
                    turtle.radius *= radius;
                else
                    turtle.radius = radiusBranch * radius;
            }
            else if (c == '\'') // Change vertice color
            {
                if (colors.Length == 0)
                {
                    Debug.LogError("[MeshGestion] You have to specify a color list!");
                    continue;
                }

                turtle.indexColor++;
                if (turtle.indexColor >= colors.Length)
                    turtle.indexColor = 0;
            }
            else if (c== 'F' || c == 'G')//(c >= 'A' && c <= 'Z')
            {
                Vector3 startPoint = turtle.position;
                bool toBuild = c == 'F';
                while (c >= 'A' && c <= 'Z' || c == '(') // Way to merge consecutive cylinder and reduce vertex count
                {
                    if (c == '(')
                    {
                        GrammarInterpretation.GetWordUntilChar(sentence, ref i, new[] {')'});
                        i++;
                    }

                    if (c == 'F')
                    {
                        float length = GetNumberInParenthesis(sentence, ref i, lengthPart);
                        turtle.position += turtle.hlu[0].normalized * length;
                        if (leafNumber > 0)
                            RecordVertex(actualPolygon, turtle);
                    }

                    i++;
                    if (i > sentence.Length)
                        break;
                    c = sentence[i];
                }
                i--;

                if (toBuild) // Create a cylinder
                {
                    Vector3 endPoint = turtle.position;

                    int oldVerticeCount = meshVertices.Count;
                    turtle.previousCylinder = GenerateCylinder.CreateCylinder(meshVertices, meshTriangles, startPoint,
                        endPoint, turtle.radius, turtle.radius, turtle.previousCylinder);

                    meshColors.AddRange(Enumerable.Repeat(colors[turtle.indexColor], meshVertices.Count - oldVerticeCount));

                    if (timeBetweenBranch > 0 && Application.isPlaying) // Way to build the mesh progressively
                    {
                        meshGenerated.vertices = meshVertices.ToArray();
                        meshGenerated.triangles = meshTriangles.ToArray();
                        meshFilter.mesh = meshGenerated;
                        yield return new WaitForSeconds(timeBetweenBranch);
                    }
                }
            }
            else if (c == '{') // Start a new polygon
            {
                polygons.Push(actualPolygon);
                actualPolygon = new Polygon();
                leafNumber++;
            }
            else if (c == '}') // Draw the actual polygon
            {
                DrawPolygon(actualPolygon, meshTriangles);
                actualPolygon = polygons.Pop();
                leafNumber--;
            }
            else if (c== '.') // Add vertice to polygon
            {
                RecordVertex(actualPolygon, turtle);
            }
            else if (c == 'f')
            {
                turtle.position += turtle.hlu[0].normalized * lengthPolygon;
                if (leafNumber > 0)
                    RecordVertex(actualPolygon, turtle);
            }

        }

        // Apply the mesh to make it visible
        meshGenerated.vertices = meshVertices.ToArray();
        meshGenerated.triangles = meshTriangles.ToArray();
        meshGenerated.colors32 = meshColors.ToArray();
        meshGenerated.RecalculateNormals();
        meshFilter.mesh = meshGenerated;

        Debug.Log("[MeshGestion] Mesh created with " + meshTriangles.Count/3 + " polygons");
    }

    // Draw a polygon by dividing it in triangles
    private void DrawPolygon(Polygon polygon, List<int> meshTriangles)
    {
        if (polygon.vertices.Count < 3)
        {
            Debug.LogError("[MeshGestion] Polygon has not enough vertices: " + polygon.vertices.Count);
        }

        for (int i = 1; i+1 < polygon.vertices.Count; i++)
        {
            meshTriangles.Add(polygon.vertices[0]);
            meshTriangles.Add(polygon.vertices[i]);
            meshTriangles.Add(polygon.vertices[i+1]);
        }
    }

    private float GetNumberInParenthesis(string sentence, ref int index, float regularValue)
    {
        if (index + 1 < sentence.Length && sentence[index + 1] == '(')
        {
            index += 2;
            string num = GrammarInterpretation.GetWordUntilChar(sentence, ref index, new[] {')'});
            return float.Parse(num, CultureInfo.InvariantCulture);
        }

        return regularValue;
    }

    private void RecordVertex(Polygon poly, TurtleInfos turtle)
    {
        meshVertices.Add(turtle.position);
        meshColors.Add(colors[turtle.indexColor]);
        poly.vertices.Add( meshVertices.Count - 1);
    }

}