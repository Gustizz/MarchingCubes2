using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class SpawnOres : MonoBehaviour
{
    public List<GameObject> OrePrefabs;
    public ChunkLoader2 chunkLoader;

    [Range(0,100)]
    public float threshold;
    
    public int iterations;
    
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < iterations; i++)
        {

            float randX = Random.Range(-chunkLoader.radius / 1.5f, chunkLoader.radius / 1.5f);
            float randY = Random.Range(-chunkLoader.radius / 1.5f, chunkLoader.radius / 1.5f);
            float randZ = Random.Range(-chunkLoader.radius / 1.5f, chunkLoader.radius / 1.5f);

            float distFromOrigin = Vector3.Distance(new Vector3(randX, randY, randZ), Vector3.zero);
            
            float noiseVal = ((float)NoiseS3D.Noise(randX / 15, randY / 15, randZ / 15) + 1) * distFromOrigin;
            
            Debug.Log("( " + randX + " , " + randY + " , " + randZ + " ) | DIST:  " + distFromOrigin +" | VAL: " + noiseVal);


            if (noiseVal < threshold)
            {
                Instantiate(OrePrefabs[0], new Vector3(randX, randY, randZ), Random.rotation);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
