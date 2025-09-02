using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CubeGeneration))]
public class CustomInspector : Editor
{

    public override void OnInspectorGUI()
    {
        CubeGeneration cubeGeneration = (CubeGeneration)target;

        if (GUILayout.Button("Create Mesh"))
        {
            cubeGeneration.pointsArr = cubeGeneration.GeneratePoints();
            cubeGeneration.cubeArr = cubeGeneration.GenerateCubes();
            cubeGeneration.CreateMesh();
        }

        DrawDefaultInspector();
    }
}
