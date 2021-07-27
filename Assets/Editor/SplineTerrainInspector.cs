using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(SplineTerrain))]
public class TerrainInspector : Editor
{
    SplineTerrain terrain;
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        terrain = target as SplineTerrain;

        if (GUILayout.Button("Solve terrain"))
        {
            terrain.runSolver();
            terrain.GetComponent<TerrainVisualizer>().fastExport();
            terrain.GetComponent<TerrainVisualizer>().saveToFile();
            terrain.GetComponent<TerrainVisualizer>().loadFromFile();
        }

    }

}
