using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawCube : MonoBehaviour
{
    [Header("Cube Properties")] public float size;
    public Vector3 cubePos;

    [Header("Noise")] 

    [Range(0f, 50f)]
    public float noiseScale;
    
    [Range(0f, 1f)]
    public float threshold;

    [Header("Active Vertices")] [SerializeField]
    public bool[] activeVertices = new bool[8];

    public Cube currentCube;

    [Header("Draw Bools")] 

    public bool drawEdgePoints;
    [Range(0f, 1f)] public float edgePointSize;
    [Space(10)]

    public bool drawVertices;
    [Range(0f, 1f)] public float vertexSize;
    [Space(10)]

    public bool drawFaces;
    public bool drawEdges;


    public TriangularData triData;
    
    private Mesh mesh;
    private MeshFilter meshFilter;
    
    // Start is called before the first frame update
    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        
    }

    private void OnDrawGizmos()
    {

        if (currentCube != null)
        {
            if (drawEdges)
            {
                Gizmos.color = new Color(1, 0, 0, 1);
                Gizmos.DrawWireCube(currentCube.worldPos, new Vector3(currentCube.size, currentCube.size, currentCube.size));
            }

            if (drawVertices)
            {
                for (int i = 0; i < currentCube.vertices.Length; i++)
                {
                    Vertex currVertex = currentCube.vertices[i];

                    //Noise value is not extreeme enough to 
                    /*if (currVertex.noiseValue > threshold)
                    {
                        Gizmos.color = new Color(currVertex.noiseValue, currVertex.noiseValue, currVertex.noiseValue, 1);
                        Gizmos.DrawSphere(currVertex.worldPos, vertexSize);
                    }*/

                    if (activeVertices[i]) Gizmos.color = new Color(1, 1, 1, 1);
                    else Gizmos.color = new Color(0, 0, 0, 1);
                    Gizmos.DrawSphere(currVertex.worldPos, vertexSize);


                }
            }

            if (drawEdgePoints)
            {
                for (int i = 0; i < currentCube.edgePoints.Length; i++)
                {
                    Vertex currPoint = currentCube.edgePoints[i];
                    Gizmos.color = new Color(0, 0, 1, 1);
                    Gizmos.DrawSphere(currPoint.worldPos, edgePointSize);
                }
            }

            if (drawFaces)
            {
                mesh = GenerateMesh();
                meshFilter.mesh = mesh;
            }
        }
        
    }

    private Mesh GenerateMesh()
    {
        //Gets the correct edge state form the tri table
        
        int cubeIndex = 0;
        for (int i = 0; i < 8; i++)
        {
            if (activeVertices[i])
            {
                cubeIndex += 1 << i;
            }
        }

        string edgeString = "";
        for (int i = 0; i < 16; i++)
        {
            edgeString += " " + triData.triTable[cubeIndex, i];
        }
        
        
        Debug.Log("Cube Index: " + cubeIndex + " | Edge Data: " + edgeString);
        

        List<Vector3> meshVertices = new List<Vector3>();

        for (int i = 0; i < 16; i += 3)
        {
            int p1 = triData.triTable[cubeIndex, i];
            int p2 = triData.triTable[cubeIndex, i + 1];
            int p3 = triData.triTable[cubeIndex, i + 2];
            
            if(p1 == -1) break;
            
            meshVertices.Add(currentCube.edgePoints[p1].worldPos);
            meshVertices.Add(currentCube.edgePoints[p2].worldPos);
            meshVertices.Add(currentCube.edgePoints[p3].worldPos);
        }

        Mesh m = new Mesh();
        
        m.vertices = meshVertices.ToArray();

        int[] triangles = new int[meshVertices.Count];
        for (int i = 0; i < triangles.Length; i++)
        {
            triangles[i] = i;
        }

        m.triangles = triangles;
  
        

        
        
        return m;
    }

    // Update is called once per frame
    void Update()
    {
        currentCube = new Cube(new Vector3(0, 0, 0), new Vector3(cubePos.x, cubePos.y, cubePos.z), size, noiseScale, true);
    }
}
