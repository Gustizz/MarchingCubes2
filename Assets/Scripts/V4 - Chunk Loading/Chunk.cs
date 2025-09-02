using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{

    private MeshFilter meshFilter;
    
    private int size;
    private Vector3 pos;
    private float yOffset;
    private float numOfPointsPerAxis;
    private float noiseScale;
    private float surfaceLevel;
    private bool isNoise3D;
    private TriangularData triData;

    //Like a void start
    public void ChunkCreated(Vector3 pos, int size, float numOfPointsPerAxis, float noiseScale, float surfaceLevel, bool isNoise3D, TriangularData triData)
    {
        meshFilter = gameObject.GetComponent<MeshFilter>();
        
        this.pos = pos;
        this.size = size;
        this.numOfPointsPerAxis = numOfPointsPerAxis;
        this.noiseScale = noiseScale;
        this.surfaceLevel = surfaceLevel;
        this.isNoise3D = isNoise3D;
        this.triData = triData;
        
        GenerateMesh();
    }

    public void UpdateMesh(float numOfPointsPerAxis, float noiseScale, float surfaceLevel, bool isNoise3D)
    {
        this.numOfPointsPerAxis = numOfPointsPerAxis;
        this.noiseScale = noiseScale;
        this.surfaceLevel = surfaceLevel;
        this.isNoise3D = isNoise3D;
        GenerateMesh();
    }

    private void GenerateMesh()
    {
        float pointSpacing = size / (numOfPointsPerAxis - 1);
        List<Vector3> meshVertices = new List<Vector3>();

        

        for (int x = 0; x < numOfPointsPerAxis; x++)
        {
            for (int y = 0; y < numOfPointsPerAxis; y++)
            {
                for (int z = 0; z < numOfPointsPerAxis; z++)
                {
                
                    Vector3 cubePosWorld = new Vector3(x * pointSpacing + pos.x - (size * 0.5f) + (pointSpacing * 0.5f), y * pointSpacing + +pos.y - (size * 0.5f) + (pointSpacing * 0.5f), z * pointSpacing + pos.z - (size * 0.5f) + (pointSpacing * 0.5f));

                    //Creates Cube
                    
                    Cube newCube = new Cube(new Vector3(x, y, z), cubePosWorld, pointSpacing, noiseScale, isNoise3D);

                    //Generates mesh for cube
                    int cubeIndex = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        if (newCube.vertices[i].noiseValue < surfaceLevel)
                        {
                            cubeIndex += 1 << i;
                        }
                    }
                    
                    Vertex[] vertices = newCube.vertices;
                    
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
        
        int[] triangles = new int[meshVertices.Count];

        if (isNoise3D)
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
        
        m.triangles = triangles;
        m.RecalculateNormals();
        UpdateMesh(m);
    }
    
    public void UpdateMesh(Mesh mesh)
    {
        meshFilter.mesh = mesh;

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

  
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireCube(pos, Vector3.one * size);

        int pointSpacing = Mathf.RoundToInt(size / numOfPointsPerAxis);

        for (int x = 0; x < numOfPointsPerAxis + 1; x++)
        {
            for (int y = 0; y < numOfPointsPerAxis + 1; y++)
            {
                for (int z = 0; z < numOfPointsPerAxis + 1; z++)
                {

                    Gizmos.color = new Color(1, 1, 1, 1);

                    Gizmos.DrawSphere(new Vector3(x * pointSpacing + pos.x - (size * 0.5f), y * pointSpacing + pos.y - (size * 0.5f) , z * pointSpacing + pos.z - (size * 0.5f)), 0.1f);
                }
            }
        }
        
        for (int x = 0; x < numOfPointsPerAxis; x++)
        {
            for (int y = 0; y < numOfPointsPerAxis; y++)
            {
                for (int z = 0; z < numOfPointsPerAxis; z++)
                {

                    Gizmos.color = new Color(1, 0, 0, 1);
                    Gizmos.DrawWireCube(
                        new Vector3(x * pointSpacing + pos.x - (size * 0.5f) + (pointSpacing * 0.5f),
                            y * pointSpacing + pos.y - (size * 0.5f) + (pointSpacing * 0.5f),
                            z * pointSpacing + pos.z - (size * 0.5f) + (pointSpacing * 0.5f)),
                        Vector3.one * pointSpacing);
                }
            }
        }
    }
}
