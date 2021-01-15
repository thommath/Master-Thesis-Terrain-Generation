using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(HydraulicErosion))]
public class HydraulicErosionInspector : Editor
{
    HydraulicErosion hydraulicErosion;
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        hydraulicErosion = target as HydraulicErosion;

        if (GUILayout.Button("Reset erosion"))
        {
             hydraulicErosion.initializeTextures();
        }

        if (GUILayout.Button("Step erosion"))
        {
            hydraulicErosion.runErosion();
        }

    }
}
