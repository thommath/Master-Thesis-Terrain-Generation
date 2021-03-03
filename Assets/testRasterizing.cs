using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class testRasterizing : MonoBehaviour
{

    public ComputeShader computeShader;

    public bool run = false;

    public struct Line
    {
        Vector2 v;
        Vector2 w;
        float h1;
        float h2;

        public Line(Vector2 v, Vector2 w, float h1, float h2)
        {
            this.v = v;
            this.w = w;
            this.h1 = h1;
            this.h2 = h2;
        }
    }
    public struct Triangle
    {
        Vector2 a;
        Vector2 b;
        Vector2 c;

        Vector4 c1;
        Vector4 c2;
        Vector4 c3;

        public Triangle(Vector2 a, Vector2 b, Vector2 c, Vector4 c1, Vector4 c2, Vector4 c3)
        {
            this.a = a;
            this.b = b;
            this.c = c;
            this.c1 = c1;
            this.c2 = c2;
            this.c3 = c3;
        }
    }
    ComputeBuffer lines;
    ComputeBuffer triangles;

    public void getData()
    {
        float time = Time.realtimeSinceStartup;
        Transform terrainFeatures = this.gameObject.transform.parent.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.CompareTag("TerrainFeatures"));
        
        foreach(BezierSpline spline in terrainFeatures.GetComponentsInChildren<BezierSpline>().ToArray())
        {
            spline.rasterizingData = RasterizingTriangles.getSplineData(spline, 2048, 500, 200);
        }
        Debug.Log((Time.realtimeSinceStartup - time) + "s for rasterizing real");
    }

    public void runShader()
    {

        float time = Time.realtimeSinceStartup;

        List<Line> _lines = new List<Line>();
        for(int n = 0; n < 1000; n++)
        {
            _lines.Add(new Line(new Vector2(5, 50), new Vector2(50, 0), 1, 0));
        }
        ComputeBuffer lines = new ComputeBuffer(_lines.Count, sizeof(float) * 6);
        lines.SetData(_lines);


        List<Triangle> _triangles = new List<Triangle>();
        for (int n = 0; n < 100; n++)
        {
            _triangles.Add(new Triangle(new Vector2(5, 50), new Vector2(50, 0), new Vector2(0, 0), new Vector4(0, 0, 1, 1), new Vector4(0, 1, 0, 1), new Vector4(1, 0, 0, 1)));
        }
        ComputeBuffer triangles = new ComputeBuffer(_triangles.Count, sizeof(float) * (6 + 4*3));
        triangles.SetData(_triangles);


        RenderTexture result = new RenderTexture(2048, 2048, 0, RenderTextureFormat.ARGBFloat);
        result.enableRandomWrite = true;
        result.autoGenerateMips = false;
        result.Create();

        Debug.Log((Time.realtimeSinceStartup - time) + "s for rasterizing 1 ");

        int RasterizeKernelHandle = computeShader.FindKernel("Rasterize");
        computeShader.SetBuffer(RasterizeKernelHandle, "lines", lines);
        computeShader.SetBuffer(RasterizeKernelHandle, "triangles", triangles);
        computeShader.SetTexture(RasterizeKernelHandle, "result", result);

        computeShader.Dispatch(RasterizeKernelHandle, result.width, result.height, 1);

        Debug.Log((Time.realtimeSinceStartup - time) + "s for rasterizing 1 ");

        RenderTexture.active = result;
        Texture2D tex2D = new Texture2D(result.width, result.height, TextureFormat.RGBAFloat, true);
        tex2D.ReadPixels(new Rect(0, 0, result.width, result.height), 0, 0, false);
        tex2D.Apply();
        RenderTexture.active = null;
        System.IO.File.WriteAllBytes(Application.dataPath + "/" + "rasterizeTest" + ".png", tex2D.EncodeToPNG());
        Debug.Log("Wrote image to " + Application.dataPath + "/" + "rasterizeTest" + ".png");

    }


    void OnValidate()
    {
        getData();
        runShader();
    }


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
