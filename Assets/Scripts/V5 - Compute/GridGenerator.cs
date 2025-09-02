using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public struct Triangle
{
    public Vector3 vectorA;
    public Vector3 vectorB;
    public Vector3 vectorC;

}

public class GridGenerator : MonoBehaviour
{
    [Header("Chunk data")]
    public int size;
    public Vector3 pos;
    public float yOffset;
    [Range(2f, 100f)]
    public int numOfPointsPerAxis;

    public Vector3 centre;
    
    private ComputeBuffer pointsBuffer;
    private ComputeBuffer offsetBuffer;
    private ComputeBuffer triangleBuffer;
    private ComputeBuffer triCountBuffer;

    private int threadGroupSize = 8;
    
    public ComputeShader pointGenerationShader;
    public ComputeShader marchingCubesShader;
    private Vector4[] points;
    private Triangle[] triangles;

    private bool canDraw = false;
    public int worldSize = 2;
    public bool drawVertices;
    public bool drawCubes;


    [Header("Noise")]
    public int seed = 5;
    public int numOctaves = 4;
    public float lacunarity = 2;
    public float persistence = .5f;
    public float noiseScale = 1;
    public float noiseWeight = 1;
    public bool closeEdges;
    public float floorOffset = 1;
    public float weightMultiplier = 1;
    public float hardFloorHeight;
    public float hardFloorWeight;

    public Vector4 shaderParams;

    [Range(0f,1f)]
    public float surfaceLevel;
    

    [Header("Mesh")]
    public Mesh mesh;
    public MeshFilter meshFilter;
    private void Start()
    {
        GeneratePos();
    }
    

    public void OnSpawn(int size, Vector3 pos, int numberOfPointsPerAxis, Vector3 centre, ComputeShader pointsShader)
    {
        //this.size = size;
        //this.pos = pos;
        //this.numOfPointsPerAxis = numberOfPointsPerAxis;
        //this.centre = centre;
        //this.pointGenerationShader = pointsShader;
        
        GeneratePos();
    }
    
    private void OnDrawGizmos()
    {
        if (canDraw)
        {
            float pointSpacing = size / (numOfPointsPerAxis - 1);

            Gizmos.DrawWireCube(pos * size , size * Vector3.one);

            if (drawVertices)
            {
                int index = 0;
                foreach (Vector4 point in points)
                {
                    Gizmos.color = new Color(1, 1, 1,  point.w);

                
                    if (point.w > surfaceLevel)
                    {

   
                    
                        Gizmos.DrawSphere(new Vector3(point.x, point.y, point.z), 0.1f);
                        Handles.Label(new Vector3(point.x, point.y, point.z), "( " + point.x + ", " + point.y + " , " + point.z + " )");
                        index++;

                    }
                    else{}
                
                    //Gizmos.DrawSphere(new Vector3(point.x + (pos.x * size ) , point.y + (pos.y * size ) , point.z + (pos.z * size)), 0.1f);
                }
            }

            if (drawCubes)
            {
                for (int i = 0; i < points.Length; i++)
                {
                    Vector4 point = points[i];

                    Vector3 pointWorldPos = new Vector3(point.x * size, point.y * size, point.z * size);
 
                    if (point.x * (pos.x + 1) + 3 >= numOfPointsPerAxis || point.y * (pos.y + 1) + 3  >= numOfPointsPerAxis || point.z * (pos.z + 1) + 3  >= numOfPointsPerAxis) {
                        continue;
                    }
                    else
                    {

                        if (point.w > surfaceLevel)
                        {
                            Gizmos.color = new Color(1, 1, 1,  1);

                            Gizmos.DrawCube(new Vector3(point.x +(pointSpacing * 0.5f), point.y +(pointSpacing * 0.5f), point.z +(pointSpacing * 0.5f)), Vector3.one * pointSpacing);

                        }

                    }
             
                    if (point.w > surfaceLevel)
                    {
                    
                        // Gizmos.DrawCube(new Vector3((point.x + points[i + 1].x) * 0.5f,(point.y + points[i + 1].y) * 0.5f, (point.z + points[i + 1].z) * 0.5f), Vector3.one * (point.x + points[i + 1].x) * 0.25f);


                    }
                
                }
            }
            



        }
    }
    
    public int IndexFromCoord(int x, int y, int z)
    {
        return z * numOfPointsPerAxis * numOfPointsPerAxis + y * numOfPointsPerAxis + x;
    }

    public void GenerateMesh()
    {
        int numVoxelsPerAxis = numOfPointsPerAxis - 1;
        int numThreadsPerAxis = Mathf.CeilToInt (numVoxelsPerAxis / (float) threadGroupSize);
        float pointSpacing = size / (numOfPointsPerAxis - 1);

        triangleBuffer = CreateTriangleBuffer();
        triangleBuffer.SetCounterValue(0);


        marchingCubesShader.SetBuffer (0, "points", pointsBuffer);
        marchingCubesShader.SetBuffer (0, "triangles", triangleBuffer);
        marchingCubesShader.SetInt ("numOfPointsPerAxis", numOfPointsPerAxis);
        marchingCubesShader.SetFloat ("surfaceLevel", surfaceLevel);
        
        marchingCubesShader.Dispatch (0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);
        
        // Get number of triangles in the triangle buffer
        ComputeBuffer.CopyCount (triangleBuffer, triCountBuffer, 0);
        int[] triCountArray = { 0 };
        triCountBuffer.GetData (triCountArray);

        int numTris = triCountArray[0];
        
        //Get triangles from compute shader
        triangles = new Triangle[numTris];
        triangleBuffer.GetData (triangles, 0, 0, numTris);

        if(triangles != null) Debug.Log("TRIANGLES EXIST: TRAINGLE COUNT: " + triangles.Length);
        else Debug.Log("TRIAGNLES DO NOT EXITS!");

        
        foreach (Triangle tri in triangles)
        {
            Debug.Log("V1: ( " + tri.vectorA.x + " , " + tri.vectorA.y + " , "  + tri.vectorA.z);
        }
        
        /*
        
        Vector3[] meshVertices = new Vector3[numTris * 3];
        for (int i = 0; i < triangles.Length; i++)
        {
            meshVertices[(i * 3) ] = triangles[i].vectorA;
            meshVertices[(i * 3) + 1] = triangles[i].vectorB;
            meshVertices[(i * 3) + 2] = triangles[i].vectorC;
        }
        
        
        int[] meshTriangles = new int[numTris * 3];
        for (int i = 0; i < meshTriangles.Length; i++)
        {
            meshTriangles[i] = (meshTriangles.Length - 1) - i;
        }*/

        var meshVertices = new Vector3[numTris * 3];
        var meshTriangles = new int[numTris * 3];
        
        Mesh m = new Mesh();
        
        for (int i = 0; i < numTris; i++) {
            for (int j = 0; j < 3; j++) {
                meshTriangles[i * 3 + j] = i * 3 + j;
                meshVertices[i * 3] = triangles[i].vectorC;
                meshVertices[i * 3 + 1] = triangles[i].vectorB;
                meshVertices[i * 3 + 2] = triangles[i].vectorA;

            }
        }
        
        m.vertices = meshVertices;
        m.triangles = meshTriangles;
        m.RecalculateNormals();
        UpdateMesh(m);

    }
    
    public void UpdateMesh(Mesh mesh)
    {
        meshFilter.mesh = mesh;

    }

    public void GeneratePos()
    {
        Debug.Log("POSITION IS GENERATED");
        
        pointsBuffer = CreatePointsBuffer();
        offsetBuffer = CreateOffsetBuffer();
        
        
        int numPoints = numOfPointsPerAxis * numOfPointsPerAxis * numOfPointsPerAxis;
        int numThreadsPerAxis = Mathf.CeilToInt (numOfPointsPerAxis  / (float) threadGroupSize);

        float pointSpacing = size / (numOfPointsPerAxis - 1);

        
        pointGenerationShader.SetBuffer(0, "points",pointsBuffer);
        pointGenerationShader.SetInt("numOfPointsPerAxis", numOfPointsPerAxis);
        pointGenerationShader.SetInt("chunkSize", size);
        pointGenerationShader.SetVector("centre", new Vector4(pos.x * size, pos.y * size, pos.z * size, 0));
        pointGenerationShader.SetFloat("spacing", pointSpacing);
        pointGenerationShader.SetInt("worldSize", worldSize);
        pointGenerationShader.SetVector("offset", Vector4.zero);
        //Noise
        pointGenerationShader.SetInt("octaves", Mathf.Max (1, numOctaves));
        pointGenerationShader.SetFloat ("lacunarity", lacunarity);
        pointGenerationShader.SetFloat ("persistence", persistence);
        pointGenerationShader.SetFloat ("noiseScale", noiseScale);
        pointGenerationShader.SetFloat ("noiseWeight", noiseWeight);
        pointGenerationShader.SetBool ("closeEdges", closeEdges);
        pointGenerationShader.SetBuffer (0, "offsets", offsetBuffer);
        pointGenerationShader.SetFloat ("floorOffset", floorOffset);
        pointGenerationShader.SetFloat ("weightMultiplier", weightMultiplier);
        pointGenerationShader.SetFloat ("hardFloor", hardFloorHeight);
        pointGenerationShader.SetFloat ("hardFloorWeight", hardFloorWeight);
        pointGenerationShader.SetVector ("params", shaderParams);

        pointGenerationShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);
        
        //Draw points
        points = new Vector4[numPoints];
        
        pointsBuffer.GetData(points);
        canDraw = true;
        
        Debug.Log("THERE ARE " + points.Length + " POINTS");
        
        
        foreach (Vector4 point in points)
        {
            Debug.Log("( " + point.x + " , " + point.y + " , " + point.z + " , " + point.z + " )");
        }
        

        GenerateMesh();

    }
    

    public ComputeBuffer CreateOffsetBuffer()
    {
        var prng = new System.Random (seed);

        var offsets = new Vector3[numOctaves];
        float offsetRange = 1000;
        for (int i = 0; i < numOctaves; i++) {
            offsets[i] = new Vector3 ((float) prng.NextDouble () * 2 - 1, (float) prng.NextDouble () * 2 - 1, (float) prng.NextDouble () * 2 - 1) * offsetRange;
        }
        var offsetsBuffer = new ComputeBuffer (offsets.Length, sizeof (float) * 3);
        offsetsBuffer.SetData (offsets);
        return offsetsBuffer;
    }
    public ComputeBuffer CreatePointsBuffer()
    {
        int numPoints = numOfPointsPerAxis * numOfPointsPerAxis * numOfPointsPerAxis;
        int numVoxelsPerAxis = numOfPointsPerAxis - 1;
        int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
        int maxTriangleCount = numVoxels * 5;

        return new ComputeBuffer(numPoints, sizeof(float) * 4);


    }

    public ComputeBuffer CreateTriangleBuffer()
    {
        int numPoints = numOfPointsPerAxis * numOfPointsPerAxis * numOfPointsPerAxis;
        int numVoxelsPerAxis = numOfPointsPerAxis - 1;
        int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
        int maxTriangleCount = numVoxels * 5;
        
        triCountBuffer = new ComputeBuffer (1, sizeof (int), ComputeBufferType.Raw);
        return new ComputeBuffer (maxTriangleCount, sizeof (float) * 3 * 3, ComputeBufferType.Append);
    }

}
