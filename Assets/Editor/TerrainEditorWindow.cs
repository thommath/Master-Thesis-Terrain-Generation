using UnityEngine;
using UnityEditor;
using UnityEngine.Experimental.TerrainAPI;

public class TerrainEditorWindow : EditorWindow
{
    // Add menu named "My Window" to the Window menu
    [MenuItem("Window/Terrain Editor")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        TerrainEditorWindow window = (TerrainEditorWindow)EditorWindow.GetWindow(typeof(TerrainEditorWindow));
        window.Show();
    }

    private int metaPointIndex = 9999;

    public void SetMetaPoint(int metaPointIndex)
    {
        this.metaPointIndex = metaPointIndex;
    }

    void OnGUI()
    {
        Color before = GUI.backgroundColor;

        // Terrain part
        SplineTerrain terrain = null;
        if (Selection.activeGameObject && Selection.activeGameObject.GetComponentInParent<SplineTerrain>())
        {
            GUILayout.Space(10);
            terrain = Selection.activeGameObject.GetComponentInParent<SplineTerrain>();
            GUILayout.Label("Terrain options", EditorStyles.boldLabel);
            
            bool erode = EditorGUILayout.Toggle("Erode the terrain", terrain.erode);
            if (erode != terrain.erode)
            {
                terrain.erode = erode;
                Undo.RecordObject(terrain, "Toggle Erode");
                EditorUtility.SetDirty(terrain);
            }

            TerrainVisualizer visualizer = null;
            if (Selection.activeGameObject.GetComponentInParent<TerrainVisualizer>())
            {

                if (erode)
                {
                    visualizer = Selection.activeGameObject.GetComponentInParent<TerrainVisualizer>();
                    GUILayout.Space(10);
                    bool renderErosion = EditorGUILayout.Toggle("Render erosion", visualizer.viewMode == TerrainVisualizer.ViewMode.Heightmap);
                    if ((visualizer.viewMode == TerrainVisualizer.ViewMode.Heightmap) != renderErosion)
                    {
                        visualizer.viewMode = renderErosion ? TerrainVisualizer.ViewMode.Heightmap : TerrainVisualizer.ViewMode.HeightmapNoErosion;
                        Undo.RecordObject(visualizer, "Toggle viewMode");
                        EditorUtility.SetDirty(visualizer);
                        visualizer.fastExport();
                    }
                }
            }
            HydraulicErosion erosion = null;
            if (Selection.activeGameObject.GetComponentInParent<HydraulicErosion>())
            {
                erosion = Selection.activeGameObject.GetComponentInParent<HydraulicErosion>();
                GUILayout.Space(10);
                int seed = EditorGUILayout.IntSlider("Erosion seed", erosion.seed, 0, 20);
                if (erosion.seed != seed)
                {
                    erosion.seed = seed;
                    Undo.RecordObject(erosion, "Change seed");
                    EditorUtility.SetDirty(erosion);
                }
            }
            
            GUILayout.Space(10);
            GUI.backgroundColor = new Color(138f / 255, 242f / 255, 116f / 255);
            if (GUILayout.Button("Render terrain"))
            {
                terrain.runSolver();
                terrain.GetComponent<TerrainVisualizer>().fastExport();
                terrain.GetComponent<TerrainVisualizer>().saveToFile();
                terrain.GetComponent<TerrainVisualizer>().loadFromFile();
            }
            GUI.backgroundColor = before;
            GUILayout.Space(10);

            if (GUILayout.Button("Add Spline"))
            {
                GameObject newSpline = terrain.AddSpline();
                Undo.RegisterCreatedObjectUndo(newSpline, "Add Spline");
                Selection.activeGameObject = newSpline;
            }
        }

        // Spline part
        BezierSpline spline = null;
        if (Selection.activeGameObject && Selection.activeGameObject.TryGetComponent<BezierSpline>(out spline))
        {
            GUILayout.Space(20);
            GUILayout.Label("Spline options", EditorStyles.boldLabel);
            
            bool elevationConstraint = EditorGUILayout.Toggle("Elevation constraints", spline.elevationConstraint);
            spline.elevationConstraint = elevationConstraint;
            bool noiseConstraint = EditorGUILayout.Toggle("Noise constraints", spline.noiseConstraint);
            spline.noiseConstraint = noiseConstraint;
            bool warpConstraint = EditorGUILayout.Toggle("Warp constraints", spline.warpConstraint);
            spline.warpConstraint = warpConstraint;
            bool erosionConstraint = EditorGUILayout.Toggle("Erosion constraints", spline.erosionConstraint);
            spline.erosionConstraint = erosionConstraint;

            GUILayout.Space(10);
            GUI.backgroundColor = before;
            if (GUILayout.Button("Add Curve"))
            {
                Undo.RecordObject(spline, "Add Curve");
                EditorUtility.SetDirty(spline);
                spline.AddCurve();
            }
                GUI.backgroundColor = new Color(230f / 255, 122f / 255, 127f / 255);
            if (GUILayout.Button("Remove Curve"))
            {
                Undo.RecordObject(spline, "Remove Curve");
                EditorUtility.SetDirty(spline);
                spline.RemoveCurve();
            }

            GUILayout.Space(5);
            GUI.backgroundColor = before;
            if (GUILayout.Button("Add Meta Point"))
            {
                Undo.RecordObject(spline, "Add Meta Point");
                EditorUtility.SetDirty(spline);
                spline.AddMetaPoint();
            }
        }

        // Meta point part
        if (spline != null && metaPointIndex < spline.metaPoints.Length)
        {
            SplineMetaPoint metaPoint = spline.metaPoints[metaPointIndex];

            GUILayout.Space(20);
            GUILayout.Label("Meta Point options", EditorStyles.boldLabel);

            float position = EditorGUILayout.Slider("Position", metaPoint.position, 0, spline.CurveCount);
            metaPoint.position = position;

            if (spline.noiseConstraint)
            {
                GUILayout.Space(5);
                float noiseAmplitude = EditorGUILayout.Slider("Noise Amplitude", metaPoint.noiseAmplitude, 0, 1);
                metaPoint.noiseAmplitude = noiseAmplitude;
                float noiseRoughness = EditorGUILayout.Slider("Noise Roughness", metaPoint.noiseRoughness, 0, 1);
                metaPoint.noiseRoughness = noiseRoughness;
            }
            
            if (spline.warpConstraint)
            {
                GUILayout.Space(5);
                float warpA = EditorGUILayout.Slider("Warp A", metaPoint.warpA, 0, 1);
                metaPoint.warpA = warpA;
                float warpB = EditorGUILayout.Slider("Warp B", metaPoint.warpB, 0, 1);
                metaPoint.warpB = warpB;
            }
            
            if (spline.erosionConstraint)
            {
                GUILayout.Space(5);
                float erosionRain = EditorGUILayout.Slider("Erosion Rain", metaPoint.erosionRain, 0, 1);
                metaPoint.erosionRain = erosionRain;
                float erosionHardness = EditorGUILayout.Slider("Erosion hardness", metaPoint.erosionHardness, 0, 1);
                metaPoint.erosionHardness = erosionHardness;
                float sedimentCapacity = EditorGUILayout.Slider("Erosion hardness", metaPoint.sedimentCapacity, 0, 1);
                metaPoint.sedimentCapacity = sedimentCapacity;
            }

            Undo.RecordObject(spline, "Update Meta Point");
            EditorUtility.SetDirty(spline);

            GUILayout.Space(10);
            GUI.backgroundColor = new Color(230f / 255, 122f / 255, 127f / 255);
            if (GUILayout.Button("Remove Meta Point"))
            {
                Undo.RecordObject(spline, "Remove Meta Point");
                EditorUtility.SetDirty(spline);
                spline.RemoveMetaPoint(metaPointIndex);
            }
        } else
        {
            SetMetaPoint(9999);
        }

        if (spline == null && terrain == null)
        {
            GUILayout.Label("Select terrain or a spline to edit terrain");
        }

        /*
        myString = EditorGUILayout.TextField("Text Field", myString);

        groupEnabled = EditorGUILayout.BeginToggleGroup("Optional Settings", groupEnabled);
        myBool = EditorGUILayout.Toggle("Toggle", myBool);
        myFloat = EditorGUILayout.Slider("Slider", myFloat, -3, 3);
        EditorGUILayout.EndToggleGroup();
        */
    }
    void OnInspectorUpdate()
    {
        Repaint();
    }
}