using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    [Header("Chunk Info")] 
    public int chunkSize;
    public int renderDistance;
    [Range(1f, 100f)]
    public int numOfPointsPerAxis;


    private int chunksInView;
    private int chunkHeight = 0;

    [Header("Noise")] 
    [SerializeField]
    private float noiseScale = .05f;
    [Range(0.0f, 1f)]
    public float surfaceLevel;
    public bool noiseIs3D;

    public TriangularData triData;
    public Material terrainMat;
    
    
    private Dictionary<Vector3, TerrainChunk> terrainChunkDict = new Dictionary<Vector3, TerrainChunk>();
    
    [Space(25)]
    [Tooltip("The chunks will load around this target")]
    public Transform target;
    
    
    public ComputeShader pointsShader;

    void Start()
    {
        
    }
    

    void Update()
    {
        UpdateVisibleChunks();
    }

    public void ReGenerateMeshes()
    {
        foreach(var chunk in terrainChunkDict.Values)
        {
            chunk.RegenerateMesh(numOfPointsPerAxis, noiseScale, surfaceLevel, noiseIs3D);
        }
    }
    void UpdateVisibleChunks()
    {
        Vector3 currentChunkPos = new Vector3((Mathf.RoundToInt(target.position.x / chunkSize)), (Mathf.RoundToInt(target.position.y / chunkSize)),(Mathf.RoundToInt(target.position.z / chunkSize)) );

        for (int xOffset = -renderDistance; xOffset <= renderDistance; xOffset++)
        {
            for (int yOffset = -renderDistance; yOffset <= renderDistance; yOffset++)
            {
                for (int zOffset = -renderDistance; zOffset <= renderDistance; zOffset++)
                {
                    Vector3 viewedChunkPos = new Vector3(currentChunkPos.x + xOffset, currentChunkPos.y + yOffset, currentChunkPos.z + zOffset);

                    if (terrainChunkDict.ContainsKey(viewedChunkPos))
                    {
                        terrainChunkDict[viewedChunkPos].Update(target.position, renderDistance);
                    }
                    else
                    {
                        //Create new chynk
                        terrainChunkDict.Add(viewedChunkPos, new TerrainChunk(viewedChunkPos, chunkSize, numOfPointsPerAxis, noiseScale, surfaceLevel, noiseIs3D, triData, terrainMat, pointsShader));
                    }
                }
            }
            

        }
    }

    public class TerrainChunk
    {
        private Vector3 pos;
        private int size;
        private GameObject meshObject;
        
        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        //private Chunk chunk;

        private GridGenerator chunk;
        
        public TerrainChunk(Vector3 pos, int size, int numOfPointsPerAxis, float noiseScale, float surfaceLevel, bool isNoise3D, TriangularData triData, Material terrainMat, ComputeShader pointsShader)
        {
            this.pos = pos * size;
            this.size = size;

            //Create chunk
            meshObject = new GameObject("Chunk");
            
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            
            
            //chunk = meshObject.AddComponent<Chunk>();
            //chunk.ChunkCreated(this.pos, this.size, numOfPointsPerAxis, noiseScale, surfaceLevel, isNoise3D, triData );

            //NEW
            chunk = meshObject.AddComponent<GridGenerator>();
            chunk.OnSpawn(size, pos, (int)numOfPointsPerAxis, new Vector3(0,0,0), pointsShader);
            
            meshRenderer.material = terrainMat;

            meshObject.transform.position = Vector3.zero;
            meshObject.transform.localScale = Vector3.one * size / 10f;
            
            
            
            SetVisible(false);

        }

        public void RegenerateMesh(float numOfPointsPerAxis, float noiseScale, float surfaceLevel, bool isNoise3D)
        {
            //chunk.UpdateMesh(numOfPointsPerAxis, noiseScale, surfaceLevel, isNoise3D);
        }


        public void Update(Vector3 targetPos, int renderDistance)
        {
            float distanceBetween = Vector3.Distance(this.pos, targetPos);

            bool visible = false;
            if (distanceBetween < renderDistance * size) visible = true;
            SetVisible(visible);

        }
        
        public void SetVisible(bool isVisible)
        {
            meshObject.SetActive(isVisible);
        }
    }
    
}
