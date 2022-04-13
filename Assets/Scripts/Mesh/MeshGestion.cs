using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using CylinderInfos = GenerateCylinder.CylinderInfos;

[ExecuteInEditMode]
public class MeshGestion : MonoBehaviour
{
    public bool cleanMesh;
    public int cylinderNbFaces = 15;
    public bool destroyable = true;

    [Header("Debug Options")]
    public bool enableDebugFeatures;
    public bool waitBetweenEachLetter = false;
    [Tooltip("Need gizmo activated to be visible")]
    public float debugTurtleRadius = 0.2f;
    public UnityEvent<string> onStartBuildingMesh;
    public UnityEvent<int> onUpdateBuildingMesh;

    public Mesh meshGenerated { get; private set; }

    private MeshFilter meshFilter;
    private bool orientation3D = false;
    private bool flatShape = false;

    private List<Vector3> meshVertices;
    private List<int> meshTriangles;
    private List<Color32> meshColors;
    private float lengthPolygon;
    private Color32[] colors;
    private TurtleInfos turtle;
    private Stack<TurtleInfos> turtleStack;

    public List<TreeNode> treeArray { get; set; }

    #region Classes & structs

    class TurtleInfos
    {
        public Vector3 position;

        // Heading, Left, Up
        public Vector3[] hlu;
        public float radius;
        public int indexColor;
        public TreeNode previousNode;

        public TurtleInfos(Vector3 _pos, Vector3[] _hlu, float _radius, int _indexColor, TreeNode _previousNode)
        {
            position = _pos;
            hlu = _hlu;
            radius = _radius;
            indexColor = _indexColor;
            previousNode = _previousNode;
        }
    }

    [System.Serializable]
    public class TreeNode
    {
        public CylinderInfos cylinder;
        public List<int> childrenIndex;
        public int parentIndex;

        public TreeNode(CylinderInfos _cylinder, int _parent)
        {
            cylinder = _cylinder;
            childrenIndex = new List<int>();
            parentIndex = _parent;
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

    #endregion

    #region Monobehaviour
    void Start()
    {
        meshFilter = gameObject.GetComponent<MeshFilter>();
        if (!meshGenerated)
        {
            meshGenerated = meshFilter.sharedMesh;
        }

        if(destroyable)
            LoadTreeData();
    }

    // Update is called once per frame
    void Update()
    {
#if UNITY_EDITOR
        if (cleanMesh)
        {
            InitMesh();
            cleanMesh = false;
        }
#endif
    }
    #endregion

    // Generate the mesh using parametrical bracketed Dol system
    public void GenerateMeshFromSentence(string sentence, float lengthPart, float angleTheta, float radiusBranch,
        float timeBetweenBranch, int _cylinderNbFaces, bool _orientation3D, float decrementRadiusMultiplier,
        Color32[] colors, float _lengthPolygon, bool _flatShape = false)
    {
        onStartBuildingMesh?.Invoke(sentence);
        cylinderNbFaces = _cylinderNbFaces;
        orientation3D = _orientation3D;
        lengthPolygon = _lengthPolygon;
        flatShape = _flatShape;
        treeArray = new List<TreeNode>();
        StopAllCoroutines();
        InitMesh();
        StartCoroutine(IGenerateMeshFromSentence(sentence, lengthPart, angleTheta, radiusBranch, timeBetweenBranch,
            decrementRadiusMultiplier, colors));
    }

    // Coroutine to generate the mesh (will be progressive on play mode)
    IEnumerator IGenerateMeshFromSentence(string sentence, float lengthPart, float angleTheta, float radiusBranch,
        float timeBetweenBranch, float decrementRadiusMultiplier, Color32[] colors)
    {
        meshTriangles = new List<int>();
        meshVertices = new List<Vector3>();
        meshColors = new List<Color32>();
        this.colors = colors;
        Vector3[] hlu = new[] {Vector3.up, Vector3.left, Vector3.forward};
        treeArray.Add(new TreeNode(null, -1));
        turtle = new TurtleInfos(Vector3.zero, hlu, radiusBranch, 0, treeArray[0]);
        int leafNumber = 0;

        turtleStack = new Stack<TurtleInfos>();
        Polygon actualPolygon = null;
        Stack<Polygon> polygons = new Stack<Polygon>();

        for (int i = 0; i < sentence.Length; i++)
        {
            if (enableDebugFeatures && timeBetweenBranch > 0 && waitBetweenEachLetter && Application.isPlaying)
            {
                UpdateMesh(meshVertices, meshTriangles, meshColors);
                onUpdateBuildingMesh?.Invoke(i);
                yield return new WaitForSeconds(timeBetweenBranch);
            }

            char c = sentence[i];
            if (c == '(') // Skip useless parenthesis number
            {
                GrammarInterpretation.GetWordUntilChar(sentence, ref i, new[] {')'});
                c = sentence[i];
            }

            if (c == '[') // Push information on the stack
            {
                turtleStack.Push(turtle);
                turtle = new TurtleInfos(turtle.position, turtle.hlu, turtle.radius, turtle.indexColor,
                    turtle.previousNode);
            }
            else if (c == ']') // Unstack position to change
            {
                if (turtle.previousNode.childrenIndex.Count == 0) // We leave a terminal branch
                {
                    GenerateCylinder.CloseTopCylinder(meshTriangles, turtle.previousNode.cylinder);
                }

                if (turtleStack.Count == 0)
                    throw new Exception("[MeshGestion] Empty stack at closing bracket. Stop at: " +
                                        sentence.Substring(0, i));
                TurtleInfos newInfos = turtleStack.Pop();
                turtle = newInfos;
            }
            else if (VegetationGeneration.rotationChar.Contains(c))
            {
                float angle = GetNumberInParenthesis(sentence, ref i, angleTheta);
                // Rotate the vector. Change only the heading if in 2D
                if (orientation3D)
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
            else if (c == 'F' || c == 'G') //(c >= 'A' && c <= 'Z')
            {
                Vector3 startPoint = turtle.position;
                bool toBuild = c == 'F';
                while (c >= 'A' && c <= 'Z' || c == '(') // Way to merge consecutive cylinder and reduce vertex count
                {
                    if (c == '(') // Skip parenthesis for paremtrical grammar
                    {
                        GrammarInterpretation.GetWordUntilChar(sentence, ref i, new[] {')'});
                    }
                    else if (c == 'F' || c == 'G')
                    {
                        float length = GetNumberInParenthesis(sentence, ref i, lengthPart);
                        turtle.position += turtle.hlu[0].normalized * length;
                        if (leafNumber > 0 && c =='F')
                            RecordVertex(actualPolygon, turtle);
                    }

                    i++;
                    if (i >= sentence.Length)
                        break;

                    if (timeBetweenBranch > 0 && waitBetweenEachLetter && Application.isPlaying)
                    {
                        onUpdateBuildingMesh?.Invoke(i);
                        yield return new WaitForSeconds(timeBetweenBranch);
                    }
                    c = sentence[i];
                }
                i--;

                if (toBuild) // Create a cylinder
                {
                    Vector3 endPoint = turtle.position;

                    int oldVerticeCount = meshVertices.Count;

                    CylinderInfos previousCylinder = null;
                    if (turtle.previousNode != null)
                        previousCylinder = turtle.previousNode.cylinder;

                    CylinderInfos cylinder;
                    if (!flatShape)
                    {
                        cylinder = GenerateCylinder.CreateCylinder(meshVertices, meshTriangles,
                            startPoint,
                            endPoint, turtle.radius, turtle.radius, previousCylinder);
                    }
                    else
                    {
                        cylinder = GenerateCylinder.CreateFace(meshVertices, meshTriangles, startPoint, endPoint,
                            turtle.radius, previousCylinder);

                    }

                    TreeNode newNode = new TreeNode(cylinder, treeArray.FindIndex(p => p == turtle.previousNode));
                    treeArray.Add(newNode);
                    turtle.previousNode?.childrenIndex.Add(treeArray.Count - 1);
                    turtle.previousNode = newNode;

                    meshColors.AddRange(Enumerable.Repeat(colors[turtle.indexColor],
                        meshVertices.Count - oldVerticeCount));

                    if (!enableDebugFeatures && timeBetweenBranch > 0 && Application.isPlaying) // Way to build the mesh progressively
                    {
                        UpdateMesh(meshVertices, meshTriangles, meshColors);
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
            else if (c == '.') // Add vertice to polygon
            {
                RecordVertex(actualPolygon, turtle);
            }
            else if (c == 'f')
            {
                turtle.position += turtle.hlu[0].normalized * lengthPolygon;
                if (leafNumber > 0)
                    RecordVertex(actualPolygon, turtle);
            }
            else if (c == '%') // Ignore the rest of the branch
            {
                int depth = 1;
                while (depth != 0)
                {
                    GrammarInterpretation.GetWordUntilChar(sentence, ref i, new[] {'[', ']'});
                    depth += sentence[i] == '[' ? 1 : -1;
                    i = sentence[i] == '[' || depth != 0 ? i + 1 : i - 1 ;
                }
                i--;
            }

            if (meshTriangles.Count / 3 > 65000)
                throw new Exception("[MeshGestion] Mesh exceed max capacity! Polygons > 65000");

        }

        onUpdateBuildingMesh?.Invoke(sentence.Length);

        if (turtle.previousNode.childrenIndex.Count == 0) // We leave a terminal branch
        {
            GenerateCylinder.CloseTopCylinder(meshTriangles, turtle.previousNode.cylinder);
        }

        // Apply the mesh to make it visible
        UpdateMesh(meshVertices, meshTriangles, meshColors);

        Debug.Log("[MeshGestion] Mesh created with " + meshTriangles.Count / 3 + " polygons");
    }

    #region Utils
    private void UpdateMesh(List<Vector3> vertices, List<int> triangles, List<Color32> colors)
    {
        meshGenerated.vertices = vertices.ToArray();
        meshGenerated.triangles = triangles.ToArray();
        meshGenerated.colors32 = colors.ToArray();
        meshGenerated.RecalculateNormals();
        meshFilter.mesh = meshGenerated;
    }

    private void InitMesh()
    {
        GenerateCylinder.nbFaces = cylinderNbFaces;
        if (meshGenerated)
            meshGenerated.Clear();
        else
            meshGenerated = new Mesh();
        meshFilter = gameObject.GetComponent<MeshFilter>();
        meshFilter.mesh = meshGenerated;
    }

    // Draw a polygon by dividing it in triangles
    private void DrawPolygon(Polygon polygon, List<int> meshTriangles)
    {
        if (polygon.vertices.Count < 3)
        {
            Debug.LogError("[MeshGestion] Polygon has not enough vertices: " + polygon.vertices.Count);
        }

        for (int i = 1; i + 1 < polygon.vertices.Count; i++)
        {
            meshTriangles.Add(polygon.vertices[0]);
            meshTriangles.Add(polygon.vertices[i]);
            meshTriangles.Add(polygon.vertices[i + 1]);
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

    // Record the vertex position for the polygon
    private void RecordVertex(Polygon poly, TurtleInfos turtle)
    {
        meshVertices.Add(turtle.position);
        meshColors.Add(colors[turtle.indexColor]);
        poly.vertices.Add(meshVertices.Count - 1);
    }

    #endregion

    #region Debug

    private void OnDrawGizmos()
    {
        if (turtle == null)
            return;
        Gizmos.color = new Color(0, 0, 200, 0.5f);
        Gizmos.DrawSphere(transform.position + turtle.position, debugTurtleRadius);
        if (turtleStack != null && turtleStack.Count != 0)
        {
            Gizmos.color = new Color(200, 200, 200, 0.7f);
            Gizmos.DrawSphere(transform.position + turtleStack.Peek().position, debugTurtleRadius);
        }
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position + turtle.position, turtle.hlu[0]);
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position + turtle.position, turtle.hlu[1]);
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position + turtle.position, turtle.hlu[2]);
    }

    #endregion

    #region Save & Load Functions
    private class TreeSave
    {
        public List<TreeNode> treeArray;
    }
    public void MakeTreeStatic()
    {
        meshFilter = GetComponent<MeshFilter>();

        gameObject.AddComponent<MeshCollider>();

        TreeSave save = new TreeSave();
        save.treeArray = treeArray;

        string dataSaved = JsonUtility.ToJson(save);
        Debug.Log(dataSaved);

        string path = "Assets/Resources/Meshes/" + meshFilter.sharedMesh.name + ".json";
        FileStream fs = File.Create(path);
        fs.Close();
        File.WriteAllText(path, dataSaved);

        AssetDatabase.SaveAssets();
        LoadTreeData();
    }

    public void LoadTreeData()
    {
        meshFilter = GetComponent<MeshFilter>();
        try
        {
            string data = File.ReadAllText("Assets/Resources/Meshes/" + meshFilter.sharedMesh.name + ".json");
            treeArray = (JsonUtility.FromJson(data, typeof(TreeSave)) as TreeSave)?.treeArray;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[MeshGestion] Can't read tree data of " + meshFilter.sharedMesh.name +
                             "! (won't be destroyable) | " + e.Message);
        }
    }
    #endregion

    #region Static functions

    public static int GenerateNewTreeNodeArray(List<TreeNode> originalTreeNodeArray, List<TreeNode> treeNodeArray, TreeNode actualTree, int parentIndex, bool eraseNodeinOldArray)
    {
        if (parentIndex == -1 && eraseNodeinOldArray)
        {
            int oldIndex = originalTreeNodeArray.FindIndex(t => t == actualTree);
            originalTreeNodeArray[oldIndex] = null;
        }

        int indexNode = treeNodeArray.Count;
        treeNodeArray.Add(actualTree);
        actualTree.parentIndex = parentIndex;
        for (int i = 0; i < actualTree.childrenIndex.Count; i++)
        {
            int newIndex =
                GenerateNewTreeNodeArray(originalTreeNodeArray, treeNodeArray, originalTreeNodeArray[actualTree.childrenIndex[i]], indexNode, eraseNodeinOldArray);
            actualTree.childrenIndex[i] = newIndex;
            if (eraseNodeinOldArray)
                originalTreeNodeArray[actualTree.childrenIndex[i]] = null;
        }

        return indexNode;
    }
    #endregion
}