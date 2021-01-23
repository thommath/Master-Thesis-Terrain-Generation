using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


public class SplineTerrain : MonoBehaviour
{
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

    [HideInInspector]
    public RenderTexture heightmap;
    [HideInInspector]
    public RenderTexture normals;

    public void runSolver()
    {
        Laplace l = this.GetComponent<Laplace>();
        RenderTexture normals = new RenderTexture(size + 1, size + 1, 32, RenderTextureFormat.ARGBFloat);
        normals.enableRandomWrite = true;
        normals.autoGenerateMips = false;
        normals.Create();
        RenderTexture heightmap = new RenderTexture(size + 1, size + 1, 1, RenderTextureFormat.ARGBFloat);
        heightmap.enableRandomWrite = true;
        heightmap.autoGenerateMips = false;
        heightmap.Create();
        for (int n = 0; n < diffusionIterations; n++)
        {
            l.poissonStep(splines, normals, heightmap, 1, this.height *2);
        }

        this.heightmap = heightmap;
        this.normals = normals;

        saveState();
    }

    private void saveState()
    {
        saveImage("_normals", normals);
        saveImage("_heightmap", heightmap, TextureFormat.RFloat);
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

}
