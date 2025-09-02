using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

[ExecuteInEditMode]
public class MarchingCubesInterpolation : MonoBehaviour
{
    [Header("Environment")] 
    //This info will be used to create the location for the cubes to be created in
    //Used to generate the initial grid of points
    public int envWidth;
    public int envHeight;
    public int envLength;
    public Vector3 envPos;
    [Range(0.5f, 50f)]
    public float size;

    public Cube[,,] cubeArr;

    
    [Header("Noise")] 
    [SerializeField]
    private float noiseScale = .05f;
    [Range(0.0f, 1f)]
    public float surfaceLevel;

    public bool noiseIs3D;
    
    [Header("Gizmos")] 
    public bool drawCubes;
    public bool drawVertices;
    public bool drawEdges;
    public bool drawFaces;
    [Range(0.1f, 1f)] public float pointsSize;

    [Header("Mesh Colour")] public bool colourMesh;
    public Gradient colourGradient;
    private float minTerrainHeight;
    private float maxTerrainHeight;

    
    [Header(("Data"))]    public TriangularData triData;
    public Mesh mesh;
    public MeshFilter meshFilter;

    public bool autoUpdateMesh;

    public MeshCollider meshCollider;
    public bool updateCollider;
    
    // Start is called before the first frame update
    void Start()
    {
        cubeArr = GenerateCubes();
        mesh = GenerateMesh();
        UpdateMesh();
    }

    // Update is called once per frame
    void Update()
    {
        if (autoUpdateMesh)
        {
            cubeArr = GenerateCubes();
            mesh = GenerateMesh();
            UpdateMesh();
        }
        

    }

    private void OnDrawGizmos()
    {
        
        Gizmos.DrawWireCube(new Vector3(envPos.y + size * envLength * 0.5f, envPos.y + size * envLength * 0.5f, envPos.y + (size * (envLength - 1)) * 0.5f ), Vector3.one * size * envLength);

        if (cubeArr != null)
        {
            for (int x = 0; x < cubeArr.GetLength(0); x++)
            {
                for (int y = 0; y < cubeArr.GetLength(1); y++)
                {
                    for (int z = 0; z < cubeArr.GetLength(2); z++)
                    {
                        Cube currCube = cubeArr[x, y, z];

                        if (drawCubes)
                        {
                            bool vertexIsActice = false;
                            for (int i = 0; i < currCube.vertices.Length; i++)
                            {
                                Vertex currVertex = currCube.vertices[i];

                                if (currVertex.noiseValue < surfaceLevel) vertexIsActice = true;
                            }

                            if (vertexIsActice)
                            {
                                Gizmos.color = new Color(1, 1, 1, 1);
                                Gizmos.DrawWireCube(currCube.worldPos, new Vector3(currCube.size, currCube.size, currCube.size));
                            }

                        }

                        if (drawVertices)
                        {
                            for (int i = 0; i < currCube.vertices.Length; i++)
                            {
                                Vertex currVertex = currCube.vertices[i];

                                if (currVertex.noiseValue > surfaceLevel)
                                {
                                    Gizmos.color = new Color(1,1,1, 1);
                                    Gizmos.DrawSphere(currVertex.worldPos, pointsSize);
                                }
                                

                            }
                        }
                    }
                }
            }
        }
    }

    public void  UpdateMesh()
    {
        meshFilter.mesh = mesh;
        if(updateCollider)        meshCollider.sharedMesh = mesh;


    }

    public Mesh GenerateMesh()
    {
        List<Vector3> meshVertices = new List<Vector3>();


        for (int x = 0; x < cubeArr.GetLength(0); x++)
        {
            for (int y = 0; y < cubeArr.GetLength(1); y++)
            {
                for (int z = 0; z < cubeArr.GetLength(2); z++)
                {
                    Cube targetCube = cubeArr[x, y, z];

                    int cubeIndex = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        if (targetCube.vertices[i].noiseValue < surfaceLevel)
                        {
                            cubeIndex += 1 << i;
                            
             
                        }

                        if (targetCube.vertices[i].noiseValue > surfaceLevel)
                        {
                            if(targetCube.vertices[i].worldPos.y > maxTerrainHeight) maxTerrainHeight = targetCube.vertices[i].worldPos.y;
                            if(targetCube.vertices[i].worldPos.y < minTerrainHeight) minTerrainHeight = targetCube.vertices[i].worldPos.y;
                        }
                    }

                    Vertex[] vertices = targetCube.vertices;

                    for (int i = 0; i < 16; i++)
                    {
                        int currPoint = triData.triTable[cubeIndex, i];
                        if (currPoint == -1) break;

                        if (currPoint == 0)
                        {
                            Vector3 newPoint = InterpolatePoints(surfaceLevel, vertices[0], vertices[1]);
                            meshVertices.Add(newPoint);
                        }
                        if (currPoint == 1) meshVertices.Add(InterpolatePoints(surfaceLevel, vertices[1], vertices[2]));
                        if (currPoint == 2) meshVertices.Add(InterpolatePoints(surfaceLevel, vertices[2], vertices[3]));
                        if (currPoint == 3) meshVertices.Add(InterpolatePoints(surfaceLevel, vertices[3], vertices[0]));
                        if (currPoint == 4) meshVertices.Add(InterpolatePoints(surfaceLevel, vertices[4], vertices[5]));
                        if (currPoint == 5) meshVertices.Add(InterpolatePoints(surfaceLevel, vertices[5], vertices[6]));
                        if (currPoint == 6) meshVertices.Add(InterpolatePoints(surfaceLevel, vertices[6], vertices[7]));
                        if (currPoint == 7) meshVertices.Add(InterpolatePoints(surfaceLevel, vertices[7], vertices[4]));
                        if (currPoint == 8) meshVertices.Add(InterpolatePoints(surfaceLevel, vertices[0], vertices[4]));
                        if (currPoint == 9) meshVertices.Add(InterpolatePoints(surfaceLevel, vertices[1], vertices[5]));
                        if (currPoint == 10)
                            meshVertices.Add(InterpolatePoints(surfaceLevel, vertices[2], vertices[6]));
                        if (currPoint == 11)
                            meshVertices.Add(InterpolatePoints(surfaceLevel, vertices[3], vertices[7]));

                    }
                    
                    

                }
            }
        }

        Mesh m = new Mesh();
        m.vertices = meshVertices.ToArray();

        
        Color[] colours = new Color[meshVertices.Count];
        for (int i = 0; i < colours.Length; i++)
        {
            float height = Mathf.InverseLerp(minTerrainHeight, maxTerrainHeight ,meshVertices[i].y);
            colours[i] = colourGradient.Evaluate(height);

        }

        int[] triangles = new int[meshVertices.Count];

        if (noiseIs3D)
        {
            for (int i = 0; i < triangles.Length; i++)
            {
                triangles[i] = i;
            }
        }
        else
        {          
            for (int i = 0; i < triangles.Length; i++)
            {
                triangles[i] = (triangles.Length - 1) - i;
            }
        }

        m.colors = colours;
        m.triangles = triangles;
        m.RecalculateNormals();

        return m;
    }
    
    public Cube[,,] GenerateCubes()
    {

        Cube[,,] cubeArr = new Cube[envWidth  , envHeight, envLength ];
        
        for (int x = 0; x < envWidth; x++)
        {
            for (int y = 0; y < envHeight; y++)
            {
                for (int z = 0; z < envLength; z++)
                {
                    //Generate cube for each point
                    Vertex cubePosition = new Vertex(new Vector3(x,y,z), new Vector3(x * size + envPos.x, y * size + envPos.y, z * size + envPos.z), noiseScale);
                    Cube newCube = new Cube(cubePosition.relativePos, cubePosition.worldPos, size, noiseScale, noiseIs3D);
                    cubeArr[x, y, z] = newCube;
                }
            }
        }
        
        return cubeArr;
    }
    
    public Vector3 InterpolatePoints(float surfaceLevel, Vertex vertex1, Vertex vertex2)
    {
        Vector3 pointOnEdge = Vector3.zero;

        if (Mathf.Approximately(surfaceLevel - vertex1.noiseValue, 0) == true) return vertex1.worldPos;
        if (Mathf.Approximately(surfaceLevel - vertex2.noiseValue, 0) == true) return vertex2.worldPos;
        if (Mathf.Approximately(vertex1.noiseValue - vertex2.noiseValue, 0) == true) return vertex1.worldPos;

        float mu = (surfaceLevel - vertex1.noiseValue) / (vertex2.noiseValue - vertex1.noiseValue);
        pointOnEdge.x = vertex1.worldPos.x + mu * (vertex2.worldPos.x - vertex1.worldPos.x);
        pointOnEdge.y = vertex1.worldPos.y + mu * (vertex2.worldPos.y - vertex1.worldPos.y);
        pointOnEdge.z = vertex1.worldPos.z + mu * (vertex2.worldPos.z - vertex1.worldPos.z);

        return pointOnEdge;
    }

}
