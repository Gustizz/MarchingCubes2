using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

//[ExecuteInEditMode]
public class TerrainEditor : MonoBehaviour
{
    public Transform cursor;

    public MarchingCubesInterpolation marchingCubes;
    
    public LayerMask layerMask;

    public float brushWeight;

    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //Cant use colliders
        //Updating them apperntly is very expensive

        
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 600, layerMask))
        {
            Debug.Log(hit.transform.name);
            Debug.Log("hit: " + hit.transform.position.x + " , " + hit.transform.position.y + ", " + hit.transform.position.z);

            cursor.position = hit.point;

            
        }
        
        
    }

    public void OnCollisionStayCursor( bool erasing)
    {
        
        for (int x = 0; x < marchingCubes.cubeArr.GetLength(0); x++)
        {
            for (int y = 0; y < marchingCubes.cubeArr.GetLength(1); y++)
            {
                for (int z = 0; z < marchingCubes.cubeArr.GetLength(2); z++)
                {
                    for (int i = 0; i < marchingCubes.cubeArr[x, y, z].vertices.Length; i++)
                    {
                        Vertex currentVertex = marchingCubes.cubeArr[x, y, z].vertices[i];

                        if (Vector3.Distance(currentVertex.worldPos, cursor.transform.position) <
                            cursor.transform.localScale.x)
                        {

                            if (erasing)
                            {
                                currentVertex.noiseValue = Mathf.Clamp(currentVertex.noiseValue + brushWeight, 0, 1);

                            }
                            else
                            {
                                currentVertex.noiseValue = Mathf.Clamp(currentVertex.noiseValue - brushWeight, 0, 1);

                            }


                        }
                    }
                }
            }
        }
        marchingCubes.mesh =  marchingCubes.GenerateMesh();
        marchingCubes.UpdateMesh();
    }
}
