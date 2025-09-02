using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

struct ChunkData
{
    public Triangle[] triangles;
    public Vector4[] points;
}

public class ChunkLoader2 : MonoBehaviour
{
    [Header("Chunk Manager")]
    public int chunkSize;
    [Range(2f, 100f)]
    public int numOfPointsPerAxis;
    public Vector3 offset;
    public int renderDistance;
    public int worldSize;
    public float radius;

    public Transform chunkHolder;
    public Material chunkMaterial;
    [Tooltip("The chunks will load around this target")]
    public Transform terrainTarget;

    public bool LoadInitChunks;

    private int chunksInView;
    private int chunkHeight = 0;
    
    private int threadGroupSize = 8;

    private Queue<ChunkTerrain> recyclableChunks = new Queue<ChunkTerrain>();
    public Dictionary<Vector3, ChunkTerrain> terrainChunkDict = new Dictionary<Vector3, ChunkTerrain>();
    public List<ChunkTerrain> allChunks = new List<ChunkTerrain>();
    private Dictionary<Vector3, ChunkData> allChunkData = new Dictionary<Vector3, ChunkData>();

    
    [Header("Noise")]
    public int seed = 5;
    public int numOctaves = 4;
    public float lacunarity = 2;
    public float persistence = .5f;
    public float noiseScale = 5;
    public float noiseWeight = 5;
    public bool closeEdges;
    public float floorOffset = 1;
    public float weightMultiplier = 1;
    public float hardFloorHeight;
    public float hardFloorWeight;
    public Vector4 shaderParams;
    [Range(0f,1f)]
    public float surfaceLevel;

    [Header("Gizmos")] 
    public bool drawChunkBounds;
    public bool drawVertices;

    [Header("Shaders")]
    public ComputeShader pointGenerationShader;
    public ComputeShader marchingCubesShader;

    private Vector4[] points;
    private Triangle[] triangles;

    private ComputeBuffer pointsBuffer;
    private ComputeBuffer offsetBuffer;
    private ComputeBuffer triangleBuffer;
    private ComputeBuffer triCountBuffer;

    public bool newSettings;

    private void Start()
    {
        if (LoadInitChunks)
        {
            
            CreateBuffers();
            
            
            Vector3 p = terrainTarget.position;
            Vector3 ps = p / chunkSize;
            Vector3Int viewerCoord = new Vector3Int (Mathf.RoundToInt (ps.x), Mathf.RoundToInt (ps.y), Mathf.RoundToInt (ps.z));

            int chunksToLoad = Mathf.RoundToInt(radius / chunkSize) + 5;
            
            for (int x = Mathf.CeilToInt(-chunksToLoad); x <= Mathf.CeilToInt(chunksToLoad); x++)
            {
                for (int y = Mathf.CeilToInt(-chunksToLoad * 2); y <= Mathf.CeilToInt(chunksToLoad); y++)
                {
                    for (int z = Mathf.CeilToInt(-chunksToLoad); z <= Mathf.CeilToInt(chunksToLoad); z++)
                    {

                        Vector3Int coord = new Vector3Int(x, y, z) + viewerCoord;
                        ChunkTerrain chunk = new ChunkTerrain(coord, chunkSize, chunkHolder);
                        terrainChunkDict.Add (coord, chunk);
                        allChunks.Add (chunk);
                        LoadChunk(chunk);
                        chunk.SetVisible(true);

                    }
                }
            }
            
            
            
            if (!Application.isPlaying) {
                ReleaseBuffers ();
            }
            /*CreateBuffers();
            //UpdateVisibleChunks();
            UpdateChunks();
        
            // Release buffers immediately in editor
            if (!Application.isPlaying) {
                ReleaseBuffers ();
            }*/
        }
    }
    private void Update()
    {

        if (Application.isPlaying) {
           Run();
        }
   
    }

    public void Run()
    {
        CreateBuffers();
            //UpdateVisibleChunks();
            //UpdateChunks();

        UpdateChunks();

        // Release buffers immediately in editor
        if (!Application.isPlaying) {
            ReleaseBuffers ();
        }
    }

    public void UpdateChunks()
    {
        if (allChunks==null) {
            return;
        }

        Vector3 p = terrainTarget.position;
        Vector3 ps = p / chunkSize;
        Vector3Int viewerCoord = new Vector3Int (Mathf.RoundToInt (ps.x), Mathf.RoundToInt (ps.y), Mathf.RoundToInt (ps.z));

        float viewDistance = chunkSize * renderDistance;
        
        int maxChunksInView = renderDistance;
        float sqrViewDistance = viewDistance * viewDistance;
        
        // Go through all existing chunks and flag for recyling if outside of max view dst
        for (int i = allChunks.Count - 1; i >= 0; i--) {
            ChunkTerrain chunk = allChunks[i];
            Vector3 centre = chunk.pos;
            Vector3 viewerOffset = p - centre;
            Vector3 o = new Vector3 (Mathf.Abs (viewerOffset.x), Mathf.Abs (viewerOffset.y), Mathf.Abs (viewerOffset.z)) - Vector3.one * chunkSize / 2;
            float sqrDst = new Vector3 (Mathf.Max (o.x, 0), Mathf.Max (o.y, 0), Mathf.Max (o.z, 0)).sqrMagnitude;
            if (sqrDst > sqrViewDistance)
            {
                if (allChunkData.ContainsKey(chunk.pos / chunkSize))
                {
                   // Debug.Log("CHUNK OVVERRIDEN");

                   ChunkData data = new ChunkData();
                   data.triangles = (Triangle[])chunk.triangles.Clone();
                   data.points = (Vector4[])chunk.points.Clone();

                   allChunkData[chunk.pos / chunkSize] = data;
                }
                else
                {

                    // Debug.Log("2UNIQUE CHUNK HAS BEEN DESPAWNED | POS: ( " + chunk.pos.x / chunkSize + " , " + chunk.pos.y / chunkSize+ " , " + chunk.pos.z/ chunkSize  + " ) | TRI COUNT: " + chunk.triangles.Length );

                    ChunkData data = new ChunkData();
                    data.triangles = (Triangle[])chunk.triangles.Clone();
                    data.points = (Vector4[])chunk.points.Clone();

                    allChunkData.Add(chunk.pos / chunkSize, data);
                }

                
                terrainChunkDict.Remove(chunk.pos / chunkSize);
                recyclableChunks.Enqueue(chunk);
                allChunks.RemoveAt(i);


                

            //Debug.Log("CHUNK DELEATED | DISTANCE: " + sqrViewDistance + " |  DISTANCE NEEDED: " + sqrDst);


            }
        }
        
            
        for (int x = -maxChunksInView; x <= maxChunksInView; x++) {
            for (int y = -maxChunksInView; y <= maxChunksInView; y++) {
                for (int z = -maxChunksInView; z <= maxChunksInView; z++) {
                    
                    Vector3Int coord = new Vector3Int (x, y, z) + viewerCoord;

                    if (terrainChunkDict.ContainsKey (coord)) {
                        continue;
                    }

                    Vector3 centre = coord * chunkSize;
                    Vector3 viewerOffset = p - centre;
                    Vector3 o = new Vector3 (Mathf.Abs (viewerOffset.x), Mathf.Abs (viewerOffset.y), Mathf.Abs (viewerOffset.z)) - Vector3.one * chunkSize / 2;
                    float sqrDst = new Vector3 (Mathf.Max (o.x, 0), Mathf.Max (o.y, 0), Mathf.Max (o.z, 0)).sqrMagnitude;

                    // Chunk is within view distance and should be created (if it doesn't already exist)
                    if (sqrDst <= sqrViewDistance) {

                        //Bounds bounds = new Bounds (coord * chunkSize, Vector3.one * chunkSize);
                        if (true) {

                            if (recyclableChunks.Count > 0) {
                                
                                ChunkTerrain chunk = recyclableChunks.Dequeue ();
                                chunk.pos = centre;
                                terrainChunkDict.Add (coord, chunk);
                                allChunks.Add (chunk);
                                
                                if (allChunkData.ContainsKey(coord))
                                {
                                    
                                    //Chunk has already been generated in the past
                                    //Used to allow building and mining without deleting the progress
                                    
                                    chunk.points = allChunkData[coord].points;
                                    chunk.triangles = allChunkData[coord].triangles;

                                    //Debug.Log("RELOADING OLD CHUNK: CHUNK POS: ( " + chunk.pos.x / chunkSize + " , " + chunk.pos.y / chunkSize+ " , " + chunk.pos.z/ chunkSize  + " ) | TRI COUNT: " + chunk.triangles.Length);
                                    chunk.UpdateMesh(chunkMaterial);
                                }
                                else
                                {
                                    LoadChunk(chunk);
                                }
                                

                            } else {
                                ChunkTerrain chunk = new ChunkTerrain(coord, chunkSize, chunkHolder);
                                terrainChunkDict.Add (coord, chunk);
                                allChunks.Add (chunk);
                                
                                if (allChunkData.ContainsKey(coord))
                                {
                                    //Chunk has already been generated in the past
                                    //Used to allow building and mining without deleting the progress#

                                    
                                    chunk.points = allChunkData[coord].points;
                                    chunk.triangles = allChunkData[coord].triangles;
                                    
                                    chunk.UpdateMesh(chunkMaterial);
                                }
                                else
                                {
                                    LoadChunk(chunk);
                                }
                                
                                chunk.SetVisible(true);
                            }
                        }
                    }

                }
            }
        }
        
    }
    
    void UpdateVisibleChunks()
    {
        //Chunk pooling
        // Queue of chunks - Recyclable chunks
        // Dict of chunks - Currently visible chunks
        // List of chunks - All chunks
        
        //1)Loop through current chunks and check if they are in render distance. If No, then remove chunk from dict and add to the queue. Else continue
        //2)Loop through the needed coordinates of chunks to be spawned. If the already exits ( already in dict ) then skip. Else if, check if the queue has chunks available to be modified. Else spawn a new gameobject
        

        for (int i = allChunks.Count - 1; i >= 0; i--)
        {
            ChunkTerrain chunk = allChunks[i];
            
            float distanceBetween = Vector3.Distance(chunk.pos, terrainTarget.position);



            
            if (distanceBetween > renderDistance * chunkSize)
            {
                //chunk.SetVisible(false);
                terrainChunkDict.Remove (chunk.pos / chunkSize);
                recyclableChunks.Enqueue (chunk);
                allChunks.RemoveAt(i);
              //  Debug.Log("CHUNK DELEATED | DISTANCE: " + distanceBetween + " |  DISTANCE NEEDED: " + renderDistance * chunkSize);
            };
            
        }

        
        Vector3 currentChunkPos = new Vector3((Mathf.RoundToInt(terrainTarget.position.x / chunkSize)), (Mathf.RoundToInt(terrainTarget.position.y / chunkSize)),(Mathf.RoundToInt(terrainTarget.position.z / chunkSize)) );

        for (int xOffset = -renderDistance; xOffset <= renderDistance; xOffset++)
        {
            for (int yOffset = -renderDistance; yOffset <= renderDistance; yOffset++)
            {
                for (int zOffset = -renderDistance; zOffset <= renderDistance; zOffset++)
                {
                    Vector3 viewedChunkPos = new Vector3(currentChunkPos.x + xOffset, currentChunkPos.y + yOffset, currentChunkPos.z + zOffset);
                    //Debug.Log("( " + viewedChunkPos.x + " , " + viewedChunkPos.y + " , " + viewedChunkPos.z + " )");
                    Debug.Log("DICT COUNT IS: " + terrainChunkDict.Count);
                    
                    if (terrainChunkDict.ContainsKey(viewedChunkPos))
                    {
                        terrainChunkDict[viewedChunkPos].SetVisible(true);

                        continue;
                    }

          
                    float distanceBetween = Vector3.Distance(viewedChunkPos * chunkSize, terrainTarget.position);
                    if (distanceBetween <= renderDistance * chunkSize) {
                        //Bounds bounds = new Bounds (viewedChunkPos, Vector3.one * chunkSize);
                        //if (IsVisibleFrom(bounds, Camera.main))
                        if(true)
                        {
                            //New chunk is needed
                            if (recyclableChunks.Count > 0)
                            {
                                ChunkTerrain chunk = recyclableChunks.Dequeue();
                                chunk.pos = viewedChunkPos * chunkSize; 
                                terrainChunkDict.Add(viewedChunkPos, chunk);
                                //chunk.SetVisible(true);
                                allChunks.Add(chunk);
                                //LoadChunk(chunk);
                            
                                Debug.Log("CHUNK Recycled");

                            }
                            else
                            {
                                //Create new chunk
                                ChunkTerrain currentChunk = new ChunkTerrain(viewedChunkPos, chunkSize, chunkHolder);
                                terrainChunkDict.Add(viewedChunkPos, currentChunk);
                                //currentChunk.SetVisible(true);
                                allChunks.Add(currentChunk);

                                //LoadChunk(currentChunk);
                            }
                        }



                    }
                  /*  if (terrainChunkDict.ContainsKey(viewedChunkPos))
                    {
                    }
                    else
                    {
                        //Chunk Pooling
                        //Check if the recyclable chunks is empty before spawning a new instance
                        if (recyclableChunks.Count > 0)
                        {
                            
                        }
                        else
                        {
                            //Create new chunk
                            terrainChunkDict.Add(viewedChunkPos, new ChunkTerrain(viewedChunkPos, chunkSize, chunkHolder));
                            ChunkTerrain currentChunk = terrainChunkDict[viewedChunkPos];
                            LoadChunk(currentChunk);
                        }
                    }*/
                    

/*
                    if (terrainChunkDict.ContainsKey(viewedChunkPos))
                    {
                        ChunkTerrain currentChunk = terrainChunkDict[viewedChunkPos];
                        
                        //Update Existing Chunk
                        bool isVisibleBefore = currentChunk.isVisible;
                        
                        currentChunk.UpdateVisibility(terrainTarget.position, renderDistance);

                        
                        //When a chunk is loaded update its mesh in case of any setting changes
                        if (currentChunk.isVisible && !isVisibleBefore)
                        {
              
                            currentChunk.UpdateMesh(chunkMaterial);
                            //Chunk should not be recalculated if the settings have not been changed
                            //LoadChunk(currentChunk);
                            //currentChunk.UpdateMesh();
                        }

                    }
                    else
                    {
                        //Chunk Pooling
                        //Check if the recyclable chunks is empty before spawning a new instance
                        if (recyclableChunks.Count > 0)
                        {
                            
                        }
                        else
                        {
                            //Create new chunk
                            terrainChunkDict.Add(viewedChunkPos, new ChunkTerrain(viewedChunkPos, chunkSize, chunkHolder));
                            ChunkTerrain currentChunk = terrainChunkDict[viewedChunkPos];
                            LoadChunk(currentChunk);
                        }
                        

                    }*/
                }
            }
        }
        

    }

    public bool IsVisibleFrom (Bounds bounds, Camera camera) {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes (camera);
        return GeometryUtility.TestPlanesAABB (planes, bounds);
    }
    public void ReleaseBuffers()
    {
        if (triangleBuffer != null) {
            triangleBuffer.Release ();
            pointsBuffer.Release ();
            triCountBuffer.Release ();
        }
    }

    public void UpdateAllVisibleChunks()
    {
        //Update the mesh of all the visible chunks

        foreach (ChunkTerrain chunk in terrainChunkDict.Values)
        {
            if (chunk.isVisible)
            {
                LoadChunk(chunk);
            }
        }
    }

    public void UpdateAllChunks()
    {
        //Update the mesh of all the visible and invisible chunks

        foreach (ChunkTerrain chunk in terrainChunkDict.Values)
        {
            //LoadChunk(chunk);
        }

    }



    public void OnDrawGizmos()
    {
        if (drawChunkBounds)
        {
            foreach(ChunkTerrain chunk in terrainChunkDict.Values)
            {

                if (chunk.isVisible)
                {
                    Gizmos.DrawWireCube(chunk.pos, chunkSize * Vector3.one);

                }
                
            }
        }

        if (drawVertices)
        {
            foreach(ChunkTerrain chunk in terrainChunkDict.Values)
            {

                if (chunk.isVisible)
                {
                    int index = 0;
                    foreach (Vector4 point in chunk.points)
                    {
                        Gizmos.color = new Color(1, 1, 1,  point.w);

                        //Gizmos.DrawSphere(new Vector3(point.x, point.y, point.z), 0.1f);

                        if (point.w > surfaceLevel)
                        {
                            
                            Gizmos.DrawSphere(new Vector3(point.x, point.y, point.z), 0.1f);
                            //Handles.Label(new Vector3(point.x, point.y, point.z), "( " + point.x + ", " + point.y + " , " + point.z + " )");
                            index++;

                        }
                
                    }
                }
                
            }
        }
    }
    
    public void LoadChunk(ChunkTerrain chunk)
    {
        Debug.Log("CHUNK LOADED");
        
        int numPoints = numOfPointsPerAxis * numOfPointsPerAxis * numOfPointsPerAxis;

        //Generate points
        ComputeBuffer currentPoints = GenerateChunkVertices(chunk);
        Vector4[] points = new Vector4[numPoints];
        currentPoints.GetData(points);
        chunk.points = points;
        
        
        //Generate mesh
        ComputeBuffer[] triangleBuffers = GenerateChunkMesh(chunk, currentPoints);
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

        chunk.UpdateMesh(chunkMaterial);
        

       // Debug.Log("CHUNK LOADED");
       
        //pointsBuffer.Release();
        //triangleBuffer.Release();
        //triCountBuffer.Release();
        //currentPoints.Release();
        

    }

    public ComputeBuffer[] GenerateChunkMesh(ChunkTerrain chunk, ComputeBuffer chunkPointsBuffer)
    {
        int numVoxelsPerAxis = numOfPointsPerAxis - 1;
        int numThreadsPerAxis = Mathf.CeilToInt (numVoxelsPerAxis / (float) threadGroupSize);
        
        triangleBuffer.SetCounterValue(0);

        
        marchingCubesShader.SetBuffer (0, "points", chunkPointsBuffer);
        marchingCubesShader.SetBuffer (0, "triangles", triangleBuffer);
        marchingCubesShader.SetInt ("numOfPointsPerAxis", numOfPointsPerAxis);
        marchingCubesShader.SetFloat ("surfaceLevel", surfaceLevel);
        
        marchingCubesShader.Dispatch (0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

        return new ComputeBuffer[2]{triangleBuffer, triCountBuffer};
    }

    public ComputeBuffer GenerateChunkVertices(ChunkTerrain chunk)
    {
        int numVoxelsPerAxis = numOfPointsPerAxis - 1;

        float pointSpacing = chunkSize / (numOfPointsPerAxis - 1);
        int numThreadsPerAxis = Mathf.CeilToInt (numVoxelsPerAxis / (float) threadGroupSize);

        var prng = new System.Random (seed);

        var offsets = new Vector3[numOctaves];
        float offsetRange = 1000;
        for (int i = 0; i < numOctaves; i++) {
            offsets[i] = new Vector3 ((float) prng.NextDouble () * 2 - 1, (float) prng.NextDouble () * 2 - 1, (float) prng.NextDouble () * 2 - 1) * offsetRange;
        }
        offsetBuffer = new ComputeBuffer (offsets.Length, sizeof (float) * 3);
        offsetBuffer.SetData(offsets);
        
        pointGenerationShader.SetBuffer(0, "points",pointsBuffer);
        pointGenerationShader.SetInt("numOfPointsPerAxis", numOfPointsPerAxis);
        pointGenerationShader.SetInt("chunkSize", chunkSize);
        pointGenerationShader.SetVector("centre", new Vector4(chunk.pos.x , chunk.pos.y , chunk.pos.z , 0));
        pointGenerationShader.SetVector("chunkPos", chunk.pos);
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
        pointGenerationShader.SetFloat("radius", radius);

        pointGenerationShader.Dispatch(0, numThreadsPerAxis, numThreadsPerAxis, numThreadsPerAxis);

        offsetBuffer.Release();
        
        return pointsBuffer;
    }

    public void OnDestroy()
    {
        if (Application.isPlaying)
        {
            ReleaseBuffers();
        }
    }

    public void CreateBuffers()
    {
        

        
        int numPoints = numOfPointsPerAxis * numOfPointsPerAxis * numOfPointsPerAxis;
        int numVoxelsPerAxis = numOfPointsPerAxis - 1;
        int numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
        int maxTriangleCount = numVoxels * 5;


        if (!Application.isPlaying || (pointsBuffer == null || numPoints != pointsBuffer.count)) {
            if (Application.isPlaying) {
                ReleaseBuffers ();
            }
            triangleBuffer = new ComputeBuffer (maxTriangleCount, sizeof (float) * 3 * 3, ComputeBufferType.Append);
            pointsBuffer = new ComputeBuffer (numPoints, sizeof (float) * 4);
            triCountBuffer = new ComputeBuffer (1, sizeof (int), ComputeBufferType.Raw);

        }

    }
    
}



public class ChunkTerrain
{
    public Vector3 pos { get; set; }
    public int chunkSize { get; set; }
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh mesh;
    public GameObject chunkObject;
    public bool isVisible;
    public MeshCollider meshCollider;

    
    public Vector4[] points;
    public Triangle[] triangles;

    public ChunkTerrain(Vector3 pos, int chunkSize, Transform chunkHolder)
    {
        this.pos = pos * chunkSize;
        this.chunkSize = chunkSize;
        chunkObject = new GameObject("Chunk: (" + pos.x + ", " + pos.y + ", " + pos.z +")");
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();
        meshCollider = chunkObject.AddComponent<MeshCollider>();

        chunkObject.layer = LayerMask.NameToLayer("Terrain");
        chunkObject.transform.position = Vector3.zero;
        //chunkObject.transform.localScale = Vector3.one * chunkSize / 10f;
        chunkObject.transform.parent = chunkHolder;
        
        SetVisible(false);
    }



    public void UpdateMesh(Material chunkMaterial)
    {
        var meshVertices = new Vector3[triangles.Length * 3];
        var meshTriangles = new int[triangles.Length * 3];
        
        Mesh m = new Mesh();
        
        for (int i = 0; i < triangles.Length; i++) {
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
        meshFilter.mesh = m;
        meshRenderer.material = chunkMaterial;

        meshCollider.sharedMesh = m;

    }


    public void SetVisible(bool isVisible)
    {
        this.isVisible = isVisible;
        chunkObject.SetActive(this.isVisible);
    }
    

}
