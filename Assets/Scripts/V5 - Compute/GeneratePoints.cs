using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GeneratePoints : MonoBehaviour
{
    
    [Header("Chunk data")]
    public int size;
    public Vector3 pos;
    public float yOffset;
    [Range(1f, 100f)]
    public int numOfPointsPerAxis;

    public Vector3 centre;
    
    private ComputeBuffer pointsBuffer;
    
    private int threadGroupSize = 8;
    
    
    public ComputeShader pointGenerationShader;
    private Vector4[] points;

    private bool canDraw = false;

    public void Start()
    {
        GeneratePos();
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q)) ;
        {
            GeneratePos();

        }
    }

    public void OnSpawn(int size, Vector3 pos, int numberOfPointsPerAxis, Vector3 centre, ComputeShader pointsShader)
    {
        this.size = size;
        this.pos = pos;
        this.numOfPointsPerAxis = numberOfPointsPerAxis;
        this.centre = centre;
        this.pointGenerationShader = pointsShader;
        
        GeneratePos();
    }

    public void GeneratePos()
    {
        pointsBuffer = CreatePointsBuffer();
        
        int numPoints = numOfPointsPerAxis * numOfPointsPerAxis * numOfPointsPerAxis;
        float pointSpacing = size / (numOfPointsPerAxis - 1);

        pointGenerationShader.SetBuffer(0, "points",pointsBuffer);
        pointGenerationShader.SetInt("numOfPointsPerAxis", numOfPointsPerAxis);
        pointGenerationShader.SetInt("chunkSize", size
        );
        pointGenerationShader.SetVector("centre", centre);
        pointGenerationShader.SetVector("chunkPos", pos);
        pointGenerationShader.SetFloat("spacing", pointSpacing);
        
        pointGenerationShader.Dispatch(0, numOfPointsPerAxis, numOfPointsPerAxis, numOfPointsPerAxis);
        
        //Draw points
        points = new Vector4[numPoints];
        
        pointsBuffer.GetData(points);
        canDraw = true;
        //pointsBuffer.Dispose();

    }

    public void OnDrawGizmos()
    {


        if (canDraw)
        {
            float pointSpacing = size / (numOfPointsPerAxis - 1);

            float sizeDiff = (size / numOfPointsPerAxis);
            
           // Gizmos.DrawWireCube(pos * size , (size / numOfPointsPerAxis) * (numOfPointsPerAxis - 1) * Vector3.one);
           Gizmos.DrawWireCube(pos * size , size * Vector3.one);

            
            foreach (Vector4 point in points)
            {
                Gizmos.color = new Color(1, 1, 1, 1);
                //Gizmos.DrawSphere(new Vector3(point.x + (pos.x * size ) , point.y + (pos.y * size ) , point.z + (pos.z * size)), 0.2f);
                //Gizmos.DrawSphere(new Vector3(point.x + (pos.x * size ) , point.y + (pos.y * size ) , point.z + (pos.z * size)), 0.2f);
                //Gizmos.DrawSphere(new Vector3(point.x + (pos.x * size ) , point.y + (pos.y * size ) , point.z + (pos.z * size)), 0.1f);
                Gizmos.DrawSphere(new Vector3(point.x, point.y, point.z), 0.1f);
                Gizmos.color = new Color(0, 1, 0, 1);

                Gizmos.DrawSphere(new Vector3(0,0,0),0.05f);
                
                Gizmos.color = new Color(1, 0, 0, 0.2f);
                /*Gizmos.DrawWireCube(
                    new Vector3(point.x + (pos.x * size)  + (pointSpacing * 0.5f),
                        point.y + (pos.y * size) + (pointSpacing * 0.5f),
                        point.z + (pos.z * size) + (pointSpacing * 0.5f)),
                    Vector3.one * pointSpacing);*/
            
            }
        }

    }

    public ComputeBuffer CreatePointsBuffer()
    {
        int numPoints = numOfPointsPerAxis * numOfPointsPerAxis * numOfPointsPerAxis;
        int numVoxelsPerAxis = numOfPointsPerAxis - 1;
        int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
        int maxTriangleCount = numVoxels * 5;

        return new ComputeBuffer(numPoints, sizeof(float) * 4);


    }
}
