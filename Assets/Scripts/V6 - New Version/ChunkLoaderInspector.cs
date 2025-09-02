using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ChunkLoader2))]

public class ChunkLoaderInspector : Editor
{
    public override void OnInspectorGUI()
    {
        ChunkLoader2 chunkLoader = (ChunkLoader2)target;

        if (GUILayout.Button("Reload all chunks"))
        {
            chunkLoader.UpdateAllChunks();
        }
        if (GUILayout.Button("Reload all Visible chunks"))
        {
            chunkLoader.UpdateAllVisibleChunks();
        }
        DrawDefaultInspector();
    }
}
