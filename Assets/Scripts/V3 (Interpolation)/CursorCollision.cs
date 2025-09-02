using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorCollision : MonoBehaviour
{
    public TerrainEditor terrainEditor;
    
    //Cant use colliders 
    //Updating them apperntly is very expensive
    
    private void OnTriggerStay(Collider other)
    {
        if (Input.GetMouseButton(0))
        {
            if (other.gameObject.layer == 6)
            {
                terrainEditor.OnCollisionStayCursor(false);

            }
        }
        else if (Input.GetMouseButton(1))
        {
            if (other.gameObject.layer == 6)
            {
                terrainEditor.OnCollisionStayCursor(true);

            }
        }

        
    }
}
