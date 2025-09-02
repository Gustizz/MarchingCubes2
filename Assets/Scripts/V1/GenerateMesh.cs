using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class cube
{
    public float x;
    public float y;
    public float z;

    public _vertex[] vertices = new _vertex[8];

    public cube(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public _vertex[] GetVertices(_vertex[,,] verticesArr)
    {
        
        
        return new _vertex[]{};
    }
}
public class GenerateMesh : MonoBehaviour
{

    public TriangularData triData;

    public GenerateVertices verticesData;

    public cube cubeToDraw;

    public cube[,,] cubeArr;
    
    // Start is called before the first frame update
    void Start()
    {
        cubeArr = GenerateCubes(verticesData.GenerateVertecies());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDrawGizmos()
    {
        if (cubeToDraw != null)
        {
            //Draw cube
        }
    }

    public void CreateMesh()
    {
        
    }

    public cube[,,] GenerateCubes(_vertex[,,] vertexArr)
    {
        cube[,,] cubeArr = new cube[vertexArr.GetLength(0) - 2, vertexArr.GetLength(1) - 2, vertexArr.GetLength(2) -2];
        
        for (int x = 1; x < vertexArr.GetLength(0) - 1; x++)
        {
            for (int y = 1; y < vertexArr.GetLength(1) - 1; y++)
            {
                for (int z = 1; z < vertexArr.GetLength(2) - 1; z++)
                {
                    cube newCube = new cube(x, y, z);
                    newCube.GetVertices(vertexArr);

                    cubeArr[x, y, z] = newCube;
                }
            }
        }
    
        
        
        return cubeArr;
    }

    public void DrawCube(int x, int y, int z)
    {
        
    }
    
    
}
