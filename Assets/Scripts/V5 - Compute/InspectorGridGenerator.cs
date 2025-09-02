using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GridGenerator))]

public class InspectorGridGenerator : Editor{
    public override void OnInspectorGUI()
    {
        GridGenerator cubeGeneration = (GridGenerator)target;

        if (GUILayout.Button("Upadate Noise"))
        {

            cubeGeneration.GeneratePos();
        }

        DrawDefaultInspector();
    }
}
