using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ChunkManager))]
public class ChunkManagerInspector : Editor
{
    public override void OnInspectorGUI()
    {
        ChunkManager chunkManager = (ChunkManager)target;

        if (GUILayout.Button("Regenerate Meshes"))
        {
            chunkManager.ReGenerateMeshes();
        }

        DrawDefaultInspector();
    }
}
