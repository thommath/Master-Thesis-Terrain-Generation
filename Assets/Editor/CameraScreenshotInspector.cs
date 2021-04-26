using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(CameraScreenshot))]
public class CameraScreenshotInspector : Editor
{
    CameraScreenshot cameraScreenshot;
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        cameraScreenshot = target as CameraScreenshot;

        if (GUILayout.Button("Screenshot"))
        {
            cameraScreenshot.screenshot();
        }


    }
}