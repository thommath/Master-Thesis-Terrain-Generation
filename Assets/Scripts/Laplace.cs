using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Laplace : MonoBehaviour
{
    public ComputeShader laplace;
    public bool saveImages = false;
    public int resolution = 25;
    public Camera rasterizeCamera;
    public Shader lineShader;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private RenderTexture createRenderTexture(int size, int layer, int depth, RenderTextureFormat tf = RenderTextureFormat.ARGBFloat)
    {
        RenderTexture renderTexture;
        renderTexture = new RenderTexture(size, size, depth, tf);
        renderTexture.enableRandomWrite = true;
        renderTexture.autoGenerateMips = false;
        renderTexture.Create();
        return renderTexture;
    }

    /**
     * 
     * Poisson for terrain restricting all textures and relaxing normals and result
     * 
     * normals and result are approximations of the result. Can be whatever - are used to improve solution over multiple iterations
     * 
     */
    public void poissonStep(BezierSpline[] splines, RenderTexture normals, RenderTexture heightmap, int h, int maxHeight)
    {
        // Break on 1
        if (normals.width == 1 || normals.width == 9)
        {
            return;
        }

        // Create buffer of half input size (special case for super tiny)
        int size = normals.width / 2 + 1;
        if (size == 2)
        {
            size = 1;
        }

        // Create smaller versions of all the textures
        RenderTexture smallerNormals = createRenderTexture(size, 0, normals.depth, normals.format);
        RenderTexture smallerHeightmap = createRenderTexture(smallerNormals.width, 1, heightmap.depth, heightmap.format);

        // Restrict textures to smaller versions
        Restrict(normals, smallerNormals);
        Restrict(heightmap, smallerHeightmap);

        // Solve recursively
        poissonStep(splines, smallerNormals, smallerHeightmap, h + 1, maxHeight);

        // Set variables
        laplace.SetFloat("h", Mathf.Pow(2, h));

        // Create textures to rasterize on
        Texture2D tseedHeightmap = new Texture2D(heightmap.width, heightmap.height, TextureFormat.RGBAFloat, false);
        for (int x = 0; x < heightmap.width + 1; x++)
        {
            for (int y = 0; y < heightmap.height + 1; y++)
            {
                tseedHeightmap.SetPixel(x, y, new Color(0, 0, 0, 1));
            }
        }
        Texture2D tRestrictions = new Texture2D(heightmap.width, heightmap.height, TextureFormat.RGBAFloat, false);
        for (int x = 0; x < heightmap.width + 1; x++)
        {
            for (int y = 0; y < heightmap.height + 1; y++)
            {
                tRestrictions.SetPixel(x, y, new Color(1, 0, 0, 1));
            }
        }
        Texture2D tseedNormals = new Texture2D(normals.width, normals.height, TextureFormat.RGBAFloat, false);
        
        // Rasterize splines
        Rasterize.rasterizeSplineTriangles(splines, tseedHeightmap, tRestrictions, tseedNormals, 0.5f, maxHeight, rasterizeCamera, lineShader);

        for (int x = 0; x < heightmap.width + 1; x++)
        {
            for (int y = 0; y < heightmap.height + 1; y++)
            {
                Color c = tseedNormals.GetPixel(x, y);
                tseedNormals.SetPixel(x, y, new Color(0, 0, c.b, 0));
            }
        }
        Rasterize.rasterizeSplineLines(splines, tseedHeightmap, tRestrictions, tseedNormals, 0.5f, maxHeight);

        tseedHeightmap.Apply();
        tseedNormals.Apply();
        tRestrictions.Apply();

        // Create rendertextures
        RenderTexture seedHeightmap;
        seedHeightmap = new RenderTexture(heightmap.width, heightmap.width, 1, heightmap.format);
        seedHeightmap.enableRandomWrite = true;
        seedHeightmap.autoGenerateMips = false;
        seedHeightmap.Create();

        RenderTexture seedNormals = createRenderTexture(normals.width, 0, 1);
        // RenderTexture seedHeightmap = createRenderTexture(heightmap.width, 0, 32);
        RenderTexture restrictions = createRenderTexture(heightmap.width, 0, 1);

        // Copy to rendertextures
        Graphics.Blit(tseedHeightmap, seedHeightmap);
        Graphics.Blit(tseedNormals, seedNormals);
        Graphics.Blit(tRestrictions, restrictions);


        // Solve the poisson equation for normals
        saveImage("normals " + h + " seed", seedNormals);
        Interpolate(smallerNormals, normals);
        saveImage("normals " + h + " pre", normals);
        Relaxation(seedNormals, normals, 12*(8-h));
        saveImage("normals " + h + " post", normals);

        // Solve poisson equation for the terrain
        Interpolate(smallerHeightmap, heightmap);

        saveImage("seedHeightmap " + h, seedHeightmap);
        saveImage("restrictions " + h, restrictions);
        saveImage("heightmap " + h + " pre", heightmap);

        TerrainRelaxation(seedHeightmap, restrictions, normals, heightmap, 2*12*(10-h));
        saveImage("heightmap " + h + " post", heightmap);

        RestrictedSmoothing(heightmap, seedHeightmap, 0);
        saveImage("heightmap " + h + " smooth", heightmap);
    }


    private void Restrict(RenderTexture inputImage, RenderTexture outputImage)
    {
        // Restrict image to half size
        int restrictKernelHandle = laplace.FindKernel("Restrict");
        laplace.SetTexture(restrictKernelHandle, "image", inputImage);
        laplace.SetTexture(restrictKernelHandle, "result", outputImage);
        laplace.SetInt("image_size", outputImage.width);

        laplace.Dispatch(restrictKernelHandle, outputImage.width, outputImage.height, 1);
    }
    private void Interpolate(RenderTexture inputImage, RenderTexture outputImage)
    {
        // Interpolate image to normal size
        int interpolateKernelHandle = laplace.FindKernel("Interpolate");
        laplace.SetTexture(interpolateKernelHandle, "image", inputImage);
        laplace.SetTexture(interpolateKernelHandle, "result", outputImage);
        laplace.SetInt("image_size", outputImage.width);

        laplace.Dispatch(interpolateKernelHandle, inputImage.width, inputImage.height, 1);
    }
    private void Relaxation(RenderTexture seedTexture, RenderTexture outputImage, int iterations)
    {
        /**
         * 
         * Seed texture contains seed values for relaxation
         * outputImage is the texture to be written to
         * 
         */
        // Relax solution
        int relaxKernelHandle = laplace.FindKernel("Relaxation");
        laplace.SetTexture(relaxKernelHandle, "seedTexture", seedTexture);
        laplace.SetTexture(relaxKernelHandle, "result", outputImage);
        laplace.SetInt("image_size", outputImage.width);

        for (int n = 0; n < iterations; n++)
        {
            laplace.Dispatch(relaxKernelHandle, outputImage.width, outputImage.height, 1);
        }
    }

    private void TerrainRelaxation(RenderTexture seedTexture, RenderTexture restrictionsTexture, RenderTexture normals, RenderTexture outputImage, int iterations)
    {
        /**
         * 
         * Seed texture contains seed values for relaxation
         * restrictionsTexture contains a and b values for terrain relaxation
         * outputImage is the texture to be written to
         * 
         */
        // Relax solution
        int relaxKernelHandle = laplace.FindKernel("TerrainRelaxation");
        laplace.SetTexture(relaxKernelHandle, "seedTexture", seedTexture);
        laplace.SetTexture(relaxKernelHandle, "restrictionsTexture", restrictionsTexture);
        laplace.SetTexture(relaxKernelHandle, "normals", normals);
        laplace.SetTexture(relaxKernelHandle, "terrainHeight", outputImage);
        laplace.SetInt("image_size", outputImage.width);

        for (int n = 0; n < iterations; n++)
        {
            laplace.Dispatch(relaxKernelHandle, outputImage.width, outputImage.height, 1);
        }
    }
    public void ImageSmoothing(RenderTexture image, int iterations)
    {
        int smoothKernelHandle = laplace.FindKernel("Smooth");
        laplace.SetTexture(smoothKernelHandle, "result", image);
        for (int n = 0; n < iterations; n++)
        {
            laplace.Dispatch(smoothKernelHandle, image.width, image.height, 1);
        }
    }
    public void RestrictedSmoothing(RenderTexture image, RenderTexture seedTexture, int iterations)
    {
        int RestrictedSmoothingKernelHandle = laplace.FindKernel("RestrictedSmoothing");
        laplace.SetTexture(RestrictedSmoothingKernelHandle, "result", image);
        laplace.SetTexture(RestrictedSmoothingKernelHandle, "seedTexture", seedTexture);
        for (int n = 0; n < iterations; n++)
        {
            laplace.Dispatch(RestrictedSmoothingKernelHandle, image.width, image.height, 1);
        }
    }

    private void saveImage(string name, RenderTexture tex, bool useExr = false)
    {
        if (!saveImages) return;

        if (useExr)
        {
            RenderTexture.active = tex;
            Texture2D tex2D = new Texture2D(tex.width, tex.height, TextureFormat.RGBAFloat, false);
            tex2D.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0, false);
            tex2D.Apply();
            RenderTexture.active = null;
            System.IO.File.WriteAllBytes(Application.dataPath + "/Images/" + name + ".exr", tex2D.EncodeToEXR(Texture2D.EXRFlags.None));
            Debug.Log("Wrote image to " + Application.dataPath + "/Images/" + name + ".exr");
        }
        else
        {
            // Now you can read it back to a Texture2D and save it
            RenderTexture.active = tex;
            Texture2D tex2D = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, true);
            tex2D.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0, false);
            tex2D.Apply();
            RenderTexture.active = null;
            System.IO.File.WriteAllBytes(Application.dataPath + "/Images/" + name + ".png", tex2D.EncodeToPNG());
            Debug.Log("Wrote image to " + Application.dataPath + "/Images/" + name + ".png");
        }

    }

}
