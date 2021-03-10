using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;

public class testRasterizing : MonoBehaviour
{
    public ComputeShader computeShader;

    public bool run = false;

    public struct Spline
    {
        Vector3 p1;
        Vector3 p2;
        Vector3 p3;
        Vector3 p4;

        public Spline(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
        {
            this.p1 = p1;
            this.p2 = p2;
            this.p3 = p3;
            this.p4 = p4;
        }
    }

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
    
    private void RasterizeGradientMesh(Mesh mesh, RenderTexture normal, RenderTexture restriction)
    {
        ComputeBuffer verticesGradients = new ComputeBuffer(mesh.vertices.Length, sizeof(float) * 3);
        ComputeBuffer indicesGradients = new ComputeBuffer(mesh.triangles.Length, sizeof(int));
        ComputeBuffer colorsGradients = new ComputeBuffer(mesh.colors.Length, sizeof(float) * 4);
        verticesGradients.SetData(mesh.vertices);
        indicesGradients.SetData(mesh.triangles);
        colorsGradients.SetData(mesh.colors);

        int gradientsKernelHandle = computeShader.FindKernel("RasterizeAverageGradients");
        
        computeShader.SetVector("gradientColorStart", new Vector4(0.7f, 0.3f, 0, 1f));
        computeShader.SetVector("gradientColorEnd", new Vector4(1f, 0f, 0, 1f));

        computeShader.SetTexture(gradientsKernelHandle, "normal", normal);
        computeShader.SetTexture(gradientsKernelHandle, "restriction", restriction);

        computeShader.SetBuffer(gradientsKernelHandle, "vertices", verticesGradients);
        computeShader.SetBuffer(gradientsKernelHandle, "indices", indicesGradients);
        computeShader.SetBuffer(gradientsKernelHandle, "colors", colorsGradients);

        Debug.Log((indicesGradients.count / 3) + " in paralell");

        computeShader.Dispatch(gradientsKernelHandle, indicesGradients.count / 3, 1, 1);
            
        verticesGradients.Release();
        indicesGradients.Release();
        colorsGradients.Release();
    }
    private void RasterizeLineMesh(Mesh mesh, RenderTexture result, RenderTexture restriction)
    {
        ComputeBuffer verticesGradients = new ComputeBuffer(mesh.vertices.Length, sizeof(float) * 3);
        ComputeBuffer indicesGradients = new ComputeBuffer(mesh.triangles.Length, sizeof(int));
        ComputeBuffer colorsGradients = new ComputeBuffer(mesh.colors.Length, sizeof(float) * 4);
        verticesGradients.SetData(mesh.vertices);
        indicesGradients.SetData(mesh.triangles);
        colorsGradients.SetData(mesh.colors);

        int linesKernelHandle = computeShader.FindKernel("RasterizeAverageThickLines");
        
        computeShader.SetTexture(linesKernelHandle, "result", result);
        computeShader.SetTexture(linesKernelHandle, "restriction", restriction);

        computeShader.SetBuffer(linesKernelHandle, "vertices", verticesGradients);
        computeShader.SetBuffer(linesKernelHandle, "indices", indicesGradients);
        computeShader.SetBuffer(linesKernelHandle, "colors", colorsGradients);

        computeShader.Dispatch(linesKernelHandle, indicesGradients.count / 3, 1, 1);
            
        verticesGradients.Release();
        indicesGradients.Release();
        colorsGradients.Release();
    }
    
    public RasterizedData drawLinesAndTriangles(IEnumerable<BezierSpline> splines, int terrainSize, int textureSize, int maxHeight)
    {
        RenderTexture result = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGBFloat);
        result.enableRandomWrite = true;
        result.autoGenerateMips = false;
        result.Create();
        RenderTexture restriction = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGBFloat);
        restriction.enableRandomWrite = true;
        restriction.autoGenerateMips = false;
        restriction.Create();
        RenderTexture normal = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGBFloat);
        normal.enableRandomWrite = true;
        normal.autoGenerateMips = false;
        normal.Create();
        RenderTexture noise = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGBFloat);
        noise.enableRandomWrite = true;
        noise.autoGenerateMips = false;
        noise.Create();
        
        
        int gradientsKernelHandle = computeShader.FindKernel("RasterizeSplines");

        computeShader.SetFloat("textureDivTerrain", 1f * textureSize / terrainSize);
        computeShader.SetVector("center", new Vector2(terrainSize * 0.5f, terrainSize * 0.5f));
        computeShader.SetFloat("maxHeight", maxHeight);

        computeShader.SetTexture(gradientsKernelHandle, "result", result);
        computeShader.SetTexture(gradientsKernelHandle, "noise", noise);
        computeShader.SetTexture(gradientsKernelHandle, "restriction", restriction);
        computeShader.SetTexture(gradientsKernelHandle, "normal", normal);

        computeShader.SetInt("width", textureSize);
        computeShader.SetInt("height", textureSize);
        
        foreach (BezierSpline spline in splines)
        {
            RasterizeGradientMesh(spline.rasterizingData.meshLeft, normal, restriction);
            RasterizeGradientMesh(spline.rasterizingData.meshRight, normal, restriction);
            RasterizeLineMesh(spline.rasterizingData.meshLine, result, restriction);

            List<Spline> gpuSplines = new List<Spline>();

            for (int n = 0; n + 3 < spline.points.Length; n += 3)
            {
                gpuSplines.Add(new Spline(
                    spline.points[n],
                    spline.points[n + 1],
                    spline.points[n + 2],
                    spline.points[n + 3]
                ));
            }
            Debug.Log(gpuSplines.Count);

            ComputeBuffer splinesBuffer = new ComputeBuffer(gpuSplines.Count, sizeof(float) * 3 * 4);
            splinesBuffer.SetData(gpuSplines);

            computeShader.SetVector("position", spline.transform.position);
            computeShader.SetBuffer(gradientsKernelHandle, "splines", splinesBuffer);
            computeShader.SetMatrix("localToWorld", spline.transform.localToWorldMatrix);

            computeShader.Dispatch(gradientsKernelHandle, splinesBuffer.count, 1, 1);
            
            splinesBuffer.Release();
        }
        ///////////////
        ///
        /// Post processing
        ///
        ///////////////
        
        int fillTexturesKernelHandle = computeShader.FindKernel("FillTextures");
        computeShader.SetTexture(fillTexturesKernelHandle, "restriction", restriction);
        computeShader.SetTexture(fillTexturesKernelHandle, "normal", normal);
        computeShader.Dispatch(fillTexturesKernelHandle, restriction.width, restriction.height, 1);

        
        RasterizedData data = new RasterizedData();
        data.noise = noise;
        data.restrictions = restriction;
        data.seedHeightmap = result;
        data.seedNormals = normal;
        return data;
        /*
        ///////////////
        ///
        /// Time process by reading one pixel
        ///
        ///////////////
        RenderTexture.active = result;
        Texture2D tex2D = new Texture2D(result.width, result.height, TextureFormat.RGB24, false);
        tex2D.ReadPixels(new Rect(0, 0, 1, 1), 0, 0, false);
        Debug.Log((Time.realtimeSinceStartup - time) + "s reading one pixel");
        
        ///////////////
        ///
        /// Save images
        ///
        ///////////////
        tex2D.ReadPixels(new Rect(0, 0, result.width, result.height), 0, 0, false);
        tex2D.Apply();
        RenderTexture.active = null;
        System.IO.File.WriteAllBytes(Application.dataPath + "/images/" + "rasterizeTest" + textureSize + ".png", tex2D.EncodeToPNG());
        Debug.Log("Wrote image to " + Application.dataPath + "/images/" + "rasterizeTest" + textureSize + ".png");

        
        RenderTexture.active = normal;
        tex2D.ReadPixels(new Rect(0, 0, result.width, result.height), 0, 0, false);
        tex2D.Apply();
        RenderTexture.active = null;
        System.IO.File.WriteAllBytes(Application.dataPath + "/images/" + "normal" + textureSize + ".png", tex2D.EncodeToPNG());
        Debug.Log("Wrote image to " + Application.dataPath + "/images/" + "normal" + textureSize + ".png");

        RenderTexture.active = noise;
        tex2D.ReadPixels(new Rect(0, 0, result.width, result.height), 0, 0, false);
        tex2D.Apply();
        RenderTexture.active = null;
        System.IO.File.WriteAllBytes(Application.dataPath + "/images/" + "noise" + textureSize + ".png", tex2D.EncodeToPNG());
        Debug.Log("Wrote image to " + Application.dataPath + "/images/" + "noise" + textureSize + ".png");

        RenderTexture.active = restriction;
        tex2D.ReadPixels(new Rect(0, 0, result.width, result.height), 0, 0, false);
        tex2D.Apply();
        RenderTexture.active = null;
        System.IO.File.WriteAllBytes(Application.dataPath + "/images/" + "restriction" + textureSize + ".png", tex2D.EncodeToPNG());
        Debug.Log("Wrote image to " + Application.dataPath + "/images/" + "restriction" + textureSize + ".png");

        Debug.Log((Time.realtimeSinceStartup - time) + "s writing image");

        result.Release();
        normal.Release();
        restriction.Release();
        noise.Release();
        */
    }

    public Dictionary<int, RasterizedData> rasterizeData(int terrainSizeExp, int textureSizeExp, int breakOnLevel, int maxHeight, int resolution)
    {
        float time = Time.realtimeSinceStartup;
        Transform terrainFeatures = this.gameObject.transform.GetComponentsInChildren<Transform>()
            .FirstOrDefault(x => x.CompareTag("TerrainFeatures"));
        BezierSpline[] splines = terrainFeatures.GetComponentsInChildren<BezierSpline>().ToArray();
        
        foreach (BezierSpline spline in splines)
        {
            spline.rasterizingData = RasterizingTriangles.getSplineData(spline, 0, maxHeight, resolution);
        }

        Dictionary<int, RasterizedData> dict = new Dictionary<int, RasterizedData>();

        int terrainSize = Mathf.RoundToInt(Mathf.Pow(2, terrainSizeExp));

        for(int n = textureSizeExp; n > breakOnLevel; n--)
        {
            int textureSize = Mathf.RoundToInt(Mathf.Pow(2, n)) + 1;
            RasterizedData rd = drawLinesAndTriangles(splines, terrainSize, textureSize, maxHeight);
            dict.Add(textureSize, rd);
        }
        
        RasterizedData rd2;
        dict.TryGetValue(Mathf.RoundToInt(Mathf.Pow(2, textureSizeExp)) + 1, out rd2);
        /*
        RenderTexture.active = rd2.restrictions;
        Texture2D tex2D = new Texture2D(rd2.restrictions.width, rd2.restrictions.height, TextureFormat.RGBAFloat, false);
        tex2D.ReadPixels(new Rect(0, 0, 1, 1), 0, 0, false);
        Debug.Log((Time.realtimeSinceStartup - time) + "s All done, read one pixel");
        */
        return dict;
    }


    public void drawLines()
    {
        float time = Time.realtimeSinceStartup;
        Transform terrainFeatures = this.gameObject.transform.parent.GetComponentsInChildren<Transform>()
            .FirstOrDefault(x => x.CompareTag("TerrainFeatures"));
        BezierSpline[] splines = terrainFeatures.GetComponentsInChildren<BezierSpline>().ToArray();

        int textureSize = 2048 / 4;

        RenderTexture result = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGBFloat);
        result.enableRandomWrite = true;
        result.autoGenerateMips = false;
        result.Create();
        RenderTexture restriction = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGBFloat);
        restriction.enableRandomWrite = true;
        restriction.autoGenerateMips = false;
        restriction.Create();
        RenderTexture normal = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGBFloat);
        normal.enableRandomWrite = true;
        normal.autoGenerateMips = false;
        normal.Create();
        RenderTexture noise = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGBFloat);
        noise.enableRandomWrite = true;
        noise.autoGenerateMips = false;
        noise.Create();

        foreach (BezierSpline spline in splines)
        {
            List<Spline> gpuSplines = new List<Spline>();
            // spline.rasterizingData = RasterizingTriangles.getSplineData(spline, 0, 500, 50);

            for (int n = 0; n + 3 < spline.points.Length; n += 3)
            {
                gpuSplines.Add(new Spline(
                    spline.points[n],
                    spline.points[n + 1],
                    spline.points[n + 2],
                    spline.points[n + 3]
                ));
            }
            Debug.Log(gpuSplines.Count);

            ComputeBuffer splinesBuffer = new ComputeBuffer(gpuSplines.Count, sizeof(float) * 3 * 4);
            splinesBuffer.SetData(gpuSplines);
            Debug.Log((Time.realtimeSinceStartup - time) + "s for rasterizing real");

            Debug.Log((Time.realtimeSinceStartup - time) + "s for cp");

            int gradientsKernelHandle = computeShader.FindKernel("RasterizeSplines");

            computeShader.SetFloat("textureDivTerrain", 1f * textureSize / 2048);
            computeShader.SetVector("center", new Vector2(2048 * 0.5f, 2048 * 0.5f));
            computeShader.SetVector("position", spline.transform.position);
            computeShader.SetFloat("maxHeight", 500);
            
            computeShader.SetInt("width", textureSize);
            computeShader.SetInt("height", textureSize);

            computeShader.SetTexture(gradientsKernelHandle, "result", result);
            computeShader.SetTexture(gradientsKernelHandle, "noise", noise);
            computeShader.SetTexture(gradientsKernelHandle, "restriction", restriction);
            computeShader.SetTexture(gradientsKernelHandle, "normal", normal);

            computeShader.SetBuffer(gradientsKernelHandle, "splines", splinesBuffer);
            
            computeShader.SetMatrix("localToWorld", spline.transform.localToWorldMatrix);

            computeShader.Dispatch(gradientsKernelHandle, splinesBuffer.count, 1, 1);
        }

        ///////////////
        ///
        /// Time process by reading one pixel
        ///
        ///////////////
        RenderTexture.active = result;
        Texture2D tex2D = new Texture2D(result.width, result.height, TextureFormat.RGB24, false);
        tex2D.ReadPixels(new Rect(0, 0, 1, 1), 0, 0, false);
        Debug.Log((Time.realtimeSinceStartup - time) + "s reading one pixel");
        
        ///////////////
        ///
        /// Save images
        ///
        ///////////////
        tex2D.ReadPixels(new Rect(0, 0, result.width, result.height), 0, 0, false);
        tex2D.Apply();
        RenderTexture.active = null;
        System.IO.File.WriteAllBytes(Application.dataPath + "/" + "rasterizeTest" + ".png", tex2D.EncodeToPNG());
        Debug.Log("Wrote image to " + Application.dataPath + "/" + "rasterizeTest" + ".png");

        
        RenderTexture.active = normal;
        tex2D.ReadPixels(new Rect(0, 0, result.width, result.height), 0, 0, false);
        tex2D.Apply();
        RenderTexture.active = null;
        System.IO.File.WriteAllBytes(Application.dataPath + "/" + "normal" + ".png", tex2D.EncodeToPNG());
        Debug.Log("Wrote image to " + Application.dataPath + "/" + "normal" + ".png");

        RenderTexture.active = noise;
        tex2D.ReadPixels(new Rect(0, 0, result.width, result.height), 0, 0, false);
        tex2D.Apply();
        RenderTexture.active = null;
        System.IO.File.WriteAllBytes(Application.dataPath + "/" + "noise" + ".png", tex2D.EncodeToPNG());
        Debug.Log("Wrote image to " + Application.dataPath + "/" + "noise" + ".png");

        RenderTexture.active = restriction;
        tex2D.ReadPixels(new Rect(0, 0, result.width, result.height), 0, 0, false);
        tex2D.Apply();
        RenderTexture.active = null;
        System.IO.File.WriteAllBytes(Application.dataPath + "/" + "restriction" + ".png", tex2D.EncodeToPNG());
        Debug.Log("Wrote image to " + Application.dataPath + "/" + "restriction" + ".png");

        Debug.Log((Time.realtimeSinceStartup - time) + "s writing image");
        
        result.Release();
        normal.Release();
        restriction.Release();
        noise.Release();
    }


    ComputeBuffer lines;
    ComputeBuffer triangles;

    public void getData()
    {
        float time = Time.realtimeSinceStartup;
        Transform terrainFeatures = this.gameObject.transform.parent.GetComponentsInChildren<Transform>()
            .FirstOrDefault(x => x.CompareTag("TerrainFeatures"));
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
        for (int n = 0; n < 1000; n++)
        {
            _lines.Add(new Line(new Vector2(5, 50), new Vector2(50, 0), 1, 0));
        }

        ComputeBuffer lines = new ComputeBuffer(_lines.Count, sizeof(float) * 6);
        lines.SetData(_lines);


        List<Triangle> _triangles = new List<Triangle>();
        for (int n = 0; n < 100; n++)
        {
            _triangles.Add(new Triangle(new Vector2(5, 50), new Vector2(50, 0), new Vector2(0, 0),
                new Vector4(0, 0, 1, 1), new Vector4(0, 1, 0, 1), new Vector4(1, 0, 0, 1)));
        }

        ComputeBuffer triangles = new ComputeBuffer(_triangles.Count, sizeof(float) * (6 + 4 * 3));
        triangles.SetData(_triangles);


        RenderTexture result = new RenderTexture(2048, 2048, 0, RenderTextureFormat.ARGBFloat);
        result.enableRandomWrite = true;
        result.autoGenerateMips = false;
        result.Create();

        Debug.Log((Time.realtimeSinceStartup - time) + "s for rasterizing 1 ");

        int RasterizeKernelHandle = computeShader.FindKernel("RasterizeLines");
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
        Debug.Log((Time.realtimeSinceStartup - time) + "s all done ");
    }


    public void trianglesParalell()
    {
        float time = Time.realtimeSinceStartup;
        Transform terrainFeatures = this.gameObject.transform.parent.GetComponentsInChildren<Transform>()
            .FirstOrDefault(x => x.CompareTag("TerrainFeatures"));
        BezierSpline[] splines = terrainFeatures.GetComponentsInChildren<BezierSpline>().ToArray();

        int textureSize = 2048;

        RenderTexture result = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGBFloat);
        result.enableRandomWrite = true;
        result.autoGenerateMips = false;
        result.Create();
        RenderTexture result2 = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGBFloat);
        result2.enableRandomWrite = true;
        result2.autoGenerateMips = false;
        result2.Create();

        foreach (BezierSpline spline in splines)
        {
            spline.rasterizingData = RasterizingTriangles.getSplineData(spline, 0, 500, 50);
            
            RasterizeMesh(spline.rasterizingData.meshLeft, textureSize, result);
            RasterizeMesh(spline.rasterizingData.meshRight, textureSize, result);
            RasterizeMesh(spline.rasterizingData.meshLine, textureSize, result);
        }

        Debug.Log((Time.realtimeSinceStartup - time) + "s for rasterizing real");

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
    }

    private void RasterizeMesh(Mesh mesh, int textureSize, RenderTexture result)
    {
        ComputeBuffer verticesGradients = new ComputeBuffer(mesh.vertices.Length, sizeof(float) * 3);
        ComputeBuffer indicesGradients = new ComputeBuffer(mesh.triangles.Length, sizeof(int));
        ComputeBuffer colorsGradients = new ComputeBuffer(mesh.colors.Length, sizeof(float) * 4);
        verticesGradients.SetData(mesh.vertices);
        indicesGradients.SetData(mesh.triangles);
        colorsGradients.SetData(mesh.colors);

        int gradientsKernelHandle = computeShader.FindKernel("RasterizeAverageGradients");

        computeShader.SetFloat("textureDivTerrain", 1f * textureSize / 2048);
        computeShader.SetVector("center", new Vector2(2048 * 0.5f, 2048 * 0.5f));

        computeShader.SetInt("width", textureSize);
        computeShader.SetInt("height", textureSize);

        computeShader.SetTexture(gradientsKernelHandle, "result", result);

        computeShader.SetBuffer(gradientsKernelHandle, "vertices", verticesGradients);
        computeShader.SetBuffer(gradientsKernelHandle, "indices", indicesGradients);
        computeShader.SetBuffer(gradientsKernelHandle, "colors", colorsGradients);

        Debug.Log((indicesGradients.count / 3) + " in paralell");

        computeShader.Dispatch(gradientsKernelHandle, indicesGradients.count / 3, 1, 1);
            
        verticesGradients.Release();
        indicesGradients.Release();
        colorsGradients.Release();
    }

    void OnValidate()
    {
        // trianglesParalell();
        //getData();
        //runShader();
        // drawLines();
        // rasterizeData(8,  8, 4);
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