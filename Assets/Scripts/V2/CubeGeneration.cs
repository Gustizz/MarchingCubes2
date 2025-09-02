using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class Vertex
{

    public Vector3 relativePos;
    public Vector3 worldPos;
    public float noiseValue;

    public Vertex(Vector3 relativePos,Vector3 worldPos, float noiseValue)
    {
        this.relativePos = relativePos;
        this.worldPos = worldPos;
        this.noiseValue = noiseValue;
    }
}



public class CubeGeneration : MonoBehaviour
{
    
    //Generate a 3D grid of points. Those points will be the centre of cubes. Using those points and a cubeSize variable the 8 vertices can be generated
    //1) Generate 3D grid of points
    //2) Generate 3D grid of cubes with central points
    //3) Calcualte the vertices for each point

    [Header("Environment")] 
    //This info will be used to create the location for the cubes to be created in
    //Used to generate the initial grid of points
    public int envWidth;
    public int envHeight;
    public int envLength;
    public Vector3 envPos;
    //amountOfPoints: the amount of segments a side of the env will be split into. (envWidth / amountofPoints) = amountOfpoints on that side / Resoultion
    [Tooltip("Amount of points per cube side")]
    [Range(0.5f, 50f)]
    public float amountOfPoints;
    public Vertex[,,] pointsArr;

    [Header("Noise")] 
    [SerializeField]
    private float noiseScale = .05f;
    [Range(0.0f, 1f)]
    public float maxThreshold;
    [Range(0.0f, 1f)]
    public float minThreshold = 0.5f;

    [Header("Gizmos")] public bool drawPoints;
    public bool drawCubes;
    public bool drawVertices;
    public bool drawEdges;
    public bool drawFaces;
    [Range(0.1f, 1f)] public float pointsSize;

    [Header("Cubes")] public Cube[,,] cubeArr;
    
    [Header(("Data"))]    public TriangularData triData;
    public Mesh mesh;
    public MeshFilter meshFilter;

    public bool interpolate;



    //Choose coordinate to highlight cube
    public Vector3 cubeToDraw;
    
    void Start()
    {
        //1) Generate 3D grid of points
        pointsArr = GeneratePoints();
        cubeArr = GenerateCubes();
    }

    private float lastMaxThreshold;
    private float lastMinThreshold;
    private float lastNoiseValue;
    void Update()
    {
        pointsArr = GeneratePoints();
        cubeArr = GenerateCubes();


        if (lastMaxThreshold != maxThreshold || lastMinThreshold != minThreshold || lastNoiseValue != lastNoiseValue)
        {
            CreateMesh();
        }
        
        lastMaxThreshold = maxThreshold;
        lastMinThreshold = minThreshold;
        lastNoiseValue = noiseScale;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 0, 1);
        Gizmos.DrawWireCube(envPos, new Vector3(envWidth, envHeight, envLength));
        
        if (drawPoints && (pointsArr != null))
        {
            for (int x = 0; x < pointsArr.GetLength(0); x++)
            {
                for (int y = 0; y < pointsArr.GetLength(1); y++)
                {
                    for (int z = 0; z < pointsArr.GetLength(2); z++)
                    {

                        Vertex currPoint = pointsArr[x, y, z];
                        
                        Gizmos.color = new Color(1,1,1, 1);
                        Gizmos.DrawSphere(currPoint.worldPos, pointsSize);

                    }
                }
            }
        }



        if (cubeArr != null)
        {
            Cube targetCube = cubeArr[0,0,0];
            
            for (int x = 0; x < cubeArr.GetLength(0); x++)
            {
                for (int y = 0; y < cubeArr.GetLength(1); y++)
                {
                    for (int z = 0; z < cubeArr.GetLength(2); z++)
                    {
                        Cube currCube = cubeArr[x, y, z];

                        //Draw Cubes
                        if (drawCubes)
                        {
                            Gizmos.color = new Color(1,1,1, 1);

                            //If cubeToDraw
                            if (x == cubeToDraw.x && y == cubeToDraw.y && z == cubeToDraw.z)
                            {
                                targetCube = currCube;
                                Gizmos.color = new Color(0,1,0, 1);
                            }
                            Gizmos.DrawWireCube(currCube.worldPos, new Vector3(currCube.size, currCube.size, currCube.size));
                        }
                        
                        //Draw Vertices
                        if (drawVertices && currCube.vertices != null)
                        {
                 
                            for (int i = 0; i < currCube.vertices.Length; i++)
                            {
                                Vertex vertex = currCube.vertices[i];
                                Gizmos.color = new Color( vertex.noiseValue, vertex.noiseValue, vertex.noiseValue, 1);

                                if (vertex.noiseValue > minThreshold)
                                {
                                    if (vertex.noiseValue < maxThreshold) Gizmos.DrawSphere(vertex.worldPos, pointsSize * 0.5f);

                                }
                                
                                
                                //If cubeToDraw
                                if(x == cubeToDraw.x && y == cubeToDraw.y && z == cubeToDraw.z)   Handles.Label(vertex.worldPos, "index: " + i.ToString());
                                
                            }
                        }
                        
                        //Draw Edges
                        if (drawEdges && currCube.edgePoints != null)
                        {
                            for (int i = 0; i < currCube.edgePoints.Length; i++)
                            {
                                Vertex edgePoint = currCube.edgePoints[i];
                                Gizmos.color = new Color(0, 0, 1, 1);

                                Gizmos.DrawSphere(edgePoint.worldPos, pointsSize * 0.25f);
                                if (x == cubeToDraw.x && y == cubeToDraw.y && z == cubeToDraw.z)
                                    Handles.Label(edgePoint.worldPos, "index: " + i.ToString());
                            }
                        }
                        
                        if(drawFaces){     
                            mesh = CreateMesh();
                            meshFilter.mesh = mesh;
                        }


                    }
                }
            }
            
    
        }
        
        
        

    }
    
    public Mesh CreateMesh()
    {
        //Test to draw faces for singular cube
        //Get active points
        //They correspond to a binary value
        //EG vertices 7 5 1 are active (below the threshold)
        //Then we take the binary value 010100010 (7 5 1 correspond to the indexes of the 1's)
        //Turn binary value to denary
        //Search the denary number in the trig table
        //The trig table will result in {5, 0, 1, 5, 4, 0, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1},
        // This respond to edges
        
        //Loop through every cube
        //Draw faces for each cube
        Mesh m = new Mesh();
        List<Vector3> meshVertices = new List<Vector3>();

        
        for (int x = 0; x < cubeArr.GetLength(0); x++)
        {
            for (int y = 0; y < cubeArr.GetLength(1); y++)
            {
                for (int z = 0; z < cubeArr.GetLength(2); z++)
                {
                    Cube targetCube = cubeArr[x,y,z];
                    
                    int cubeIndex = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        if (targetCube.vertices[i].noiseValue > minThreshold)
                        {
                            if (targetCube.vertices[i].noiseValue < maxThreshold)
                            {
                                cubeIndex += 1 << i;
                            }
                        }
                        

                    }

                    if (interpolate)
                    {
                        
                        
                        Vertex[] vertices = targetCube.vertices;
                        
                        for (int i = 0; i < 16; i++)
                        {
                            int currPoint = triData.triTable[cubeIndex, i];
                            if(currPoint == -1) break;

                            // y = y0 + (Value - x0) * (y1 - y0 / x1 - x0)
                            
                            Vector3 currentEdgePointPos = targetCube.edgePoints[currPoint].worldPos;
                            switch (currPoint)
                            {
                                case 0:
                                    meshVertices.Add(InterpolatePoints(maxThreshold, vertices[0], vertices[1]));
                                    //Debug.Log("0");
                                    //meshVertices.Add( new Vector3( Mathf.Lerp(vertices[0].worldPos.x, vertices[1].worldPos.x, (vertices[0].noiseValue + vertices[1].noiseValue) * 0.5f), currentEdgePointPos.y, currentEdgePointPos.z));
                                    break;
                                case 1:
                                    Debug.Log("1");
                                    meshVertices.Add(InterpolatePoints(maxThreshold, vertices[1], vertices[2]));

                                   // meshVertices.Add( new Vector3( currentEdgePointPos.x, currentEdgePointPos.y, Mathf.Lerp(vertices[1].worldPos.z, vertices[2].worldPos.z, (vertices[1].noiseValue + vertices[2].noiseValue) * 0.5f)));
                                    break;
                                case 2:
                                    Debug.Log("2");
                                    meshVertices.Add(InterpolatePoints(maxThreshold, vertices[2], vertices[3]));

                                    // meshVertices.Add( new Vector3( Mathf.Lerp(vertices[2].worldPos.x, vertices[3].worldPos.x, (vertices[2].noiseValue + vertices[3].noiseValue) * 0.5f), currentEdgePointPos.y, currentEdgePointPos.z));
                                    break;
                                case 3:
                                    Debug.Log("3");
                                    meshVertices.Add(InterpolatePoints(maxThreshold, vertices[3], vertices[0]));

                                    // meshVertices.Add( new Vector3( currentEdgePointPos.x, currentEdgePointPos.y, Mathf.Lerp(vertices[3].worldPos.z, vertices[0].worldPos.z, (vertices[3].noiseValue + vertices[0].noiseValue) * 0.5f)));
                                    break;
                                case 4:
                                    meshVertices.Add(InterpolatePoints(maxThreshold, vertices[4], vertices[5]));

                                    // meshVertices.Add( new Vector3( Mathf.Lerp(vertices[4].worldPos.x, vertices[5].worldPos.x, (vertices[4].noiseValue + vertices[5].noiseValue) * 0.5f), currentEdgePointPos.y, currentEdgePointPos.z));
                                    break;
                                case 5:
                                    meshVertices.Add(InterpolatePoints(maxThreshold, vertices[5], vertices[6]));

                                    //  meshVertices.Add( new Vector3( currentEdgePointPos.x, currentEdgePointPos.y, Mathf.Lerp(vertices[5].worldPos.z, vertices[6].worldPos.z, (vertices[5].noiseValue + vertices[6].noiseValue) * 0.5f)));
                                    break;
                                case 6:
                                    meshVertices.Add(InterpolatePoints(maxThreshold, vertices[6], vertices[7]));

                                    //  meshVertices.Add( new Vector3( Mathf.Lerp(vertices[6].worldPos.x, vertices[7].worldPos.x, (vertices[6].noiseValue + vertices[7].noiseValue) * 0.5f), currentEdgePointPos.y, currentEdgePointPos.z));
                                    break;
                                case 7:
                                    meshVertices.Add(InterpolatePoints(maxThreshold, vertices[7], vertices[4]));

                                    //   meshVertices.Add( new Vector3( currentEdgePointPos.x, currentEdgePointPos.y, Mathf.Lerp(vertices[7].worldPos.z, vertices[4].worldPos.z, (vertices[7].noiseValue + vertices[4].noiseValue) * 0.5f)));
                                    break;
                                case 8:
                                    meshVertices.Add(InterpolatePoints(maxThreshold, vertices[0], vertices[4]));

                                    //   meshVertices.Add( new Vector3( currentEdgePointPos.x, Mathf.Lerp(vertices[0].worldPos.z, vertices[4].worldPos.z, (vertices[0].noiseValue + vertices[4].noiseValue) * 0.5f), currentEdgePointPos.z));
                                    break;
                                case 9:
                                    meshVertices.Add(InterpolatePoints(maxThreshold, vertices[1], vertices[5]));

                                    //   meshVertices.Add( new Vector3( currentEdgePointPos.x, Mathf.Lerp(vertices[1].worldPos.z, vertices[5].worldPos.z, (vertices[1].noiseValue + vertices[5].noiseValue) * 0.5f), currentEdgePointPos.z));
                                    break;
                                case 10:
                                    meshVertices.Add(InterpolatePoints(maxThreshold, vertices[2], vertices[6]));

                                    //  meshVertices.Add( new Vector3( currentEdgePointPos.x, Mathf.Lerp(vertices[2].worldPos.z, vertices[6].worldPos.z, (vertices[2].noiseValue + vertices[6].noiseValue) * 0.5f), currentEdgePointPos.z));
                                    break;
                                case 11:
                                    meshVertices.Add(InterpolatePoints(maxThreshold, vertices[3], vertices[7]));

                                    //  meshVertices.Add( new Vector3( currentEdgePointPos.x, Mathf.Lerp(vertices[3].worldPos.z, vertices[7].worldPos.z, (vertices[3].noiseValue + vertices[7].noiseValue) * 0.5f), currentEdgePointPos.z));
                                    break;
                                    
                            }
                            
                            //edge 0: between vertices 0 and 1
                            //edge 1: betweem vertices 1 and 2
                            //edge 2: betweem vertices 2 and 3
                            //edge 3: betweem vertices 3 and 0
                            //edge 4: betweem vertices 4 and 5
                            //edge 5: betweem vertices 5 and 6
                            //edge 6: betweem vertices 6 and 7
                            //edge 7: betweem vertices 7 and 4
                            //edge 8: betweem vertices 0 and 4
                            //edge 9: betweem vertices 1 and 5
                            //edge 10: betweem vertices 2 and 6
                            //edge 11: betweem vertices 3 and 7
                        }


                    }
                    
                    
                    for (int i = 0; i < 15; i += 3)
                    {
                        //Debug.Log("INDEX: " + i + " | CUBE INDEX: " + cubeIndex);
            
                        int p1 = triData.triTable[cubeIndex, i];
                        int p2 = triData.triTable[cubeIndex, i + 1];
                        int p3 = triData.triTable[cubeIndex, i + 2];
            
                        if(p1 == -1) break;




                        if (!interpolate)
                        {
                            meshVertices.Add(targetCube.edgePoints[p1].worldPos);
                            meshVertices.Add(targetCube.edgePoints[p2].worldPos);
                            meshVertices.Add(targetCube.edgePoints[p3].worldPos);
                        }
                        else
                        {



                        }

                    }

                }
            }
        }
        
        
        
        




     /*   string edgeString = "";
        List<int> edgeList = new List<int>();
        for (int i = 0; i < 16; i ++)
        {


            edgeString += " " + triData.triTable[cubeIndex, i];
                edgeList.Add(triData.triTable[cubeIndex, i]);
        }
        Debug.Log("EDGE LIST: " + edgeString);*/



        m.vertices = meshVertices.ToArray();
        
        int[] triangles = new int[meshVertices.Count];
        for (int i = 0; i < triangles.Length; i++)
        {
            triangles[i] = i;
        }

        m.triangles = triangles;

        mesh = m;
        
        mesh.RecalculateNormals();
        
        meshFilter.mesh = mesh;
        
        return m;
    }

    private Vector3 InterpolatePoints(float surfaceLevel, Vertex v1, Vertex v2)
    {
        Vector3 pointsOnEdge = Vector3.zero;

        if (Mathf.Approximately(surfaceLevel - v1.noiseValue, 0) == true) return v1.worldPos;
        if (Mathf.Approximately(surfaceLevel - v2.noiseValue, 0) == true) return v2.worldPos;
        if (Mathf.Approximately(v1.noiseValue - v2.noiseValue, 0) == true) return v1.worldPos;

        float mu = (surfaceLevel - v1.noiseValue) / (v2.noiseValue - v1.noiseValue);

        pointsOnEdge.x = v1.worldPos.x + mu * (v2.worldPos.x - v1.worldPos.x); 
        pointsOnEdge.y = v1.worldPos.y + mu * (v2.worldPos.y - v1.worldPos.y); 
        pointsOnEdge.z = v1.worldPos.z + mu * (v2.worldPos.z - v1.worldPos.z); 

        
        return pointsOnEdge;
    }
    public Vertex[,,] GeneratePoints()
    {
        //Generates a 3d Grid of points
        //These will be used as the centres for the cubes
        
        //Amount of points
        float pointWidth = envWidth / amountOfPoints;
        float pointHeight = envHeight / amountOfPoints;
        float pointLength = envLength / amountOfPoints;

        Vertex[,,] pointsArr = new Vertex[Round(pointWidth), Round(pointHeight), Round(pointLength)];
        
        for (int y = 0; y < Round(pointHeight); y++)
        {
            for (int x = 0; x < Round(pointWidth); x++)
            {
                for (int z = 0; z <  Round(pointLength); z++)
                {
                    Vertex newPoint = new Vertex(new Vector3(x, y, z),
                        new Vector3((x * amountOfPoints) - (Round(pointWidth) * 0.5f) + envPos.x, (y * amountOfPoints) - (Round(pointHeight)* 0.5f) + envPos.y, (z * amountOfPoints) - (Round(pointLength) * 0.5f)+ envPos.z) , 0);

                    pointsArr[x, y, z] = newPoint;


                }
            }
        }

        return pointsArr;
    }

    public Cube[,,] GenerateCubes()
    {
        //Generate cubes from points
        //Size of cube will amount of points

        Cube[,,] cubeArr = new Cube[pointsArr.GetLength(0) , pointsArr.GetLength(1) , pointsArr.GetLength(2) ];
        
        for (int x = 0; x < pointsArr.GetLength(0); x++)
        {
            for (int y = 0; y < pointsArr.GetLength(1); y++)
            {
                for (int z = 0; z < pointsArr.GetLength(2); z++)
                {
                    //Generate cube for each point

                    Vertex currPoint = pointsArr[x, y, z];

                    Cube newCube = new Cube(currPoint.relativePos, currPoint.worldPos, amountOfPoints, noiseScale, true);
                    //newCube.GenerateVertices(noiseScale);

                    cubeArr[x, y, z] = newCube;


                }
            }
        }

        return cubeArr;
    }
    

    
    private int Round(float num)
    {
        return (int)Math.Round(num);
    }
}
