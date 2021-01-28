using UnityEngine;
using UnityEditor;

public class TerrainEditorWindow : EditorWindow
{
    string myString = "Hello World";
    bool groupEnabled;
    bool myBool = true;
    float myFloat = 1.23f;

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
        if (Selection.activeGameObject && (Selection.activeGameObject.TryGetComponent<SplineTerrain>(out terrain) || Selection.activeGameObject.transform.parent.TryGetComponent<SplineTerrain>(out terrain) || Selection.activeGameObject.transform.parent.parent.TryGetComponent<SplineTerrain>(out terrain)))
        {
            GUILayout.Label("Terrain options", EditorStyles.boldLabel);
            GUI.backgroundColor = new Color(138f / 255, 242f / 255, 116f / 255);
            if (GUILayout.Button("Render terrain"))
            {
                terrain.runSolver();
                terrain.GetComponent<TerrainVisualizer>().fastExport();
            }
            GUI.backgroundColor = before;


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

            float noiseAmplitude = EditorGUILayout.Slider("Noise Amplitude", metaPoint.noiseAmplitude, 0, 1);
            metaPoint.noiseAmplitude = noiseAmplitude;
            float noiseRoughness = EditorGUILayout.Slider("Noise Roughness", metaPoint.noiseRoughness, 0, 1);
            metaPoint.noiseRoughness = noiseRoughness;

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