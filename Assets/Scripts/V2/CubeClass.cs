using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Cube
{
    public Vector3 relativePos;
    public Vector3 worldPos;
    public float size;
    public Vertex[] vertices;
    public Vertex[] edgePoints;

    public Cube(Vector3 relativePos,Vector3 worldPos, float size, float noiseScale, bool noiseIs3D)
    {
        this.relativePos = relativePos;
        this.worldPos = worldPos;
        this.size = size;

        vertices = GenerateVertices(noiseScale, noiseIs3D);
        edgePoints = GenerateEdgePoints();
    }

    public Vertex[] GenerateVertices(float noiseScale, bool noiseIs3D)
    {
        Vertex[] vertices = new Vertex[8];

        float halfCubeSize = (this.size * 0.5f);
       // float noiseValue = Perlin3D(x * noiseScale, y * noiseScale, z * noiseScale);//get value of the noise at given x, y, and z.

        //bottom left far 
        Vector3 vertexWorldPos = new Vector3(this.worldPos.x - halfCubeSize, this.worldPos.y - halfCubeSize, this.worldPos.z + halfCubeSize);
        vertices[0] = new Vertex(
            new Vector3(0, 0, 1) + this.relativePos,
            vertexWorldPos,
            GetNoiseFloat(vertexWorldPos.x * noiseScale, vertexWorldPos.y * noiseScale, vertexWorldPos.z * noiseScale, noiseIs3D));
        
        
        //bottom right far 
        vertexWorldPos = new Vector3(this.worldPos.x + halfCubeSize, this.worldPos.y - halfCubeSize, this.worldPos.z + halfCubeSize);
        vertices[1] = new Vertex(
            new Vector3(1, 0, 1) + this.relativePos,
            vertexWorldPos,
            GetNoiseFloat(vertexWorldPos.x * noiseScale, vertexWorldPos.y * noiseScale, vertexWorldPos.z * noiseScale, noiseIs3D));
        
        
        //bottom right close 
        vertexWorldPos = new Vector3(this.worldPos.x + halfCubeSize, this.worldPos.y - halfCubeSize, this.worldPos.z - halfCubeSize);
        vertices[2] = new Vertex(
            new Vector3(1, 0, 0) + this.relativePos,
            vertexWorldPos,
            GetNoiseFloat(vertexWorldPos.x * noiseScale, vertexWorldPos.y * noiseScale, vertexWorldPos.z * noiseScale, noiseIs3D));
        
        
        //bottom left close 
        vertexWorldPos = new Vector3(this.worldPos.x - halfCubeSize, this.worldPos.y - halfCubeSize, this.worldPos.z - halfCubeSize);
        vertices[3] = new Vertex(
            new Vector3(0, 0, 0) + this.relativePos,
            vertexWorldPos,
            GetNoiseFloat(vertexWorldPos.x * noiseScale, vertexWorldPos.y * noiseScale, vertexWorldPos.z * noiseScale, noiseIs3D));
        
        
        //top left far 
        vertexWorldPos = new Vector3(this.worldPos.x - halfCubeSize, this.worldPos.y + halfCubeSize, this.worldPos.z + halfCubeSize);
        vertices[4] = new Vertex(
            new Vector3(0, 1, 1) + this.relativePos,
            vertexWorldPos,
            GetNoiseFloat(vertexWorldPos.x * noiseScale, vertexWorldPos.y * noiseScale, vertexWorldPos.z * noiseScale, noiseIs3D));
        
        //top right far 
        vertexWorldPos = new Vector3(this.worldPos.x + halfCubeSize, this.worldPos.y + halfCubeSize, this.worldPos.z + halfCubeSize);
        vertices[5] = new Vertex(
            new Vector3(1, 1, 1) + this.relativePos,
            vertexWorldPos,
            GetNoiseFloat(vertexWorldPos.x * noiseScale, vertexWorldPos.y * noiseScale, vertexWorldPos.z * noiseScale, noiseIs3D));

        
        //top right close 
        vertexWorldPos = new Vector3(this.worldPos.x + halfCubeSize, this.worldPos.y + halfCubeSize, this.worldPos.z - halfCubeSize);
        vertices[6] = new Vertex(
            new Vector3(1, 1, 0) + this.relativePos,
            vertexWorldPos,
            GetNoiseFloat(vertexWorldPos.x * noiseScale, vertexWorldPos.y * noiseScale, vertexWorldPos.z * noiseScale, noiseIs3D));

        
        //top left close 
        vertexWorldPos = new Vector3(this.worldPos.x - halfCubeSize, this.worldPos.y + halfCubeSize, this.worldPos.z - halfCubeSize);
        vertices[7] = new Vertex(
            new Vector3(0, 1, 0) + this.relativePos,
            vertexWorldPos,
            GetNoiseFloat(vertexWorldPos.x * noiseScale, vertexWorldPos.y * noiseScale, vertexWorldPos.z * noiseScale, noiseIs3D));

        
        
            
        return  vertices;
    }

    public Vertex[] GenerateEdgePoints()
    {
        //I am ignoring relative position and noise scale
        // Relative position might be useful in future!!
        
        Vertex[] edgePoints = new Vertex[12];
        float halfCubeSize = (this.size * 0.5f);

        //bottom middle far
        edgePoints[0] = new Vertex(new Vector3(0, 0, 1) + this.relativePos,
            new Vector3(this.worldPos.x, this.worldPos.y - halfCubeSize, this.worldPos.z + halfCubeSize),
            1);
        
        //bottom right middle
        edgePoints[1] = new Vertex(new Vector3(0, 0, 1) + this.relativePos,
            new Vector3(this.worldPos.x + halfCubeSize, this.worldPos.y - halfCubeSize, this.worldPos.z),
            1);
        
        //bottom middle close
        edgePoints[2] = new Vertex(new Vector3(0, 0, 1) + this.relativePos,
            new Vector3(this.worldPos.x, this.worldPos.y - halfCubeSize, this.worldPos.z - halfCubeSize),
            1);
        
        //bottom left middle
        edgePoints[3] = new Vertex(new Vector3(0, 0, 1) + this.relativePos,
            new Vector3(this.worldPos.x - halfCubeSize, this.worldPos.y - halfCubeSize, this.worldPos.z ),
            1);
        
        //top middle far
        edgePoints[4] = new Vertex(new Vector3(0, 0, 1) + this.relativePos,
            new Vector3(this.worldPos.x, this.worldPos.y + halfCubeSize, this.worldPos.z + halfCubeSize),
            1);
        
        //top right middle
        edgePoints[5] = new Vertex(new Vector3(0, 0, 1) + this.relativePos,
            new Vector3(this.worldPos.x + halfCubeSize, this.worldPos.y + halfCubeSize, this.worldPos.z),
            1);
        
        //top middle close
        edgePoints[6] = new Vertex(new Vector3(0, 0, 1) + this.relativePos,
            new Vector3(this.worldPos.x, this.worldPos.y + halfCubeSize, this.worldPos.z - halfCubeSize),
            1);
        
        //top left middle
        edgePoints[7] = new Vertex(new Vector3(0, 0, 1) + this.relativePos,
            new Vector3(this.worldPos.x - halfCubeSize, this.worldPos.y + halfCubeSize, this.worldPos.z),
            1);
        
        //middle left far
        edgePoints[8] = new Vertex(new Vector3(0, 0, 1) + this.relativePos,
            new Vector3(this.worldPos.x - halfCubeSize, this.worldPos.y, this.worldPos.z + halfCubeSize),
            1);
        
        //middle right far
        edgePoints[9] = new Vertex(new Vector3(0, 0, 1) + this.relativePos,
            new Vector3(this.worldPos.x + halfCubeSize, this.worldPos.y, this.worldPos.z + halfCubeSize),
            1);
        
        //middle right close
        edgePoints[10] = new Vertex(new Vector3(0, 0, 1) + this.relativePos,
            new Vector3(this.worldPos.x + halfCubeSize, this.worldPos.y , this.worldPos.z - halfCubeSize),
            1);
        
        //middle left close
        edgePoints[11] = new Vertex(new Vector3(0, 0, 1) + this.relativePos,
            new Vector3(this.worldPos.x - halfCubeSize, this.worldPos.y , this.worldPos.z - halfCubeSize),
            1);


        
        return edgePoints;
    }

    private float GetNoiseFloat(float x, float y, float z, bool isNoise3D)
    {
        if (isNoise3D) return Perlin3D(x, y, z);
        else return Perlin2D(z, x) * y;
    }
    
    public float Perlin2D(float x, float y)
    {
        return Mathf.PerlinNoise(x, y);
    }
    public float Perlin3D(float x, float y, float z)
    {

        return (float)NoiseS3D.Noise(x / 15, y / 15, z / 15);

        /*  float ab = Mathf.PerlinNoise(x, y);
          float bc = Mathf.PerlinNoise(y, z);
          float ac = Mathf.PerlinNoise(x, z);
  
          float ba = Mathf.PerlinNoise(y, x);
          float cb = Mathf.PerlinNoise(z, y);
          float ca = Mathf.PerlinNoise(z, x);
  
          float abc = ab + bc + ac + ba + cb + ca;
          return abc / 6f;*/
    }
    

}
