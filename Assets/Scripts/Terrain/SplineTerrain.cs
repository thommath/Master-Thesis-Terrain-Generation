using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

public class SplineTerrain : MonoBehaviour
{
    [Header("Splines creating the terrain")]
    public BezierSpline[] splines;

    [Header("Size of the terrain")]
    public int height = 100;

    [Range(8, 12)]
    public int terrainSizeExp = 8;
    [Range(5, 13)]
    public int terrainResolutionExp = 8;

    [HideInInspector]
    public int terrainSize = 512;
    [HideInInspector]
    public int terrainResolution = 512;

    [Header("Rasterizing")]
    [Range(20, 500)]
    public int splineSamplings = 200;

    [Header("Diffusion")]
    [Range(0.0f, 20)]
    public float diffusionIterationMultiplier = 10;

    [Range(1, 10)]
    public int breakOnLevel = 2;

    [Range(1, 7)]
    public int kernelType = 1;

    [Range(0f, 1f)]
    public float startHeight = 0;

    [Range(0, 30)] public int postSmoothing = 0;

    [Header("Erosion")]
    public bool erode = false;

    [Header("Noise Global")]
    [Range(1, 50f)]
    public float noiseScale = 30f;
    [Range(1f, 100f)]
    public float noiseAmplitude = 10f;


    [Header("What to update")]
    // public bool updateTerrain = false;
    public bool saveImages = false;

    [HideInInspector]
    public RenderTexture heightmap;
    [HideInInspector]
    public RenderTexture normals;
    [HideInInspector]
    public RenderTexture erosion;

    [HideInInspector]
    public UnityEvent updatedData;

    public void OnValidate()
    {
        terrainSize = Mathf.RoundToInt(Mathf.Pow(2, terrainSizeExp));
        terrainResolution = Mathf.RoundToInt(Mathf.Pow(2, terrainResolutionExp));
    }

    public void runSolver()
    {
        Transform terrainFeatures = this.gameObject.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.CompareTag("TerrainFeatures"));
        BezierSpline[] splines = terrainFeatures.GetComponentsInChildren<BezierSpline>().ToArray();

        if (!terrainFeatures)
        {
            GameObject terrainFeaturesGO = new GameObject("TerrainFeatures");
            terrainFeaturesGO.tag = "TerrainFeatures";
            terrainFeaturesGO.transform.parent = this.transform;

            Debug.LogError("Could not find a child with the tag 'TerrainFeatures'. This gameObject is now created, please move your splines here and they will be included in the terrain.");
            return;
        }

        Laplace l = this.GetComponent<Laplace>();
        RenderTexture normals = new RenderTexture(terrainResolution + 1, terrainResolution + 1, 0, RenderTextureFormat.ARGBFloat);
        normals.enableRandomWrite = true;
        normals.autoGenerateMips = false;
        normals.Create();
        RenderTexture heightmap = new RenderTexture(terrainResolution + 1, terrainResolution + 1, 0, RenderTextureFormat.ARGBFloat);
        heightmap.enableRandomWrite = true;
        heightmap.autoGenerateMips = false;
        heightmap.Create();
        RenderTexture erosion = new RenderTexture(terrainResolution + 1, terrainResolution + 1, 0, RenderTextureFormat.ARGBFloat);
        erosion.enableRandomWrite = true;
        erosion.autoGenerateMips = false;
        erosion.Create();
        RenderTexture noiseSeed = new RenderTexture(terrainResolution + 1, terrainResolution + 1, 0, RenderTextureFormat.ARGBFloat);
        noiseSeed.enableRandomWrite = true;
        noiseSeed.autoGenerateMips = false;
        noiseSeed.Create();
        RenderTexture warp = new RenderTexture(terrainResolution + 1, terrainResolution + 1, 0, RenderTextureFormat.ARGBFloat);
        warp.enableRandomWrite = true;
        warp.autoGenerateMips = false;
        warp.Create();

        l.clearRasterizedDataDict();

        float time = Time.realtimeSinceStartup;
        
        // l.rasterizeTriangles(terrainFeatures.GetComponentsInChildren<BezierSpline>().ToArray(), this.height * 2, terrainSizeExp, splineSamplings, breakOnLevel);

        testRasterizing tr = this.GetComponent<testRasterizing>();
        Dictionary<int, RasterizedData> dict = tr.rasterizeData(splines, terrainSizeExp, terrainResolutionExp, breakOnLevel, height * 2, splineSamplings);
        l.setRasterizedDataDict(dict);
        
        Debug.Log((Time.realtimeSinceStartup - time) + "s for rasterizing 1 ");
        //l.rasterizeData(terrainFeatures.GetComponentsInChildren<BezierSpline>().ToArray(), terrainResolution + 1, this.height * 2, terrainSizeExp, splineSamplings, breakOnLevel);
        //Debug.Log((Time.realtimeSinceStartup - time) + "s for rasterizing 2 ");

        l.poissonStep(normals, heightmap, noiseSeed, warp, erosion, 1, terrainSizeExp, diffusionIterationMultiplier, breakOnLevel, startHeight / 2, erode);

        RenderTexture.active = normals;
        Texture2D tex2D3 = new Texture2D(1, 1, TextureFormat.RGBAFloat, false);
        tex2D3.ReadPixels(new Rect(0, 0, 1, 1), 0, 0, false);
        Debug.Log((Time.realtimeSinceStartup - time) + "s for diffusion");
        RenderTexture.active = null;

        l.ImageSmoothing(heightmap, postSmoothing);
        
        if (erode)
        {
            GetComponent<HydraulicErosion>().evaporate();
        }
        else
        {
            /*
            // To only render noise
            Texture2D h2o = new Texture2D(terrainResolution + 1, terrainResolution + 1, TextureFormat.RGBAFloat, false);
            for (int n = 0; n < terrainResolution + 1; n++)
            {
                for (int m = 0; m < terrainResolution + 1; m++)
                {
                    h2o.SetPixel(n, m, new Color(0.15f, 0, 0, 1));
                }
            }
            h2o.Apply();
            Graphics.Blit(h2o, heightmap);
            */
            /*
            RenderTexture h2 = new RenderTexture(terrainResolution + 1, terrainResolution + 1, 0, RenderTextureFormat.ARGBFloat);
            h2.enableRandomWrite = true;
            h2.autoGenerateMips = false;
            h2.Create();
            Graphics.Blit(heightmap, h2);
            RenderTexture result = h2;
            */
            /*
            RenderTexture h2 = new RenderTexture(terrainResolution + 1, terrainResolution + 1, 0, RenderTextureFormat.ARGBFloat);
            h2.enableRandomWrite = true;
            h2.autoGenerateMips = false;
            h2.Create();
            Graphics.Blit(heightmap, h2);
            RenderTexture result = l.AddNoise(heightmap, noiseSeed, h2);
            */
            RenderTexture result = l.AddNoise(heightmap, noiseSeed, warp);
            GetComponent<HydraulicErosion>()._inputHeight = result;
        }
    
        RenderTexture.active = normals;
        Texture2D tex2D2 = new Texture2D(1, 1, TextureFormat.RGBAFloat, false);
        tex2D2.ReadPixels(new Rect(0, 0, 1, 1), 0, 0, false);
        float done = (Time.realtimeSinceStartup - time);
        Debug.Log(done + "s for all");
        RenderTexture.active = null;

        l.clearRasterizedDataDict();
        
        if (this.heightmap)
        {
            this.heightmap.Release();
        }
        if (this.normals)
        {
            this.normals.Release();
        }
        if (this.erosion)
        {
            this.erosion.Release();
        }

        updatedData.Invoke();
        this.heightmap = heightmap;
        this.normals = normals;
        this.erosion = erosion;

        if (saveImages)
        {
            saveState();
            if (erode)
            {
                GetComponent<HydraulicErosion>().exportImages();
            }
        }
        
        noiseSeed.Release();
        warp.Release();

        if (this.heightmap)
        {
            this.heightmap.Release();
        }
        if (this.normals)
        {
            this.normals.Release();
        }

        Debug.Log((Time.realtimeSinceStartup - time) + "s all done");
        

        int curveCount = splines.Select(spline => spline.CurveCount).Sum();
        float size = 0.001f * sizeof(float) * (splines.Select(spline => spline.points.Length).Sum() + splines.Select(spline =>
            spline.metaPoints.Length * (
                (spline.erosionConstraint ? 2 : 0) + (spline.noiseConstraint ? 2 : 0))).Sum());
        
        Debug.Log("Size: " + terrainResolution+ " , number of features: " + curveCount + ", Size: " + size + "kB, Time: " +
                  done + "s");
    }

    private void saveState()
    {
        saveImage("_normals", normals);
        saveImage("_heightmap", heightmap, TextureFormat.RFloat);
        saveImage("_erosion", erosion);
    }
    private void loadState()
    {
        loadImage("_normals", normals);
        loadImage("_heightmap", heightmap, TextureFormat.RFloat);
    }

    private void loadImage(string name, RenderTexture tex, TextureFormat tf = TextureFormat.RGBAFloat)
    {
        Texture2D tempTex = new Texture2D(tex.width, tex.height, tf, false);
        string filepath = Application.dataPath + "/Images/" + name + ".png";
        if (File.Exists(filepath))
        {
            tempTex.LoadImage(File.ReadAllBytes(filepath));
            Debug.Log("Loaded image from " + Application.dataPath + "/Images/" + name + ".png");
            Graphics.Blit(tempTex, tex);
        }
    }

    private void saveImage(string name, RenderTexture tex, TextureFormat tf = TextureFormat.RGBA32)
    {
        // Now you can read it back to a Texture2D and save it
        RenderTexture.active = tex;
        Texture2D tex2D = new Texture2D(tex.width, tex.height, tf, true);
        tex2D.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0, false);
        tex2D.Apply();
        RenderTexture.active = null;
        System.IO.File.WriteAllBytes(Application.dataPath + "/Images/" + name + ".png", tex2D.EncodeToPNG());
        Debug.Log("Wrote image to " + Application.dataPath + "/Images/" + name + ".png");
    }

    public void saveRAW()
    {
        RenderTexture.active = heightmap;
        Texture2D tex = new Texture2D(heightmap.width, heightmap.height, TextureFormat.RFloat, true);
        tex.ReadPixels(new Rect(0, 0, heightmap.width, heightmap.height), 0, 0, false);
        tex.Apply();
        RenderTexture.active = null;

        System.IO.File.WriteAllBytes(Application.dataPath + "/" + "Terrain" + ".png", tex.EncodeToPNG());
        Debug.Log("Wrote image to " + Application.dataPath + "/" + "Terrain" + ".png");

        byte[] rawBytes = new byte[tex.width * tex.height];

        for (int y = 0; y < tex.height; y++)
        {
            for (int x  = 0; x < tex.width; x++)
            {
                rawBytes[y * tex.width + x] = Convert.ToByte(Math.Min(Mathf.RoundToInt(tex.GetPixel(x, y).r * 255), 255));
            }
        }

        System.IO.File.WriteAllBytes(Application.dataPath + "/" + "Terrain" + ".raw", rawBytes);
        Debug.Log("Wrote image to " + Application.dataPath + "/" + "Terrain" + ".raw");
    }


    public GameObject AddSpline()
    {
        Transform terrainFeatures = this.gameObject.GetComponentsInChildren<Transform>().FirstOrDefault(x => x.CompareTag("TerrainFeatures"));

        if (!terrainFeatures)
        {
            GameObject terrainFeaturesGO = new GameObject("TerrainFeatures");
            terrainFeaturesGO.tag = "TerrainFeatures";
            terrainFeaturesGO.transform.parent = this.transform;

            Debug.Log("Could not find a child with the tag 'TerrainFeatures'. This is now created with a spline.");
            terrainFeatures = terrainFeaturesGO.transform;
        }

        GameObject spline = new GameObject("Unnamed terrain feature");
        spline.transform.parent = terrainFeatures;
        spline.AddComponent<BezierSpline>();
        return spline;
    }

}
