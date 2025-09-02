using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mining : MonoBehaviour
{
    public ChunkLoader2 chunkLoader;
    

    public LayerMask terrainLayer;
    public float brushSize;
    public float brushWeight;
    
    public Camera cam;
    public PlayerMovement playerMovement;
    bool hasHit;
    Vector3 hitPoint;

    public Transform brushObject;
    private bool terraFormingLastFrame = false;

    
    public ComputeShader terraFormingShader;
    private ComputeBuffer pointsBuffer;
    
    public GameObject miningParticles;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = 100f;
        mousePos = cam.ScreenToWorldPoint(mousePos);

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100))
        {
            
            brushObject.position = hit.point;

            if (!terraFormingLastFrame)
            {
                if (Input.GetMouseButton(0))
                {
                    //Mine(true);
                    ChangeTerrain(true);


               
                }
                if (Input.GetMouseButton(1))
                {
                    //Mine(false);
                    ChangeTerrain(false);
                    SpawnParticles(hit.point);


                }
            }
            else
            {
                terraFormingLastFrame = false;
            }
            

        }

        

    }


    public List<ChunkTerrain> GetAffectedChunks()
    {
        List<ChunkTerrain> affectedChunks = new List<ChunkTerrain>();
        
        Vector3 p = transform.position;
        Vector3 ps = p / chunkLoader.chunkSize;

        float viewDistance = brushSize;

        foreach (ChunkTerrain chunk in chunkLoader.terrainChunkDict.Values)
        {
           // ChunkTerrain chunk = chunkLoader.allChunks[i];
            Vector3 centre = chunk.pos;
            Vector3 viewerOffset = p - centre;
            Vector3 o = new Vector3 (Mathf.Abs (viewerOffset.x), Mathf.Abs (viewerOffset.y), Mathf.Abs (viewerOffset.z)) - Vector3.one * chunkLoader.chunkSize / 2;
            float sqrDst = new Vector3 (Mathf.Max (o.x, 0), Mathf.Max (o.y, 0), Mathf.Max (o.z, 0)).sqrMagnitude;
            
            
            //if (sqrDst < viewDistance * viewDistance)
            if (sqrDst < viewDistance * viewDistance)            {

                //Debug.Log("CHUNK DELEATED | DISTANCE: " + sqrViewDistance + " |  DISTANCE NEEDED: " + sqrDst);
                affectedChunks.Add(chunk);
                // Debug.Log("CHUNK AFFECTED: (  " + chunk.pos.x / chunkLoader.chunkSize + " , " +  chunk.pos.y / chunkLoader.chunkSize + " , " +  chunk.pos.z / chunkLoader.chunkSize + ")");

            }
        }


        return affectedChunks;
    }
    public void ChangeTerrain(bool isMinning)
    {

        float tBefore = Time.realtimeSinceStartup;
        List<ChunkTerrain> affectedChunks = GetAffectedChunks();
        float tAfter = Time.realtimeSinceStartup;
        float getChunksDuration = tAfter - tBefore;
        

        int numThreadsPerAxis = Mathf.CeilToInt (chunkLoader.numOfPointsPerAxis / (float) 8);
        float pointSpacing = chunkLoader.chunkSize / (chunkLoader.numOfPointsPerAxis - 1);


        foreach (ChunkTerrain chunk in affectedChunks)
        {
            ComputeBuffer pointsBuffer = CreateEmptyBuffer();
            pointsBuffer.SetData(chunk.points);
            
            terraFormingShader.SetBuffer(0, "points", pointsBuffer);
            terraFormingShader.SetInt("numOfPointsPerAxis", chunkLoader.numOfPointsPerAxis);
            terraFormingShader.SetFloat("brushSize", brushSize);
            
            if(isMinning) terraFormingShader.SetFloat("brushWeight", brushWeight);
            else terraFormingShader.SetFloat("brushWeight", -brushWeight);

            terraFormingShader.SetVector("brushPos", brushObject.position);
            
            terraFormingShader.SetVector("centre", new Vector4(chunk.pos.x , chunk.pos.y , chunk.pos.z , 0));
            terraFormingShader.SetFloat("spacing", pointSpacing);
            terraFormingShader.SetInt("chunkSize", chunkLoader.chunkSize);

            terraFormingShader.Dispatch(0, chunk.points.Length, 1, 1);

            this.pointsBuffer = pointsBuffer;
            UpdateChunk(chunk);

            
            
        }
        //Debug.Log("Duration in milliseconds: " + getChunksDuration * 1000);

        
        
        terraFormingLastFrame = true;



    }

    public void SpawnParticles(Vector3 hitPos)
    {
        Instantiate(miningParticles, hitPos, Quaternion.identity);
    }
    
    public void Mine(bool isBuilding)
    {
        
        
        //Get the chunks that are being collided by the collider
        //Get the points that are being affected in those colliders and change their noise values

        List<ChunkTerrain> affectedChunks = GetAffectedChunks();


        foreach (ChunkTerrain chunk in affectedChunks)
        {
            
            for (int i = 0; i < chunk.points.Length; i++)
            {
                Vector4 currPoint = chunk.points[i];
                
               // if(Vector3.Distance(new Vector3(currPoint.x, currPoint.y, currPoint.z), transform.position) < brushSize)  chunk.points[i].w = Mathf.Clamp(chunk.points[i].w - brushWeight, 0, 1);

               if (isBuilding)
               {
                   if(Vector3.Distance(new Vector3(currPoint.x, currPoint.y, currPoint.z), transform.position) < brushSize)  chunk.points[i].w = Mathf.Clamp(chunk.points[i].w - brushWeight, 0, 1);
               }
               else
               {
                   if(Vector3.Distance(new Vector3(currPoint.x, currPoint.y, currPoint.z), transform.position) < brushSize)  chunk.points[i].w = Mathf.Clamp(chunk.points[i].w + brushWeight, 0, 1);
               }
            }
        }

        foreach (ChunkTerrain chunk in affectedChunks)
        {
            ComputeBuffer pointsBuffer = CreateEmptyBuffer();
            pointsBuffer.SetData(chunk.points);

            UpdateChunk(chunk);
        }


        //UpdateChunks(affectedChunks);
    }

    public void UpdateChunk( ChunkTerrain chunk)
    {
        //ComputeBuffer pointsBuffer = CreateEmptyBuffer();
        //pointsBuffer.SetData(chunk.points);
            
                
        //Generate mesh
        ComputeBuffer[] triangleBuffers = chunkLoader.GenerateChunkMesh(chunk, pointsBuffer);
        ComputeBuffer triangleBuffer = triangleBuffers[0];
        ComputeBuffer triCountBuffer = triangleBuffers[1];
        
        // Get number of triangles in the triangle buffer
        ComputeBuffer.CopyCount (triangleBuffer, triCountBuffer, 0);
        int[] triCountArray = { 0 };
        triCountBuffer.GetData (triCountArray);

        int numTris = triCountArray[0];
        
        //Get triangles from compute shader
        Triangle[] triangles = new Triangle[numTris];
        triangleBuffer.GetData (triangles, 0, 0, numTris);
        chunk.triangles = triangles;

        Vector4[] points = new Vector4[pointsBuffer.count];
        pointsBuffer.GetData(points);
        chunk.points = points;
       // Debug.Log(chunk.points.Length);
        
        chunk.UpdateMesh(chunkLoader.chunkMaterial);
        
        pointsBuffer.Release();

    }
    public void UpdateChunks(List<ChunkTerrain> chunksToUpdate, ComputeBuffer pointsBuffer)
    {
        foreach (ChunkTerrain chunk in chunksToUpdate)
        {

            //ComputeBuffer pointsBuffer = CreateEmptyBuffer();
            //pointsBuffer.SetData(chunk.points);
            
                
            //Generate mesh
            ComputeBuffer[] triangleBuffers = chunkLoader.GenerateChunkMesh(chunk, pointsBuffer);
            ComputeBuffer triangleBuffer = triangleBuffers[0];
            ComputeBuffer triCountBuffer = triangleBuffers[1];
        
            // Get number of triangles in the triangle buffer
            ComputeBuffer.CopyCount (triangleBuffer, triCountBuffer, 0);
            int[] triCountArray = { 0 };
            triCountBuffer.GetData (triCountArray);

            int numTris = triCountArray[0];
        
            //Get triangles from compute shader
            Triangle[] triangles = new Triangle[numTris];
            triangleBuffer.GetData (triangles, 0, 0, numTris);
            chunk.triangles = triangles;
            
            chunk.UpdateMesh(chunkLoader.chunkMaterial);
            

        }
    }

    public ComputeBuffer CreateEmptyBuffer()
    {
        int numPoints = chunkLoader.numOfPointsPerAxis * chunkLoader.numOfPointsPerAxis * chunkLoader.numOfPointsPerAxis;

        return new ComputeBuffer (numPoints, sizeof (float) * 4);
    }
    private void OnTriggerStay(Collider other)
    {

        
    }
}
