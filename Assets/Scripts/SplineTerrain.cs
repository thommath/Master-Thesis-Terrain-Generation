using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SplineTerrain : MonoBehaviour
{
    [Header("Nessecary objects")]
    public Terrain terrain;
    public ComputeShader hydraulicShader;


    [Header("Splines creating the terrain")]
    public BezierSpline[] splines;

    [Header("Size of the terrain")]
    public int height = 100;
    public int size = 512;


    [Header("Rasterizing")]
    public int resolution = 10;
    public int zoom = 2;

    [Space(10)]
    public bool deleteConstructedItems = true;


    [Header("Diffusion")]
    [Range(1, 10)]
    public int diffusionIterations = 10;


    [Header("What to update")]
    // public bool updateTerrain = false;
    public bool saveImages = false;


    [Header("Hydraulic erosion")]
    [Range(0.001f, 0.2f)]
    public float erosionTimeDelta = 0.02f;
    [Range(0, 5000)]
    public int erosionIterationsEachStep = 1;


    RenderTexture heightmap;
    public HydraulicErosion erosion;


    public void Update()
    {
    }

    private void Reset()
    {}


    public void initializeErosion()
    {
        if (this.erosion != null)
        {
            erosion.initializeTextures(heightmap, hydraulicShader);
        }
    }
    public void stepErosion()
    {
        erosion.timeDelta = erosionTimeDelta;
        for (int n = 0; n < erosionIterationsEachStep; n++)
        {
            erosion.runStep();
        }
        erosion.exportImages();

        Graphics.Blit(erosion._stateTexture, heightmap);
        copyToTerrain(heightmap);
        return;

        // Copy other state to terrain
        RenderTexture.active = erosion._stateTexture;
        Texture2D tex = new Texture2D(erosion._stateTexture.width, erosion._stateTexture.height, TextureFormat.RGBAFloat, false);
        tex.ReadPixels(new Rect(0, 0, erosion._stateTexture.width, erosion._stateTexture.height), 0, 0, false);
        tex.Apply();
        RenderTexture.active = null;

        float[,] heights = new float[size, size];
        for (int y = 0; y < tex.height - 1; y++)
        {
            for (int x = 0; x < tex.width - 1; x++)
            {
                Color c = tex.GetPixel(x, y);
                // set height to height + water
                heights[y, x] = c.r + c.g;
            }
        }

        terrain.gameObject.transform.position = new Vector3(-(size / zoom) / 2, 0, -(size / zoom) / 2);
        terrain.terrainData.heightmapResolution = size;
        terrain.terrainData.size = new Vector3(size / zoom, height, size / zoom);
        terrain.terrainData.SetHeights(0, 0, heights);
    }

    public void runSolver()
    {
        Laplace l = this.GetComponent<Laplace>();
        RenderTexture normals = new RenderTexture(size + 1, size + 1, 32, RenderTextureFormat.ARGBFloat);
        normals.enableRandomWrite = true;
        normals.autoGenerateMips = false;
        normals.Create();
        RenderTexture heightmap = new RenderTexture(size + 1, size + 1, 32, RenderTextureFormat.RFloat);
        heightmap.enableRandomWrite = true;
        heightmap.autoGenerateMips = false;
        heightmap.Create();
        for (int n = 0; n < diffusionIterations; n++)
        {
            l.poissonStep(splines, normals, heightmap, 1, this.height);
        }
        saveImage("result normals", normals);
        saveImage("result heightmap", heightmap, TextureFormat.RFloat);

        copyToTerrain(heightmap);

        this.heightmap = heightmap;

        initializeErosion();
    }


    private void saveImage(string name, RenderTexture tex, TextureFormat tf = TextureFormat.RGBA32)
    {
        if (!saveImages) return;

        // Now you can read it back to a Texture2D and save it
        RenderTexture.active = tex;
        Texture2D tex2D = new Texture2D(tex.width, tex.height, tf, true);
        tex2D.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0, false);
        tex2D.Apply();
        RenderTexture.active = null;
        System.IO.File.WriteAllBytes(Application.dataPath + "/Images/" + name + ".png", tex2D.EncodeToPNG());
        Debug.Log("Wrote image to " + Application.dataPath + "/Images/" + name + ".png");
    }

    private void copyToTerrain(RenderTexture rawTex)
    {
        RenderTexture.active = rawTex;
        Texture2D tex = new Texture2D(rawTex.width, rawTex.height, TextureFormat.RFloat, true);
        tex.ReadPixels(new Rect(0, 0, rawTex.width, rawTex.height), 0, 0, false);
        tex.Apply();
        RenderTexture.active = null;

        float[,] heights = new float[size,size];
        for (int y = 0; y < tex.height-1; y++)
        {
            for (int x = 0; x < tex.width-1; x++)
            {
                heights[y, x] = tex.GetPixel(x, y).r;
            }
        }

        terrain.gameObject.transform.position = new Vector3(-(size / zoom) / 2, 0, -(size / zoom )/ 2);
        terrain.terrainData.heightmapResolution = size;
        terrain.terrainData.size = new Vector3(size / zoom, height, size / zoom);
        terrain.terrainData.SetHeights(0, 0, heights);
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

}
