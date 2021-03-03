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
    [Range(20, 200)]
    public int splineSamplings = 50;

    [Header("Diffusion")]
    [Range(1, 20)]
    public int diffusionIterationMultiplier = 10;

    [Range(1, 10)]
    public int breakOnLevel = 2;

    [Range(0f, 1f)]
    public float startHeight = 0;


    [Header("Noise Global")]
    [Range(1, 50f)]
    public float noiseScale = 30f;
    [Range(1f, 30f)]
    public float noiseAmplitude = 10f;


    [Header("What to update")]
    // public bool updateTerrain = false;
    public bool saveImages = false;

    [HideInInspector]
    public RenderTexture heightmap;
    [HideInInspector]
    public RenderTexture normals;
    [HideInInspector]
    public RenderTexture noise;

    public ComputeShader noiseShader;


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
        RenderTexture noise = new RenderTexture(terrainResolution + 1, terrainResolution + 1, 0, RenderTextureFormat.ARGBFloat);
        noise.enableRandomWrite = true;
        noise.autoGenerateMips = false;
        noise.Create();
        RenderTexture noiseSeed = new RenderTexture(terrainResolution + 1, terrainResolution + 1, 0, RenderTextureFormat.ARGBFloat);
        noiseSeed.enableRandomWrite = true;
        noiseSeed.autoGenerateMips = false;
        noiseSeed.Create();
        RenderTexture result = new RenderTexture(terrainResolution + 1, terrainResolution + 1, 0, RenderTextureFormat.RFloat);
        result.enableRandomWrite = true;
        result.autoGenerateMips = false;
        result.Create();

        l.clearRasterizedDataDict();

        float time = Time.realtimeSinceStartup;
        
        l.rasterizeTriangles(terrainFeatures.GetComponentsInChildren<BezierSpline>().ToArray(), this.height * 2, terrainSizeExp, splineSamplings, breakOnLevel);

        Debug.Log((Time.realtimeSinceStartup - time) + "s for rasterizing 1 ");
        //l.rasterizeData(terrainFeatures.GetComponentsInChildren<BezierSpline>().ToArray(), terrainResolution + 1, this.height * 2, terrainSizeExp, splineSamplings, breakOnLevel);
        //Debug.Log((Time.realtimeSinceStartup - time) + "s for rasterizing 2 ");

        l.poissonStep(normals, heightmap, noiseSeed, 1, terrainSizeExp, diffusionIterationMultiplier, breakOnLevel, startHeight / 2);

        Debug.Log((Time.realtimeSinceStartup - time) + "s for diffusion");

        /////////////////////////////
        ///
        ///  noise 
        ///
        /////////////////////////////
        // Interpolate image to normal size
        int genKernelHandle = noiseShader.FindKernel("GenerateNoise");
        noiseShader.SetTexture(genKernelHandle, "seedNoise", noiseSeed);
        noiseShader.SetTexture(genKernelHandle, "result", noise);
        noiseShader.SetFloat("scale", noiseScale);

        noiseShader.Dispatch(genKernelHandle, noiseSeed.width, noiseSeed.height, 1);

        l.SumTwoTextures(result, heightmap, noise, 1, 0, noiseAmplitude * 0.005f * (100f / height), 0f);

        Debug.Log((Time.realtimeSinceStartup - time) + "s for noise and sum");


        this.heightmap = result;
        this.normals = normals;
        this.noise = noise;

        updatedData.Invoke();
        saveState();
    }

    private void saveState()
    {
        saveImage("_normals", normals);
        saveImage("_heightmap", heightmap, TextureFormat.RFloat);
        saveImage("_noise", noise);
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

        float c = 0;

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
