using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class _vertex
{
    
    public float x;
    public float y;
    public float z;
    public float noiseValue;

    public _vertex(float x, float y, float z, float noiseValue)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.noiseValue = noiseValue;
    }
}

public class GenerateVertices : MonoBehaviour
{
    [Header("Cube Data")] 
    public int cubeWidth;
    public int cubeHeight;
    public int cubeLength;
    public Vector3 cubePos;
    [Range(0.4f, 10)]
    public float gapInterval;

    
    [Header("Noise")] 

    [SerializeField]
    public _vertex[,,] verticesArr;
    
    [SerializeField]
    private float noiseScale = .05f;
    
    [Range(0.0f, 1f)]
    public float threshold;
    
    // Start is called before the first frame update
    void Start()
    {
        verticesArr = GenerateVertecies();
    }

    // Update is called once per frame
    void Update()
    {
        verticesArr = GenerateVertecies();
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 0, 0, 1);
        Gizmos.DrawWireCube(cubePos, new Vector3(cubeWidth, cubeHeight, cubeLength));

        
        //If gapinterval is 0 the for loops crash the project 
        gapInterval =  Math.Clamp(gapInterval, 0.25f, 100f);
        
        //Generate vertices

        int gapIncrease = RoundNum(gapInterval * 2) ;

        if (verticesArr != null)
        {
            for (int x = 0; x < verticesArr.GetLength(0); x++)
            {
                for (int y = 0; y < verticesArr.GetLength(1); y++)
                {
                    for (int z = 0; z < verticesArr.GetLength(2); z++)
                    {
                        if (verticesArr[x, y, z].noiseValue > threshold)
                        {
                            Gizmos.color = new Color(verticesArr[x, y, z].noiseValue, verticesArr[x, y, z].noiseValue, verticesArr[x, y, z].noiseValue, 1);
                            Gizmos.DrawSphere(new Vector3(verticesArr[x,y,z].x + cubePos.x ,verticesArr[x,y,z].y + cubePos.y,verticesArr[x,y,z].z + cubePos.z), 0.5f);
                        }
                        

                    }
                }
            }
        }
        

    }

    public _vertex[,,] GenerateVertecies()
    {
        
        //Generate vertices
        //Gap interval is distance between points
        //Amount of vertices = length / gap inteval

        verticesArr = new _vertex[RoundUp(cubeWidth / gapInterval), RoundUp(cubeHeight / gapInterval), RoundUp(cubeLength / gapInterval)];

        
        for (int y = 0; y < RoundUp(cubeHeight / gapInterval); y++)
        {
            for (int x = 0; x < RoundUp(cubeWidth / gapInterval); x++)
            {
                for (int z = 0; z < RoundUp(cubeLength / gapInterval); z++)
                {
                    
                    float noiseValue = Perlin3D(x * noiseScale, y * noiseScale, z * noiseScale);//get value of the noise at given x, y, and z.

                    //Debug.Log("(" + x + "," + y+ "," + z +")");

                    float xDist = x * gapInterval;
                    float yDist = y * gapInterval;
                    float zDist = z * gapInterval;

                    verticesArr[x,y,z] = new _vertex(xDist - (cubeWidth / 2), yDist- (cubeHeight / 2), zDist- (cubeLength / 2), noiseValue);
                    
                }
            }
        }

        return verticesArr;
    }

    private int RoundNum(float num)
    {
        return (int)Math.Round(num);
    }

    private int RoundUp(float num)
    {
        return (int)Math.Ceiling(num);
    }
    
    public static float Perlin3D(float x, float y, float z) {
        float ab = Mathf.PerlinNoise(x, y);
        float bc = Mathf.PerlinNoise(y, z);
        float ac = Mathf.PerlinNoise(x, z);

        float ba = Mathf.PerlinNoise(y, x);
        float cb = Mathf.PerlinNoise(z, y);
        float ca = Mathf.PerlinNoise(z, x);

        float abc = ab + bc + ac + ba + cb + ca;
        return abc / 6f;
    }
}
