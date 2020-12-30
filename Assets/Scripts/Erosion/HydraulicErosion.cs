using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HydraulicErosion
{

    public RenderTexture _stateTexture;
    RenderTexture _waterFluxTexture;
    RenderTexture _velocityTexture; 
    RenderTexture _terrainFluxTexture;

    ComputeShader hydraulicShader;

    int RainAndControl;
    int FluxComputation;
    int FluxApply;
    int HydraulicErosionKernel;
    int SedimentAdvection;

    public float timeDelta = 0.2f;

    public void initializeTextures(RenderTexture heightmap, ComputeShader hydraulicShader)
    {
        this.hydraulicShader = hydraulicShader;

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
        _velocityTexture = new RenderTexture(Width, Height, 0, RenderTextureFormat.RGHalf)
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



        Graphics.Blit(heightmap, _stateTexture);

        exportImages();


        hydraulicShader.SetInt("_Width", _stateTexture.width);
        hydraulicShader.SetInt("_Height", _stateTexture.height);
        hydraulicShader.SetFloat("_TimeDelta", 0.2f);

        hydraulicShader.SetFloat("_PipeArea", 10f);
        hydraulicShader.SetFloat("_Gravity", 9.81f);
        hydraulicShader.SetFloat("_PipeLength", 1f);
        hydraulicShader.SetVector("_CellSize", new Vector2(1f, 1f));

        // Hydraulic erosion
        hydraulicShader.SetFloat("_SedimentCapacity", 0.2f);
        hydraulicShader.SetFloat("_SuspensionRate", 0.5f);
        hydraulicShader.SetFloat("_DepositionRate", 0.5f);

        hydraulicShader.SetFloat("_Evaporation", 0.01f);



        // Unused floats
        hydraulicShader.SetFloat("_RainRate", 0.012f);
        hydraulicShader.SetFloat("_SedimentSofteningRate", 40f);
        hydraulicShader.SetFloat("_MaxErosionDepth", 1f);

        // Thermal erosion
        hydraulicShader.SetFloat("_ThermalErosionRate", 1f);
        hydraulicShader.SetFloat("_TalusAngleTangentCoeff", 0.8f);
        hydraulicShader.SetFloat("_TalusAngleTangentBias", 0.1f);
        hydraulicShader.SetFloat("_ThermalErosionTimeScale", 1f);



        RainAndControl = hydraulicShader.FindKernel("RainAndControl");
        hydraulicShader.SetTexture(RainAndControl, "HeightMap", _stateTexture);
        hydraulicShader.SetTexture(RainAndControl, "VelocityMap", _velocityTexture);
        hydraulicShader.SetTexture(RainAndControl, "FluxMap", _waterFluxTexture);
        hydraulicShader.SetTexture(RainAndControl, "TerrainFluxMap", _terrainFluxTexture);

        FluxComputation = hydraulicShader.FindKernel("FluxComputation");
        hydraulicShader.SetTexture(FluxComputation, "HeightMap", _stateTexture);
        hydraulicShader.SetTexture(FluxComputation, "VelocityMap", _velocityTexture);
        hydraulicShader.SetTexture(FluxComputation, "FluxMap", _waterFluxTexture);
        hydraulicShader.SetTexture(FluxComputation, "TerrainFluxMap", _terrainFluxTexture);

        FluxApply = hydraulicShader.FindKernel("FluxApply");
        hydraulicShader.SetTexture(FluxApply, "HeightMap", _stateTexture);
        hydraulicShader.SetTexture(FluxApply, "VelocityMap", _velocityTexture);
        hydraulicShader.SetTexture(FluxApply, "FluxMap", _waterFluxTexture);
        hydraulicShader.SetTexture(FluxApply, "TerrainFluxMap", _terrainFluxTexture);

        HydraulicErosionKernel = hydraulicShader.FindKernel("HydraulicErosion");
        hydraulicShader.SetTexture(HydraulicErosionKernel, "HeightMap", _stateTexture);
        hydraulicShader.SetTexture(HydraulicErosionKernel, "VelocityMap", _velocityTexture);
        hydraulicShader.SetTexture(HydraulicErosionKernel, "FluxMap", _waterFluxTexture);
        hydraulicShader.SetTexture(HydraulicErosionKernel, "TerrainFluxMap", _terrainFluxTexture);

        SedimentAdvection = hydraulicShader.FindKernel("SedimentAdvection");
        hydraulicShader.SetTexture(SedimentAdvection, "HeightMap", _stateTexture);
        hydraulicShader.SetTexture(SedimentAdvection, "VelocityMap", _velocityTexture);
        hydraulicShader.SetTexture(SedimentAdvection, "FluxMap", _waterFluxTexture);
        hydraulicShader.SetTexture(SedimentAdvection, "TerrainFluxMap", _terrainFluxTexture);

    }

    public void runStep()
    {
        // Update values
        hydraulicShader.SetFloat("_TimeDelta", timeDelta);

        // Add random rain
        hydraulicShader.SetFloat("raindropStrength", 1f);
        hydraulicShader.SetFloat("raindropRadius", 5f);
        hydraulicShader.SetVector("raindropLocation", new Vector2(Random.Range(0f, _stateTexture.width), Random.Range(0f, _stateTexture.height)));

        hydraulicShader.Dispatch(RainAndControl, _stateTexture.width, _stateTexture.height, 1);

        hydraulicShader.Dispatch(FluxComputation, _stateTexture.width, _stateTexture.height, 1);

        hydraulicShader.Dispatch(FluxApply, _stateTexture.width, _stateTexture.height, 1);

        hydraulicShader.Dispatch(HydraulicErosionKernel, _stateTexture.width, _stateTexture.height, 1);

        hydraulicShader.Dispatch(SedimentAdvection, _stateTexture.width, _stateTexture.height, 1);

    }

    public void exportImages()
    {
        saveImage("_stateTexture", _stateTexture, TextureFormat.RGBAFloat);
        saveImage("_velocityTexture", _velocityTexture, TextureFormat.RGBAFloat);
        saveImage("_waterFluxTexture", _waterFluxTexture, TextureFormat.RGBAFloat);
        saveImage("_terrainFluxTexture", _terrainFluxTexture, TextureFormat.RGBAFloat);
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


        System.IO.File.WriteAllBytes(Application.dataPath + "/Images/Erosion/" + name + ".png", tex2D.EncodeToPNG());
        Debug.Log("Wrote image to " + Application.dataPath + "/Images/Erosion/" + name + ".png");
    }
}
