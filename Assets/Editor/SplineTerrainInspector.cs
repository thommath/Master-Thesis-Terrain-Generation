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
            //Undo.RecordObject(spline, "Add Curve");
            foreach ( BezierSpline spline in terrain.splines)
            {
                if (spline.rasterizingData != null)
                {
                    spline.rasterizingData = null;
                }
            }
            terrain.runSolver();

            if (terrain.deleteConstructedItems)
            {
                foreach (BezierSpline spline in terrain.splines)
                {
                    if (spline.rasterizingData != null)
                    {
                        spline.rasterizingData = null;
                    }
                }
            }
            //EditorUtility.SetDirty(spline);
        }


        if (GUILayout.Button("Save RAW"))
        {
            terrain.saveRAW();
        }
    }

    public void OnEnable()
    {
        // Subscribe to callback
        EditorApplication.update += MyUpdate;

    }

    public void OnDisable()
    {
        // Unsubscribe from callback
        EditorApplication.update -= MyUpdate;
    }

    private void MyUpdate()
    {
        terrain = target as SplineTerrain;
    }
}
