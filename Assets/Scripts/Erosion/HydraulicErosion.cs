using System.IO;
using UnityEditor.SceneTemplate;
using UnityEngine;
using UnityEngine.Events;

public class HydraulicErosion : MonoBehaviour
{
    [HideInInspector]
    public RenderTexture _stateTexture;
    [HideInInspector]
    public RenderTexture _waterFluxTexture;
    [HideInInspector]
    public RenderTexture _velocityTexture;
    [HideInInspector]
    public RenderTexture _terrainFluxTexture;
    
    [HideInInspector]
    public RenderTexture _inputHeight;
    [HideInInspector]
    public RenderTexture _erosionParams;

    public ComputeShader hydraulicShader;

    int RainAndControl;
    int FluxComputation;
    int FluxApply;
    int TiltAngle;
    int HydraulicErosionKernel;
    int SedimentAdvection;

    public ErosionSetting settings;

    [HideInInspector]
    public UnityEvent updatedData;

    [Range(0, 20)]
    public int seed = 0;

    float lastRaindrop = 0f;
    public float time = 0f;

    public void initializeTextures()
    {
        Random.InitState(seed);
        RenderTexture heightmap = this.gameObject.GetComponent<SplineTerrain>().heightmap;
        _inputHeight = heightmap;
        RenderTexture erosion = this.gameObject.GetComponent<SplineTerrain>().erosion;
        _erosionParams = erosion;
        
        int Width = heightmap.width;
        int Height = heightmap.height;

        /* ========= Setup computation =========== */
        // If there are already existing textures - release them
        if (_stateTexture != null)
            _stateTexture.Release();

        if (_waterFluxTexture != null)
            _waterFluxTexture.Release();

        if (_velocityTexture != null)
            _velocityTexture.Release();

        // Initialize texture for storing height map
        _stateTexture = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGBFloat)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };


        // Initialize texture for storing flow
        _waterFluxTexture = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGBFloat)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        // Initialize texture for storing flow for thermal erosion
        _terrainFluxTexture = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGBFloat)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        // Velocity texture
        _velocityTexture = new RenderTexture(Width, Height, 0, RenderTextureFormat.RGFloat)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        if (!_stateTexture.IsCreated())
            _stateTexture.Create();

        if (!_waterFluxTexture.IsCreated())
            _waterFluxTexture.Create();

        if (!_terrainFluxTexture.IsCreated())
            _terrainFluxTexture.Create();

        if (!_velocityTexture.IsCreated())
            _velocityTexture.Create();

        // Graphics.Blit(heightmap, _stateTexture);


        Laplace l = this.GetComponent<Laplace>();
        l.ImageSmoothing(_stateTexture, settings.SmoothingIterationsOnStart);

        exportImages();

        RainAndControl = hydraulicShader.FindKernel("RainAndControl");
        FluxComputation = hydraulicShader.FindKernel("FluxComputation");
        FluxApply = hydraulicShader.FindKernel("FluxApply");
        TiltAngle = hydraulicShader.FindKernel("TiltAngle");
        HydraulicErosionKernel = hydraulicShader.FindKernel("HydraulicErosion");
        SedimentAdvection = hydraulicShader.FindKernel("SedimentAdvection");
        
        updateShaderValues();

        updatedData.Invoke();

        time = 0f;
        lastRaindrop = 0f;

        RenderBehaviour rb = this.GetComponent<RenderBehaviour>();
        if (rb != null)
        {
            rb.initiateFilm();
        }
    }
    public void initializeTextures(RenderTexture heightmap, RenderTexture erosion)
    {
        Random.InitState(seed);
        _inputHeight = heightmap;
        _erosionParams = erosion;
        
        int Width = heightmap.width;
        int Height = heightmap.height;

        /* ========= Setup computation =========== */
        // If there are already existing textures - release them
        if (_stateTexture != null)
            _stateTexture.Release();

        if (_waterFluxTexture != null)
            _waterFluxTexture.Release();

        if (_velocityTexture != null)
            _velocityTexture.Release();

        // Initialize texture for storing height map
        _stateTexture = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGBFloat)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };


        // Initialize texture for storing flow
        _waterFluxTexture = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGBFloat)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        // Initialize texture for storing flow for thermal erosion
        _terrainFluxTexture = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGBFloat)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        // Velocity texture
        _velocityTexture = new RenderTexture(Width, Height, 0, RenderTextureFormat.RGFloat)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        if (!_stateTexture.IsCreated())
            _stateTexture.Create();

        if (!_waterFluxTexture.IsCreated())
            _waterFluxTexture.Create();

        if (!_terrainFluxTexture.IsCreated())
            _terrainFluxTexture.Create();

        if (!_velocityTexture.IsCreated())
            _velocityTexture.Create();


        Laplace l = this.GetComponent<Laplace>();
        l.ImageSmoothing(_stateTexture, settings.SmoothingIterationsOnStart);

        RainAndControl = hydraulicShader.FindKernel("RainAndControl");
        FluxComputation = hydraulicShader.FindKernel("FluxComputation");
        FluxApply = hydraulicShader.FindKernel("FluxApply");
        TiltAngle = hydraulicShader.FindKernel("TiltAngle");
        HydraulicErosionKernel = hydraulicShader.FindKernel("HydraulicErosion");
        SedimentAdvection = hydraulicShader.FindKernel("SedimentAdvection");
        
        updateShaderValues();

        time = 0f;
        lastRaindrop = 0f;

        RenderBehaviour rb = this.GetComponent<RenderBehaviour>();
        if (rb != null)
        {
            rb.initiateFilm();
        }
    }

    private void updateShaderValues()
    {
        hydraulicShader.SetTexture(RainAndControl, "HeightMap", _stateTexture);
        hydraulicShader.SetTexture(RainAndControl, "VelocityMap", _velocityTexture);
        hydraulicShader.SetTexture(RainAndControl, "FluxMap", _waterFluxTexture);
        hydraulicShader.SetTexture(RainAndControl, "TerrainFluxMap", _terrainFluxTexture);
        hydraulicShader.SetTexture(RainAndControl, "InputHeight", _inputHeight);
        hydraulicShader.SetTexture(RainAndControl, "ErosionParams", _erosionParams);
        hydraulicShader.SetTexture(FluxComputation, "HeightMap", _stateTexture);
        hydraulicShader.SetTexture(FluxComputation, "VelocityMap", _velocityTexture);
        hydraulicShader.SetTexture(FluxComputation, "FluxMap", _waterFluxTexture);
        hydraulicShader.SetTexture(FluxComputation, "TerrainFluxMap", _terrainFluxTexture);
        hydraulicShader.SetTexture(FluxComputation, "InputHeight", _inputHeight);
        hydraulicShader.SetTexture(FluxComputation, "ErosionParams", _erosionParams);
        hydraulicShader.SetTexture(FluxApply, "HeightMap", _stateTexture);
        hydraulicShader.SetTexture(FluxApply, "VelocityMap", _velocityTexture);
        hydraulicShader.SetTexture(FluxApply, "FluxMap", _waterFluxTexture);
        hydraulicShader.SetTexture(FluxApply, "TerrainFluxMap", _terrainFluxTexture);
        hydraulicShader.SetTexture(FluxApply, "InputHeight", _inputHeight);
        hydraulicShader.SetTexture(FluxApply, "ErosionParams", _erosionParams);
        hydraulicShader.SetTexture(TiltAngle, "HeightMap", _stateTexture);
        hydraulicShader.SetTexture(TiltAngle, "VelocityMap", _velocityTexture);
        hydraulicShader.SetTexture(TiltAngle, "FluxMap", _waterFluxTexture);
        hydraulicShader.SetTexture(TiltAngle, "TerrainFluxMap", _terrainFluxTexture);
        hydraulicShader.SetTexture(TiltAngle, "InputHeight", _inputHeight);
        hydraulicShader.SetTexture(TiltAngle, "ErosionParams", _erosionParams);
        hydraulicShader.SetTexture(HydraulicErosionKernel, "HeightMap", _stateTexture);
        hydraulicShader.SetTexture(HydraulicErosionKernel, "VelocityMap", _velocityTexture);
        hydraulicShader.SetTexture(HydraulicErosionKernel, "FluxMap", _waterFluxTexture);
        hydraulicShader.SetTexture(HydraulicErosionKernel, "TerrainFluxMap", _terrainFluxTexture);
        hydraulicShader.SetTexture(HydraulicErosionKernel, "InputHeight", _inputHeight);
        hydraulicShader.SetTexture(HydraulicErosionKernel, "ErosionParams", _erosionParams);
        hydraulicShader.SetTexture(SedimentAdvection, "HeightMap", _stateTexture);
        hydraulicShader.SetTexture(SedimentAdvection, "VelocityMap", _velocityTexture);
        hydraulicShader.SetTexture(SedimentAdvection, "FluxMap", _waterFluxTexture);
        hydraulicShader.SetTexture(SedimentAdvection, "TerrainFluxMap", _terrainFluxTexture);
        hydraulicShader.SetTexture(SedimentAdvection, "InputHeight", _inputHeight);
        hydraulicShader.SetTexture(SedimentAdvection, "ErosionParams", _erosionParams);

        hydraulicShader.SetInt("_Width", _stateTexture.width);
        hydraulicShader.SetInt("_Height", _stateTexture.height);

        hydraulicShader.SetFloat("_TimeDelta", settings.TimeDelta);

        hydraulicShader.SetFloat("_PipeArea", settings.PipeArea);
        hydraulicShader.SetFloat("_Gravity", settings.Gravity);
        hydraulicShader.SetFloat("_PipeLength", settings.PipeLength);
        hydraulicShader.SetVector("_CellSize", new Vector2(settings.CellSize, settings.CellSize));

        // Hydraulic erosion
        // bad paper! --> from http://graphics.uni-konstanz.de/publikationen/Neidhold2005InteractivePhysicallyBased/Neidhold2005InteractivePhysicallyBased.pdf
        hydraulicShader.SetFloat("_SedimentCapacity", settings.SedimentCapacity);
        hydraulicShader.SetFloat("_SuspensionRate", settings.SuspensionRate);
        hydraulicShader.SetFloat("_DepositionRate", settings.DepositionRate);

        hydraulicShader.SetFloat("_Evaporation", settings.Evaporation);
    }

    public void runErosion()
    {
        RenderBehaviour rb = this.GetComponent<RenderBehaviour>();

        updateShaderValues();
        for (int i = 0; i < settings.IterationsEachStep; i++)
        {
            runStep();

            if (rb != null)
            {
                rb.filmStep(time);
            }

        }
        exportImages();
        updatedData.Invoke();
    }

    public void runStep()
    {
        time += settings.TimeDelta;

        if (settings.AddWater && settings.StopRainAfterTime > time && time - lastRaindrop > settings.RainFrequency)
        {
            hydraulicShader.SetFloat("raindropStrength", Random.Range(settings.MinRainIntensity, settings.MaxRainIntensity) / 25);
            hydraulicShader.SetFloat("raindropRadius", Random.Range(settings.MinRainSize, settings.MaxRainSize * (_stateTexture.width / 512f)));

            // Add random rain
            if (settings.RandomizedRain)
            {
                hydraulicShader.SetVector("raindropLocation", new Vector2(Random.Range(0f, _stateTexture.width), Random.Range(0f, _stateTexture.height)));
            } else
            {
                hydraulicShader.SetVector("raindropLocation", new Vector2(_stateTexture.width / 2f, _stateTexture.width / 2f));
            }
            lastRaindrop = time;
            
            // Only run rain shader if rain should be added
            hydraulicShader.Dispatch(RainAndControl, _stateTexture.width, _stateTexture.height, 1);
        }

        hydraulicShader.Dispatch(FluxComputation, _stateTexture.width, _stateTexture.height, 1);

        hydraulicShader.Dispatch(FluxApply, _stateTexture.width, _stateTexture.height, 1);

        hydraulicShader.Dispatch(TiltAngle, _stateTexture.width, _stateTexture.height, 1);

        hydraulicShader.Dispatch(HydraulicErosionKernel, _stateTexture.width, _stateTexture.height, 1);

        hydraulicShader.Dispatch(SedimentAdvection, _stateTexture.width, _stateTexture.height, 1);
    }

    public void evaporate()
    {
        hydraulicShader.SetFloat("raindropStrength", 0.05f);
        
        
        int addWaterKernelHandle = hydraulicShader.FindKernel("AddWaterEverywhere");
        hydraulicShader.SetTexture(addWaterKernelHandle, "HeightMap", _stateTexture);
        hydraulicShader.SetTexture(addWaterKernelHandle, "ErosionParams", _erosionParams);
        hydraulicShader.SetFloat("_Evaporation", 0.9f);
        for (int n = 0; n < 100; n++)
        {
            hydraulicShader.Dispatch(FluxComputation, _stateTexture.width, _stateTexture.height, 1);

            hydraulicShader.Dispatch(FluxApply, _stateTexture.width, _stateTexture.height, 1);

            hydraulicShader.Dispatch(TiltAngle, _stateTexture.width, _stateTexture.height, 1);

            hydraulicShader.Dispatch(HydraulicErosionKernel, _stateTexture.width, _stateTexture.height, 1);

            hydraulicShader.Dispatch(SedimentAdvection, _stateTexture.width, _stateTexture.height, 1);
        }
        
        
        int smoothingIterations = 4;
        int kernelType = 1;
        Laplace l = GetComponent<Laplace>();
        l.ImageSmoothing(_stateTexture, smoothingIterations, kernelType);

    }

    public void interpolate()
    {
        int smoothingIterations = 4;
        int kernelType = 1;
        Laplace l = GetComponent<Laplace>();
        RenderTexture temp = new RenderTexture((_stateTexture.width - 1) * 2 + 1, (_stateTexture.height - 1) * 2 + 1, 0, RenderTextureFormat.ARGBFloat)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        temp.Create();
        l.Interpolate(_stateTexture, temp);
        l.ImageSmoothing(temp, smoothingIterations, kernelType, new []{1f, 1f, 0.0f, 0});
        _stateTexture.Release();
        _stateTexture = temp;

        temp = new RenderTexture((_velocityTexture.width - 1) * 2 + 1, (_velocityTexture.height - 1) * 2 + 1, 0, RenderTextureFormat.ARGBFloat)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        temp.Create();
        _velocityTexture.Release();
        _velocityTexture = temp;

        temp = new RenderTexture((_waterFluxTexture.width - 1) * 2 + 1, (_waterFluxTexture.height - 1) * 2 + 1, 0, RenderTextureFormat.ARGBFloat)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        temp.Create();
        l.Interpolate(_waterFluxTexture, temp);
        _waterFluxTexture.Release();
        _waterFluxTexture = temp;

        temp = new RenderTexture((_terrainFluxTexture.width - 1) * 2 + 1, (_terrainFluxTexture.height - 1) * 2 + 1, 0, RenderTextureFormat.ARGBFloat)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        temp.Create();
        _terrainFluxTexture.Release();
        _terrainFluxTexture = temp;
        

        updateShaderValues();
    }


    public void exportImages(string suffix = "")
    {
        saveImage("stateTexture" + suffix, _stateTexture, TextureFormat.RGBAFloat);
        saveImage("velocityTexture" + suffix, _velocityTexture, TextureFormat.RGBAFloat);
        saveImage("waterFluxTexture" + suffix, _waterFluxTexture, TextureFormat.RGBAFloat);
        saveImage("terrainFluxTexture" + suffix, _terrainFluxTexture, TextureFormat.RGBAFloat);
    }


    private void saveImage(string name, RenderTexture tex, TextureFormat tf = TextureFormat.RGBAFloat, bool normalize = true)
    {
        // Now you can read it back to a Texture2D and save it
        RenderTexture tempRT = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        Graphics.Blit(tex, tempRT);
        RenderTexture.active = tempRT;
        Texture2D tex2D = new Texture2D(tex.width, tex.height, tf, true);
        tex2D.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0, false);
        tex2D.Apply();
        RenderTexture.active = null;

        Vector4 scaling = new Vector4(0.00001f, 0.00001f, 0.00001f, 0.00001f);
        if (normalize)
        {
            for(int x = 0; x < tex2D.width; x++)
            {
                for (int y = 0; y < tex2D.height; y++)
                {
                    Color c = tex2D.GetPixel(x, y);
                    scaling = new Vector4(
                        Mathf.Max(c.r, scaling.x),
                        Mathf.Max(c.g, scaling.y),
                        Mathf.Max(c.b, scaling.z),
                        Mathf.Max(c.a, scaling.w)
                        );
                }
            }
        } else
        {
            scaling = new Vector4(1, 1, 1, 1);
        }

        for (int x = 0; x < tex2D.width; x++)
        {
            for (int y = 0; y < tex2D.height; y++)
            {
                Color c = tex2D.GetPixel(x, y);
                tex2D.SetPixel(x, y, new Color(
                    c.r / scaling.x,
                    c.g / scaling.y,
                    c.b / scaling.z,
                    c.a / scaling.w
                    ));
            }
        }

        File.WriteAllBytes(Application.dataPath + "/Images/Erosion/" + name + ".png", tex2D.EncodeToPNG());
        Debug.Log("Wrote image to " + Application.dataPath + "/Images/Erosion/" + name + ".png");
    }

}
