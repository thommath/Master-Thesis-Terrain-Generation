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
        BezierSpline[] splines = terrainFeatures.GetComponentsInChildren<BezierSpline>().ToArray();
        foreach (BezierSpline spline in splines)
        {
            spline.rasterizingData = RasterizingTriangles.getSplineData(spline, 0, 500, 50);
        }

        Debug.Log((Time.realtimeSinceStartup - time) + "s for rasterizing real");

        RasterizingSplineData rsd = splines[0].rasterizingData;

        ComputeBuffer verticesGradients = new ComputeBuffer(rsd.meshLeft.vertexCount, sizeof(float) * 3);
        ComputeBuffer indicesGradients = new ComputeBuffer(rsd.meshLeft.triangles.Length, sizeof(int));
        ComputeBuffer colorsGradients = new ComputeBuffer(rsd.meshLeft.vertexCount, sizeof(float) * 4);
        verticesGradients.SetData(rsd.meshLeft.vertices);
        indicesGradients.SetData(rsd.meshLeft.triangles);
        colorsGradients.SetData(rsd.meshLeft.colors);

        ComputeBuffer verticesLines = new ComputeBuffer(rsd.meshLine.vertexCount, sizeof(float) * 3);
        ComputeBuffer indicesLines = new ComputeBuffer(rsd.meshLine.triangles.Length, sizeof(int));
        ComputeBuffer colorsLines = new ComputeBuffer(rsd.meshLine.vertexCount, sizeof(float) * 4);
        verticesLines.SetData(rsd.meshLine.vertices);
        indicesLines.SetData(rsd.meshLine.triangles);
        colorsLines.SetData(rsd.meshLine.colors);

        int textureSize = 2048;

        RenderTexture result = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGBFloat);
        result.enableRandomWrite = true;
        result.autoGenerateMips = false;
        result.Create();
        RenderTexture result2 = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGBFloat);
        result2.enableRandomWrite = true;
        result2.autoGenerateMips = false;
        result2.Create();

        Debug.Log((Time.realtimeSinceStartup - time) + "s for cp");

        int gradientsKernelHandle = computeShader.FindKernel("RasterizeGradients");

        computeShader.SetFloat("textureDivTerrain", 1f * textureSize / 2048);
        computeShader.SetVector("center", new Vector2(2048 * 0.5f, 2048 * 0.5f));

        computeShader.SetTexture(gradientsKernelHandle, "result", result);

        computeShader.SetBuffer(gradientsKernelHandle, "vertices", verticesGradients);
        computeShader.SetBuffer(gradientsKernelHandle, "indices", indicesGradients);
        computeShader.SetBuffer(gradientsKernelHandle, "colors", colorsGradients);

        computeShader.Dispatch(gradientsKernelHandle, result.width, result.height, 1);

        int thickLinesKernelHandle = computeShader.FindKernel("RasterizeThickLines");
        computeShader.SetTexture(thickLinesKernelHandle, "result", result);

        computeShader.SetBuffer(thickLinesKernelHandle, "vertices", verticesLines);
        computeShader.SetBuffer(thickLinesKernelHandle, "indices", indicesLines);
        computeShader.SetBuffer(thickLinesKernelHandle, "colors", colorsLines);

        computeShader.Dispatch(thickLinesKernelHandle, result.width, result.height, 1);

        Debug.Log((Time.realtimeSinceStartup - time) + "s done");


        RenderTexture.active = result;
        Texture2D tex2D = new Texture2D(result.width, result.height, TextureFormat.RGB24, false);
        tex2D.ReadPixels(new Rect(0, 0, result.width, result.height), 0, 0, false);
        tex2D.Apply();
        RenderTexture.active = null;
        System.IO.File.WriteAllBytes(Application.dataPath + "/" + "rasterizeTest" + ".png", tex2D.EncodeToPNG());
        Debug.Log("Wrote image to " + Application.dataPath + "/" + "rasterizeTest" + ".png");

        Debug.Log((Time.realtimeSinceStartup - time) + "s writing image");


        result.Release();
        result2.Release();

        verticesGradients.Release();
        indicesGradients.Release();
        colorsGradients.Release();
        verticesLines.Release();
        indicesLines.Release();
        colorsLines.Release();


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
        //getData();
        //runShader();
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
