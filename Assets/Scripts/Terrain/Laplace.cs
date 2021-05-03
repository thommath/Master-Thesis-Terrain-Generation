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
        foreach (var rd in rasterizedDataDict.Values)
        {
            rd.release();
        }
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
    public void poissonStep(RenderTexture normals, RenderTexture heightmap, RenderTexture noise, RenderTexture warp, RenderTexture erosion, int h, int terrainSizeExp, float iterationsMultiplier = 4, int breakOn = 1, float startHeight = 0, bool erode = false)
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
        RenderTexture smallerWarp = createRenderTexture(size, 0, warp.depth, warp.format);
        RenderTexture smallerErosion = createRenderTexture(size, 0, erosion.depth, erosion.format);

        // Solve recursively
        poissonStep(smallerNormals, smallerHeightmap, smallerNoise, smallerWarp, smallerErosion, h + 1, terrainSizeExp, iterationsMultiplier, breakOn, startHeight, erode);

        // Set variables
        laplace.SetFloat("h", Mathf.Pow(2, h));

        // Get rasterized data
        RasterizedData rd;
        if(!rasterizedDataDict.TryGetValue(heightmap.width, out rd))
        {
            throw new System.Exception("Could not get rasterized data. Run rasterizeData with size " + heightmap.width + " first");
        }

        int iterations = terrainSizeExp;

        // Solve the poisson equation for normals
        saveImage("normals " + h + " seed", rd.seedNormals);
        Interpolate(smallerNormals, normals);
        saveImage("normals " + h + " pre", normals);
        Relaxation(rd.seedNormals, normals, Mathf.RoundToInt(iterationsMultiplier * iterations));
        saveImage("normals " + h + " post", normals);

        // Solve the poisson equation for noise
        saveImage("noise " + h + " seed", rd.noise);
        Interpolate(smallerNoise, noise);
        saveImage("noise " + h + " pre", noise);
        Relaxation(rd.noise, noise, Mathf.RoundToInt(iterationsMultiplier / 2 * iterations));
        saveImage("noise " + h + " post", noise);
        
        // Solve the poisson equation for warp
        saveImage("warp " + h + " seed", rd.warp);
        Interpolate(smallerWarp, warp);
        saveImage("warp " + h + " pre", warp);
        Relaxation(rd.warp, warp, Mathf.RoundToInt(iterationsMultiplier / 2 * iterations));
        saveImage("warp " + h + " post", warp);
        
        if (erode)
        {
            // Solve the poisson equation for erosion
            saveImage("erosion " + h + " seed", rd.erosion);
            Interpolate(smallerErosion, erosion);
            saveImage("erosion " + h + " pre", erosion);
            Relaxation(rd.erosion, erosion, Mathf.RoundToInt(iterationsMultiplier / 2 * iterations));
            saveImage("erosion " + h + " post", erosion);
        }

        // Solve poisson equation for the terrain
        Interpolate(smallerHeightmap, heightmap);

        saveImage("seedHeightmap " + h, rd.seedHeightmap);
        saveImage("restrictions " + h, rd.restrictions);
        saveImage("heightmap " + h + " pre", heightmap);


        TerrainRelaxation(rd.seedHeightmap, rd.restrictions, normals, heightmap, Mathf.RoundToInt(iterationsMultiplier * iterations));
        saveImage("heightmap " + h + " post", heightmap);

        //RestrictedSmoothing(heightmap, rd.seedHeightmap, 0);
        //saveImage("heightmap " + h + " smooth", heightmap);
        if (erode)
        {
            runErosion(heightmap, erosion, noise, warp);
        }

        smallerHeightmap.Release();
        smallerNormals.Release();
        smallerNoise.Release();
        smallerWarp.Release();
        smallerErosion.Release();
        
        rd.release();
        
    }

    private void initErosion(RenderTexture heightmap, RenderTexture erosion)
    {
        HydraulicErosion hyEro = GetComponent<HydraulicErosion>();
        hyEro.initializeTextures(heightmap, erosion);
    }
    private void runErosion(RenderTexture heightmap, RenderTexture erosion, RenderTexture noiseSeed, RenderTexture warp)
    {
        HydraulicErosion hyEro = GetComponent<HydraulicErosion>();

        if (hyEro._inputHeight)
        {
            hyEro._inputHeight.Release();
        }
        if (hyEro._erosionParams)
        {
            hyEro._erosionParams.Release();
        }
        
        hyEro._inputHeight = AddNoise(heightmap, noiseSeed, warp);;
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

    public RenderTexture AddNoise(RenderTexture input, RenderTexture noiseSeed, RenderTexture warp)
    {
        RenderTexture result = createRenderTexture(input.width, 0, input.depth, input.format);
        RenderTexture noise = createRenderTexture(input.width, 0, input.depth, input.format);
        int genKernelHandle = noiseShader.FindKernel("GenerateNoise");
        noiseShader.SetTexture(genKernelHandle, "seedNoise", noiseSeed);
        noiseShader.SetTexture(genKernelHandle, "warp", warp);
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

    private int[] gaussian5x5 = new[]
    {
        1, 4, 7, 4, 1, 4, 16, 26, 16, 4, 7, 26, 41, 26, 7, 4, 16, 26, 16, 4, 1, 4, 7, 4, 1
    };
    private int[] gaussian5x5NoIdentity = new[]
    {
        1, 4, 7, 4, 1, 4, 16, 26, 16, 4, 7, 26, 0, 26, 7, 4, 16, 26, 16, 4, 1, 4, 7, 4, 1
    };
    private int[] gaussian3x3 = new[]
    {
        1,2,1,2,4,2,1,2,1
    };
    private int[] gaussian3x3NoIdentity = new[]
    {
        1,2,1,2,0,2,1,2,1
    };
    private int[] terrainPaper3x3 = new[]
    {
        0,1,0,1,0,1,0,1,0
    };
    private int[] doubleDerivativeLaplace3x3 = new[]
    {
        0,1,0,1,-4,1,0,1,0
    };
    private int[] doubleDerivativeLaplaceAlternative3x3 = new[]
    {
        1,1,1,1,-8,1,1,1,1
    };
    

    private void TerrainRelaxation(RenderTexture seedTexture, RenderTexture restrictionsTexture, RenderTexture normals, RenderTexture terrainHeight, int iterations)
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
        laplace.SetTexture(relaxKernelHandle, "terrainHeight", terrainHeight);
        laplace.SetInt("image_size", terrainHeight.width);
        
        
        laplace.SetBool("doubleDerivative", false);
        
        int kernelType = GetComponent<SplineTerrain>().kernelType;
        ComputeBuffer kernel;
        switch (kernelType)
        {
            case 1:
            {
                kernel = new ComputeBuffer(9, sizeof(int));
                kernel.SetData(gaussian3x3);
                laplace.SetBuffer(relaxKernelHandle, "kernel", kernel);
                laplace.SetInt("kernelSize", 1);
                break;
            }
            case 2:
            {
                kernel = new ComputeBuffer(9, sizeof(int));
                kernel.SetData(gaussian3x3NoIdentity);
                laplace.SetBuffer(relaxKernelHandle, "kernel", kernel);
                laplace.SetInt("kernelSize", 1);
                break;
            }
            case 3:
            {
                kernel = new ComputeBuffer(25, sizeof(int));
                kernel.SetData(gaussian5x5);
                laplace.SetBuffer(relaxKernelHandle, "kernel", kernel);
                laplace.SetInt("kernelSize", 2);
                break;
            }
            case 4:
            {
                kernel = new ComputeBuffer(25, sizeof(int));
                kernel.SetData(gaussian5x5NoIdentity);
                laplace.SetBuffer(relaxKernelHandle, "kernel", kernel);
                laplace.SetInt("kernelSize", 2);
                break;
            }
            case 5:
            {
                kernel = new ComputeBuffer(9, sizeof(int));
                kernel.SetData(doubleDerivativeLaplace3x3);
                laplace.SetBuffer(relaxKernelHandle, "kernel", kernel);
                laplace.SetInt("kernelSize", 1);
                laplace.SetBool("doubleDerivative", true);
                break;
            }
            case 6:
            {
                kernel = new ComputeBuffer(9, sizeof(int));
                kernel.SetData(doubleDerivativeLaplaceAlternative3x3);
                laplace.SetBuffer(relaxKernelHandle, "kernel", kernel);
                laplace.SetInt("kernelSize", 1);
                laplace.SetBool("doubleDerivative", true);
                break;
            }
            default:
            {
                kernel = new ComputeBuffer(9, sizeof(int));
                kernel.SetData(terrainPaper3x3);
                laplace.SetBuffer(relaxKernelHandle, "kernel", kernel);
                laplace.SetInt("kernelSize", 1);
                break;
            }
        }
        
        
        RenderTexture newTerrain = new RenderTexture(terrainHeight.width, terrainHeight.width, 0, terrainHeight.format);
        newTerrain.enableRandomWrite = true;
        newTerrain.autoGenerateMips = false;
        newTerrain.Create();

        for (int n = 0; n < iterations; n++)
        {
            laplace.SetTexture(relaxKernelHandle, "terrainHeight", terrainHeight);
            laplace.SetTexture(relaxKernelHandle, "result", newTerrain);
            laplace.Dispatch(relaxKernelHandle, terrainHeight.width, terrainHeight.height, 1);
            laplace.SetTexture(relaxKernelHandle, "terrainHeight", newTerrain);
            laplace.SetTexture(relaxKernelHandle, "result", terrainHeight);
            laplace.Dispatch(relaxKernelHandle, terrainHeight.width, terrainHeight.height, 1);
        }
        kernel.Release();
        newTerrain.Release();
    }
    public void ImageSmoothing(RenderTexture image, int iterations, int kernelType = -1, float[] smoothlayers = null)
    {
        int smoothKernelHandle = laplace.FindKernel("SmoothKernel");
        laplace.SetTexture(smoothKernelHandle, "result", image);

        if (smoothlayers == null)
        {
            smoothlayers = new[] {1f, 1f, 1f, 1f};
        }
        
        laplace.SetFloats("smoothLayer", smoothlayers);

        if (kernelType == -1)
        {
            kernelType = GetComponent<SplineTerrain>().kernelType;
        }
        ComputeBuffer kernel;
        switch (kernelType)
        {
            case 1:
            {
                kernel = new ComputeBuffer(9, sizeof(int));
                kernel.SetData(gaussian3x3);
                laplace.SetBuffer(smoothKernelHandle, "kernel", kernel);
                laplace.SetInt("kernelSize", 1);
                break;
            }
            case 2:
            {
                kernel = new ComputeBuffer(9, sizeof(int));
                kernel.SetData(gaussian3x3NoIdentity);
                laplace.SetBuffer(smoothKernelHandle, "kernel", kernel);
                laplace.SetInt("kernelSize", 1);
                break;
            }
            case 3:
            {
                kernel = new ComputeBuffer(25, sizeof(int));
                kernel.SetData(gaussian5x5);
                laplace.SetBuffer(smoothKernelHandle, "kernel", kernel);
                laplace.SetInt("kernelSize", 2);
                break;
            }
            case 4:
            {
                kernel = new ComputeBuffer(25, sizeof(int));
                kernel.SetData(gaussian5x5NoIdentity);
                laplace.SetBuffer(smoothKernelHandle, "kernel", kernel);
                laplace.SetInt("kernelSize", 2);
                break;
            }
            default:
            {
                kernel = new ComputeBuffer(9, sizeof(int));
                kernel.SetData(terrainPaper3x3);
                laplace.SetBuffer(smoothKernelHandle, "kernel", kernel);
                laplace.SetInt("kernelSize", 1);
                break;
            }
        }
        
        for (int n = 0; n < iterations; n++)
        {
            laplace.Dispatch(smoothKernelHandle, image.width, image.height, 1);
        }
        kernel.Release();
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
