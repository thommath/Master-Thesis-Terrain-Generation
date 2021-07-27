using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;

public class RasterizeWithShader : MonoBehaviour
{
    public ComputeShader computeShader;

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
    public struct MetaPoint
    {
        float position;
        float lineRadius;
        float gradientLengthLeft;
        float gradientAngleLeft;
        float gradientLengthRight;
        float gradientAngleRight;
        
        float noiseAmplitude;
        float noiseRoughness;
        
        float warpA;
        float warpB;

        float erosionRain;
        float sedimentCapacity;

        public MetaPoint(SplineMetaPoint metaPoint)
        {
            position = metaPoint.position;
            lineRadius = metaPoint.lineRadius;
            gradientLengthLeft = metaPoint.gradientLengthLeft;
            gradientAngleLeft = metaPoint.gradientAngleLeft;
            gradientLengthRight = metaPoint.gradientLengthRight;
            gradientAngleRight = metaPoint.gradientAngleRight;
            
            noiseAmplitude = metaPoint.noiseAmplitude;
            noiseRoughness = metaPoint.noiseRoughness;
            warpA = metaPoint.warpA;
            warpB = metaPoint.warpB;
            
            erosionRain = metaPoint.erosionRain;
            sedimentCapacity = metaPoint.sedimentCapacity;

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
        float gradientStart = 0.3f;
        computeShader.SetVector("gradientColorStart", new Vector4(1f-gradientStart, gradientStart, 0, 1f));
        computeShader.SetVector("gradientColorEnd", new Vector4(1f, 0f, 0, 1f));

        computeShader.SetTexture(gradientsKernelHandle, "normal", normal);
        computeShader.SetTexture(gradientsKernelHandle, "restriction", restriction);

        computeShader.SetBuffer(gradientsKernelHandle, "vertices", verticesGradients);
        computeShader.SetBuffer(gradientsKernelHandle, "indices", indicesGradients);
        computeShader.SetBuffer(gradientsKernelHandle, "colors", colorsGradients);

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
    
    public RasterizedData drawLinesAndTriangles(IEnumerable<BezierSpline> splines, int terrainSize, int textureSize, int maxHeight, int resolution)
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
        RenderTexture warp = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGBFloat);
        warp.enableRandomWrite = true;
        warp.autoGenerateMips = false;
        warp.Create();
        RenderTexture erosion = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.ARGBFloat);
        erosion.enableRandomWrite = true;
        erosion.autoGenerateMips = false;
        erosion.Create();
        
        int gradientsKernelHandle = computeShader.FindKernel("RasterizeSplines");

        computeShader.SetFloat("textureDivTerrain", 1f * textureSize / terrainSize);
        computeShader.SetVector("center", new Vector2(terrainSize * 0.5f, terrainSize * 0.5f));
        computeShader.SetInt("maxHeight", maxHeight);
        computeShader.SetFloat("lineIncrement", (1f / resolution));

        computeShader.SetTexture(gradientsKernelHandle, "result", result);
        computeShader.SetTexture(gradientsKernelHandle, "noise", noise);
        computeShader.SetTexture(gradientsKernelHandle, "warp", warp);
        computeShader.SetTexture(gradientsKernelHandle, "restriction", restriction);
        computeShader.SetTexture(gradientsKernelHandle, "normal", normal);
        computeShader.SetTexture(gradientsKernelHandle, "erosion", erosion);

        computeShader.SetInt("width", textureSize);
        computeShader.SetInt("height", textureSize);
        
        
        foreach (BezierSpline spline in splines)
        {
            if (Mathf.Pow(2, spline.minGridLevel) > textureSize || Mathf.Pow(2, spline.maxGridLevel) < textureSize)
            {
                continue;
            }
            
            computeShader.SetBool("strictElevationContraint", spline.elevationConstraint);
            computeShader.SetBool("noiseConstraint", spline.noiseConstraint);
            computeShader.SetBool("warpConstraint", spline.warpConstraint);
            computeShader.SetBool("erosionConstraint", spline.erosionConstraint);
            computeShader.SetInt("splineCount", spline.CurveCount);
            computeShader.SetInt("metaPointCount", spline.metaPoints.Length);
            
            RasterizeGradientMesh(spline.rasterizingData.meshLeft, normal, restriction);
            RasterizeGradientMesh(spline.rasterizingData.meshRight, normal, restriction);
            if (spline.elevationConstraint)
            {
                RasterizeLineMesh(spline.rasterizingData.meshLine, result, restriction);
            }

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

            ComputeBuffer splinesBuffer = new ComputeBuffer(gpuSplines.Count, sizeof(float) * 3 * 4);
            splinesBuffer.SetData(gpuSplines);

            ComputeBuffer metaPointsBuffer = null;
            if (spline.metaPoints.Length > 0)
            {
                metaPointsBuffer = new ComputeBuffer(spline.metaPoints.Length, sizeof(float) * 12, ComputeBufferType.Default, ComputeBufferMode.Immutable);
                metaPointsBuffer.SetData(spline.getSortedMetaPoints().Select(metaPoint => new MetaPoint(metaPoint))
                    .ToArray());
            }
            else
            {
                // Meta points has to be set so we write a buffer as small as possible
                metaPointsBuffer = new ComputeBuffer(1, sizeof(float), ComputeBufferType.Default, ComputeBufferMode.SubUpdates);
                metaPointsBuffer.BeginWrite<float>(0, 1);
                metaPointsBuffer.EndWrite<float>(1);
            }
            computeShader.SetBuffer(gradientsKernelHandle, "metaPoints", metaPointsBuffer);

            computeShader.SetVector("position", spline.transform.position);
            computeShader.SetBuffer(gradientsKernelHandle, "splines", splinesBuffer);
            computeShader.SetMatrix("localToWorld", spline.transform.localToWorldMatrix);

            computeShader.Dispatch(gradientsKernelHandle, splinesBuffer.count, 1, 1);
            
            splinesBuffer.Release();
            metaPointsBuffer.Release();
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
        data.warp = warp;
        data.restrictions = restriction;
        data.seedHeightmap = result;
        data.seedNormals = normal;
        data.erosion = erosion;
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

    public Dictionary<int, RasterizedData> rasterizeData(BezierSpline[] splines, int terrainSizeExp, int textureSizeExp, int breakOnLevel, int maxHeight, int resolution)
    {
        //float time = Time.realtimeSinceStartup;

        foreach (BezierSpline spline in splines)
        {
            spline.rasterizingData = TesselateSplineLineAndGradients.getSplineData(spline, 0, maxHeight, resolution);
        }

        Dictionary<int, RasterizedData> dict = new Dictionary<int, RasterizedData>();

        int terrainSize = Mathf.RoundToInt(Mathf.Pow(2, terrainSizeExp));

        for(int n = textureSizeExp; n > breakOnLevel; n--)
        {
            int textureSize = Mathf.RoundToInt(Mathf.Pow(2, n)) + 1;
            RasterizedData rd = drawLinesAndTriangles(splines, terrainSize, textureSize, maxHeight, resolution);
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

}