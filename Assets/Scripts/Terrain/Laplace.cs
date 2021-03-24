using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.TerrainAPI;

public class Laplace : MonoBehaviour
{
    public ComputeShader laplace;
    public ComputeShader noiseShader;
    public Texture RedGreenBlackTexture;

    public bool saveImages = false;
    
    Dictionary<int, RasterizedData> rasterizedDataDict = new Dictionary<int, RasterizedData>();

    public void clearRasterizedDataDict()
    {
        rasterizedDataDict.Clear();
    }

    public void setRasterizedDataDict(Dictionary<int, RasterizedData> dict)
    {
        this.rasterizedDataDict = dict;
    }

    public void rasterizeTriangles(BezierSpline[] splines, int maxHeight, int terrainSizeExp, int resolution = 50, int breakOn = 1)
    {
        Rasterize.rasterizeSplines(splines, rasterizedDataDict, terrainSizeExp, maxHeight, resolution, breakOn);
    }


    private RenderTexture createRenderTexture(int size, int layer, int depth, RenderTextureFormat tf = RenderTextureFormat.ARGBFloat)
    {
        RenderTexture renderTexture;
        renderTexture = new RenderTexture(size, size, depth, tf, RenderTextureReadWrite.Linear);
        renderTexture.enableRandomWrite = true;
        renderTexture.autoGenerateMips = false;
        renderTexture.filterMode = FilterMode.Point;
        renderTexture.wrapMode = TextureWrapMode.Clamp;
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
    public void poissonStep(RenderTexture normals, RenderTexture heightmap, RenderTexture noise, RenderTexture erosion, int h, int terrainSizeExp, int iterationsMultiplier = 4, int breakOn = 1, float startHeight = 0, bool erode = false)
    {
        // Break on 1
        if (normals.width == Mathf.RoundToInt(Mathf.Pow(2, breakOn)) + 1)
        {
            if (startHeight > 0)
            {
                Texture2D tex = new Texture2D(heightmap.width, heightmap.height, TextureFormat.RGBAFloat, false);
                Color[] cs = new Color[heightmap.width * heightmap.height];
                Color c = new Color(startHeight, 0, 0, 1);
                for(int n = 0; n < cs.Length; n++)
                {
                    cs[n] = c;
                }
                tex.SetPixels(cs);
                tex.Apply();
                Graphics.Blit(tex, heightmap);
            }

            {
                // Erosion default values
                
                Texture2D tex = new Texture2D(erosion.width, erosion.height, TextureFormat.RGBAFloat, false);
                Color[] cs = new Color[erosion.width * erosion.height];
                Color c = new Color(0, 1, 1, 0);
                for(int n = 0; n < cs.Length; n++)
                {
                    cs[n] = c;
                }
                tex.SetPixels(cs);
                tex.Apply();
                Graphics.Blit(tex, erosion);
            }

            if (erode)
            {
                initErosion(heightmap, erosion);
            }

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
        RenderTexture smallerHeightmap = createRenderTexture(size, 0, heightmap.depth, heightmap.format);
        RenderTexture smallerNoise = createRenderTexture(size, 0, noise.depth, noise.format);
        RenderTexture smallerErosion = createRenderTexture(size, 0, erosion.depth, erosion.format);

        // Restrict textures to smaller versions
        //Restrict(normals, smallerNormals);
        //Restrict(heightmap, smallerHeightmap);
        //Restrict(noise, smallerNoise);

        // Solve recursively
        poissonStep(smallerNormals, smallerHeightmap, smallerNoise, smallerErosion, h + 1, terrainSizeExp, iterationsMultiplier, breakOn, startHeight, erode);

        // Set variables
        laplace.SetFloat("h", Mathf.Pow(2, h));

        // Get rasterized data
        RasterizedData rd;
        if(!rasterizedDataDict.TryGetValue(heightmap.width, out rd))
        {
            throw new System.Exception("Could not get rasterized data. Run rasterizeData with size " + heightmap.width + " first");
        }

        // Create rendertextures
        /*RenderTexture seedHeightmap = new RenderTexture(heightmap.width, heightmap.width, 0, heightmap.format);
        seedHeightmap.enableRandomWrite = true;
        seedHeightmap.autoGenerateMips = false;
        seedHeightmap.Create();
        RenderTexture seedNoise;
        seedNoise = new RenderTexture(noise.width, noise.width, 0, noise.format);
        seedNoise.enableRandomWrite = true;
        seedNoise.autoGenerateMips = false;
        seedNoise.Create();

        RenderTexture seedNormals = createRenderTexture(normals.width, 0, 0);
        RenderTexture restrictions = createRenderTexture(heightmap.width, 0, 0);

        // Copy to rendertextures
        Graphics.Blit(rd.tseedHeightmap, seedHeightmap);
        Graphics.Blit(rd.tseedNormals, seedNormals);
        Graphics.Blit(rd.tRestrictions, restrictions);
        Graphics.Blit(rd.tNoise, seedNoise);
        */

        // Solve the poisson equation for normals
        saveImage("normals " + h + " seed", rd.seedNormals);
        Interpolate(smallerNormals, normals);
        saveImage("normals " + h + " pre", normals);
        Relaxation(rd.seedNormals, normals, iterationsMultiplier * (1 + terrainSizeExp - h - breakOn));
        saveImage("normals " + h + " post", normals);

        // Solve the poisson equation for noise
        saveImage("noise " + h + " seed", rd.noise);
        Interpolate(smallerNoise, noise);
        saveImage("noise " + h + " pre", noise);
        Relaxation(rd.noise, noise, iterationsMultiplier / 2 * (1 + terrainSizeExp - h - breakOn));
        saveImage("noise " + h + " post", noise);
        
        if (erode)
        {
            // Solve the poisson equation for erosion
            saveImage("erosion " + h + " seed", rd.erosion);
            Interpolate(smallerErosion, erosion);
            saveImage("erosion " + h + " pre", erosion);
            Relaxation(rd.erosion, erosion, iterationsMultiplier / 2 * (1 + terrainSizeExp - h - breakOn));
            saveImage("erosion " + h + " post", erosion);
        }

        // Solve poisson equation for the terrain
        Interpolate(smallerHeightmap, heightmap);

        saveImage("seedHeightmap " + h, rd.seedHeightmap);
        saveImage("restrictions " + h, rd.restrictions);
        saveImage("heightmap " + h + " pre", heightmap);

        TerrainRelaxation(rd.seedHeightmap, rd.restrictions, normals, heightmap, iterationsMultiplier * (1 + terrainSizeExp - h - breakOn));
        saveImage("heightmap " + h + " post", heightmap);

        //RestrictedSmoothing(heightmap, rd.seedHeightmap, 0);
        //saveImage("heightmap " + h + " smooth", heightmap);
        if (erode)
        {
            runErosion(heightmap, erosion, noise);
        }

        smallerHeightmap.Release();
        smallerNormals.Release();
        smallerNoise.Release();
        smallerErosion.Release();
    }

    private void initErosion(RenderTexture heightmap, RenderTexture erosion)
    {
        HydraulicErosion hyEro = GetComponent<HydraulicErosion>();
        hyEro.initializeTextures(heightmap, erosion);
    }
    private void runErosion(RenderTexture heightmap, RenderTexture erosion, RenderTexture noiseSeed)
    {
        HydraulicErosion hyEro = GetComponent<HydraulicErosion>();
        
        hyEro._inputHeight = AddNoise(heightmap, noiseSeed);;
        hyEro._erosionParams = erosion;

        hyEro.interpolate();

        int iterations = 0;

        switch (heightmap.width)
        {
            case 257: iterations = hyEro.settings.iterationsOn257;
                break;
            case 513: iterations = hyEro.settings.iterationsOn513;
                break;
            case 1025: iterations = hyEro.settings.iterationsOn1025;
                break;
            case 2049: iterations = hyEro.settings.iterationsOn2049;
                break;
        }
        for (int n = 0; n < iterations; n++)
        {
            hyEro.runStep();
        }
        // hyEro.evaporate();

        if (saveImages)
        {
            hyEro.exportImages("" + heightmap.width);
        }
    }

    public RenderTexture AddNoise(RenderTexture input, RenderTexture noiseSeed)
    {
        RenderTexture result = createRenderTexture(input.width, 0, input.depth, input.format);
        RenderTexture noise = createRenderTexture(input.width, 0, input.depth, input.format);
        int genKernelHandle = noiseShader.FindKernel("GenerateNoise");
        noiseShader.SetTexture(genKernelHandle, "seedNoise", noiseSeed);
        noiseShader.SetTexture(genKernelHandle, "result", noise);

        SplineTerrain s = GetComponent<SplineTerrain>();
        
        noiseShader.SetFloat("scale", s.noiseScale);
        noiseShader.SetFloat("textureDivTerrain", 1f * input.width / 2048f);

        noiseShader.Dispatch(genKernelHandle, noiseSeed.width, noiseSeed.height, 1);

        SumTwoTextures(result, input, noise, 1, 0, s.noiseAmplitude * 0.005f * (100f / s.height), 0f);
        noise.Release();
        
        saveImage("noise " + input.width, result);
        return result;
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
    public void Interpolate(RenderTexture inputImage, RenderTexture outputImage)
    {
        // Interpolate image to normal size
        int interpolateKernelHandle = laplace.FindKernel("Interpolate");
        laplace.SetTexture(interpolateKernelHandle, "image", inputImage);
        laplace.SetTexture(interpolateKernelHandle, "redGreenBlack", RedGreenBlackTexture);
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

    private void TerrainRelaxation(RenderTexture seedTexture, RenderTexture restrictionsTexture, RenderTexture normals, RenderTexture terrainHeight, int iterations)
    {
        /**
         * 
         * Seed texture contains seed values for relaxation
         * restrictionsTexture contains a and b values for terrain relaxation
         * outputImage is the texture to be written to
         * 
         */

        // normalize normals

        RenderTexture normalizedNormals = new RenderTexture(normals.width, normals.width, 0, normals.format);
        normalizedNormals.enableRandomWrite = true;
        normalizedNormals.autoGenerateMips = false;
        normalizedNormals.Create();
        
        int normalizeKernelHandle = laplace.FindKernel("NormalizeNormals");
        laplace.SetTexture(normalizeKernelHandle, "normals", normals);
        laplace.SetTexture(normalizeKernelHandle, "result", normalizedNormals);
        laplace.Dispatch(normalizeKernelHandle, normalizedNormals.width, normalizedNormals.height, 1);

        // Relax solution
        int relaxKernelHandle = laplace.FindKernel("TerrainRelaxation");
        laplace.SetTexture(relaxKernelHandle, "seedTexture", seedTexture);
        laplace.SetTexture(relaxKernelHandle, "restrictionsTexture", restrictionsTexture);
        laplace.SetTexture(relaxKernelHandle, "normals", normalizedNormals);
        laplace.SetTexture(relaxKernelHandle, "terrainHeight", terrainHeight);
        laplace.SetInt("image_size", terrainHeight.width);

        for (int n = 0; n < iterations; n++)
        {
            laplace.Dispatch(relaxKernelHandle, terrainHeight.width, terrainHeight.height, 1);
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
    public void SumTwoTextures(RenderTexture image, RenderTexture input1, RenderTexture input2, float input1_weight, float input1_bias, float input2_weight, float input2_bias)
    {
        int SumTwoTextures = laplace.FindKernel("SumTwoTextures");
        laplace.SetTexture(SumTwoTextures, "result", image);
        laplace.SetTexture(SumTwoTextures, "input1", input1);
        laplace.SetTexture(SumTwoTextures, "input2", input2);
        laplace.SetFloat("input1_weight", input1_weight);
        laplace.SetFloat("input2_weight", input2_weight);
        laplace.SetFloat("input1_bias", input1_bias);
        laplace.SetFloat("input2_bias", input2_bias);
        laplace.Dispatch(SumTwoTextures, image.width, image.height, 1);
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
            RenderTexture.active = null;
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
            RenderTexture.active = null;
        }

    }

}
