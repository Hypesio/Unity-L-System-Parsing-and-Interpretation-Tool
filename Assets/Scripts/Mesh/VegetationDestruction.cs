using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using TreeNode = MeshGestion.TreeNode;

public class VegetationDestruction : MonoBehaviour
{
    public float forceEjection = 2.3f;
    public float timeIgnoreCollision = 0.2f;
    public GameObject explosionEffect;

    private Camera mainCam;

    // Start is called before the first frame update
    void Start()
    {
        mainCam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            Ray mouseRay = mainCam.ScreenPointToRay(Input.mousePosition);
            Vector3 direction = mouseRay.direction;
            if (Physics.Raycast(mouseRay,out RaycastHit hit, 20))
            {
                MeshGestion meshGestion = hit.collider.GetComponent<MeshGestion>();
                if (meshGestion != null)
                {
                    if (Application.isPlaying && hit.collider.attachedRigidbody)
                    {
                        MeshCollider collider = meshGestion.GetComponent<MeshCollider>();
                        collider.attachedRigidbody.isKinematic = true;
                        collider.convex = false;
                        Physics.Raycast(mouseRay, out hit, 20);

                        Touched(meshGestion, direction, hit.triangleIndex, hit.point);
                    }
                    else
                    {
                        Touched(meshGestion, direction, hit.triangleIndex, hit.point);
                    }

                    if (explosionEffect)
                    {
                        GameObject effect = Instantiate(explosionEffect, hit.point, Quaternion.identity);
                        Destroy(effect, 2);
                    }
                }
            }
        }
    }

    public void Touched(MeshGestion meshGestion, Vector3 direction, int triangleIndex, Vector3 positionTouched)
    {

        // For test purpose change color of triangle
        int[] triangles = meshGestion.meshGenerated.triangles;
        if (triangleIndex * 3 + 3 >= triangles.Length || triangleIndex < 0)
        {
            Debug.LogWarning("[VegetationDestruction] Invalid triangle specified. Triangle: " + triangleIndex + '/' + triangles.Length/3);
            return;
        }

        int[] triangleTouched = new []
            {triangles[triangleIndex * 3], triangles[triangleIndex * 3 + 1], triangles[triangleIndex * 3 + 2]};

        List<TreeNode> nodes = meshGestion.treeArray;

        if (nodes == null || nodes.Count == 0)
        {
            Debug.LogWarning("[VegetationDestruction] No tree array link with the tree: " +
                             meshGestion.gameObject.name);
            return;
        }

        TreeNode nodeTouched = GetTriangleNode(triangleTouched, nodes, nodes[0]);
        if (nodeTouched == null)
            throw new Exception("[VegetationDestruction] Can't find node touched");

        if (nodeTouched.parentIndex == -1)
        {
            meshGestion.GetComponent<Rigidbody>()?.AddForce(direction * forceEjection, ForceMode.Impulse);
        }

        Vector3 localPositionTouched = positionTouched - meshGestion.transform.position;
        Mesh newMesh = CreateCutedPart(nodeTouched, meshGestion.meshGenerated, nodes, localPositionTouched);

        // We create a new mesh only if we are in play mode
        if (Application.isPlaying)
        {
            // Generate cuted object
            GameObject cutedPart = Instantiate(new GameObject(meshGestion.gameObject.name + "CutedPart"),
                positionTouched, Quaternion.identity);
            cutedPart.AddComponent<MeshRenderer>().material = meshGestion.GetComponent<Renderer>().material;
            newMesh.RecalculateNormals();
            cutedPart.AddComponent<MeshFilter>().mesh = newMesh;
            cutedPart.AddComponent<Rigidbody>().AddForce(direction * forceEjection, ForceMode.Impulse);
            MeshCollider meshCollider = cutedPart.AddComponent<MeshCollider>();
            meshCollider.convex = true;
            MeshGestion newMeshGestion = cutedPart.AddComponent<MeshGestion>();
            newMeshGestion.treeArray = new List<TreeNode>();
            MeshGestion.GenerateNewTreeNodeArray(nodes, newMeshGestion.treeArray, nodeTouched, -1, false);

            meshGestion.meshGenerated.RecalculateBounds();
            meshGestion.GetComponent<MeshFilter>().sharedMesh = meshGestion.meshGenerated;
            meshGestion.GetComponent<MeshCollider>().sharedMesh = meshGestion.meshGenerated;
            MeshCollider newMeshCollider = meshGestion.GetComponent<MeshCollider>();
            newMeshCollider.sharedMesh = meshGestion.meshGenerated;

            Rigidbody rb = meshGestion.GetComponent<Rigidbody>();
            if (rb)
            {
                newMeshCollider.convex = true;
                rb.isKinematic = false;
            }
            // Ignore collision for a short amount of time to avoid the cut part to "bounce" out of original shape
            StartCoroutine(IIgnoreCollision(meshCollider, newMeshCollider));
        }
        else
        {
            // Update original mesh
            /*meshGestion.meshGenerated.RecalculateBounds();
            meshGestion.GetComponent<MeshFilter>().mesh = meshGestion.meshGenerated;
            meshGestion.GetComponent<MeshCollider>().sharedMesh= meshGestion.meshGenerated;*/
        }


    }

    // Return the new mesh created by cutting at the root of the cylinder touched
    private Mesh CreateCutedPart(TreeNode nodeToCut, Mesh originalMesh, List<TreeNode> nodes, Vector3 localPositionTouched)
    {
        Mesh meshCreated = new Mesh();
        List<Vector3> verticesToCut = new List<Vector3>();
        List<int> trianglesToCut = new List<int>();
        List<Color32> colorsToCut = new List<Color32>();
        List<Vector3> originalVertices = originalMesh.vertices.ToList();

        RecursiveCut(verticesToCut, trianglesToCut, colorsToCut, originalMesh, nodes, nodeToCut);
        DeleteCutedPart(originalMesh, trianglesToCut);

        // -- Prepare triangle for new part
        // Fix triangles index
        Dictionary<int, int> verticesOldAndNewIndex = new Dictionary<int, int>();
        for (int i = 0; i < verticesToCut.Count; i++)
        {
            int oldIndex = originalVertices.FindIndex(v => v == verticesToCut[i]);
            if (oldIndex == -1)
            {
                Debug.LogWarning("[VegetationDestruction] A vertice is missing in original mesh ?!");
            }
            else
                verticesOldAndNewIndex.Add(oldIndex, i);
        }

        for (int i = 0; i < trianglesToCut.Count; i++)
        {
            if (verticesOldAndNewIndex.TryGetValue(trianglesToCut[i], out int newIndex))
            {
                trianglesToCut[i] = newIndex;
            }
            else
                throw new Exception("[VegetationDestruction] Missing vertices in cut part");
        }


        if (nodeToCut.parentIndex != -1)
        {
            nodes[nodeToCut.parentIndex].childrenIndex.Remove(nodes.FindIndex(n => n == nodeToCut));
            nodeToCut.parentIndex = -1;
        }

        // Update Vertex position to have a better object center
        for (int i = 0; i < verticesToCut.Count; i++)
        {
            verticesToCut[i] -= localPositionTouched;
        }

        UpdateTreeTrianglesAndVertex(verticesOldAndNewIndex, nodes, nodeToCut, localPositionTouched);

        meshCreated.vertices = verticesToCut.ToArray();
        meshCreated.triangles = trianglesToCut.ToArray();
        meshCreated.colors32 = colorsToCut.ToArray();

        return meshCreated;
    }

    // Remove triangles from original mesh
    private void DeleteCutedPart(Mesh originalMesh, List<int> trianglesToRemove)
    {
        // --- Remove cuted part from original mesh
        List<int> oldTriangles = originalMesh.triangles.ToList();
        List<int> newTriangles = new List<int>();

        // Take triangle needed
        for (int i = 0; i < oldTriangles.Count; i+=3)
        {
            int[] triangleToSearch = new int[]{oldTriangles[i], oldTriangles[i + 1], oldTriangles[i + 2]};
            bool triangleRemoved = false;
            for (int t = 0; t < trianglesToRemove.Count; t += 3)
            {
                int[] actualTriangle = new int[]{trianglesToRemove[t], trianglesToRemove[t + 1], trianglesToRemove[t + 2]};
                if (Enumerable.SequenceEqual(triangleToSearch, actualTriangle))
                {
                    triangleRemoved = true;
                    break;
                }
            }
            if (!triangleRemoved)
                newTriangles.AddRange(triangleToSearch);
        }

        originalMesh.triangles = newTriangles.ToArray();
    }

    // Take all vertices and triangles needed to build the new part cut
    private void RecursiveCut(List<Vector3> vertices, List<int> triangles, List<Color32> colors, Mesh mesh, List<TreeNode> nodes, TreeNode parent)
    {
        if (parent == null)
            return;

        GenerateCylinder.CylinderInfos infos = parent.cylinder;

        int added = 0;
        for (int i = 0; i < infos.vertices.Count; i++)
        {
            bool doubled = false;
            foreach (var v in vertices)
            {
                if (v == infos.vertices[i])
                {
                    doubled = true;
                    break;
                }
            }

            if (!doubled)
            {
                vertices.Add(infos.vertices[i]);
                added++;
            }
        }
        triangles.AddRange(infos.triangles);
        colors.AddRange(Enumerable.Repeat(mesh.colors32[infos.triangles[0]], added));

        foreach (var child in parent.childrenIndex)
        {
            RecursiveCut(vertices, triangles, colors, mesh, nodes, nodes[child]);
        }
    }

    // Search the node parents of the triangle specified
    private TreeNode GetTriangleNode(int[] triangleTouched, List<TreeNode> nodes, TreeNode parent)
    {
        if (parent == null)
            return null;

        if (parent.cylinder != null && parent.cylinder.triangles != null)
        {
            List<int> triangles = parent.cylinder.triangles;
            if (FindTriangleInArray(triangles, triangleTouched) != -1)
            {
                return parent;
            }
        }

        foreach (var child in parent.childrenIndex)
        {
            TreeNode res = GetTriangleNode(triangleTouched, nodes, nodes[child]);
            if (res != null)
                return res;
        }

        return null;
    }

    // Update triangles index in the specified tree to match new vertices index
    private void UpdateTreeTrianglesAndVertex(Dictionary<int, int> verticesOldAndNewIndex, List<TreeNode> treeArray, TreeNode parent, Vector3 toRemoveOnVertex)
    {
        if (parent == null)
            return;

        List<int> trianglesNode = parent.cylinder.triangles;


        for (int i = 0; i < trianglesNode.Count; i++)
        {
            if (verticesOldAndNewIndex.TryGetValue(trianglesNode[i], out int newIndex))
            {
                trianglesNode[i] = newIndex;
            }
            else
                throw new Exception("[VegetationDestruction] Missing vertices in cut part");
        }

        List<Vector3> vertices = parent.cylinder.vertices;
        for (int i = 0; i < vertices.Count; i++)
        {
            vertices[i] -= toRemoveOnVertex;
        }

        for(int i = 0; i < parent.childrenIndex.Count; i ++)
        {
            int oldIndex = parent.childrenIndex[i];
            UpdateTreeTrianglesAndVertex(verticesOldAndNewIndex, treeArray, treeArray[oldIndex], toRemoveOnVertex);
        }
    }

    // Search for a triangle int[3] in an array of triangles int[3 * x]
    private int FindTriangleInArray(List<int> triangles, int[] triangleToSearch)
    {
        for (int t = 0; t < triangles.Count; t += 3)
        {
            if (triangles[t] == triangleToSearch[0] && triangles[t + 1] == triangleToSearch[1] &&
                triangles[t + 2] == triangleToSearch[2])
            {
                return t;
            }
        }

        return -1;
    }

    // Ignore collision between two collider during timeIgnoreCollision
    IEnumerator IIgnoreCollision(Collider a, Collider b)
    {
        Physics.IgnoreCollision(a, b, true);
        yield return new WaitForSeconds(timeIgnoreCollision);
        Physics.IgnoreCollision(a, b, false);
    }
}