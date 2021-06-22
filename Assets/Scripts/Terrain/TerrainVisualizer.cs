using System;
using UnityEditor;
using UnityEngine;

public class TerrainVisualizer : MonoBehaviour
{
    public Terrain terrain;

    //public enum ViewMode { Heightmap, HeightmapAndWater, Water, Erosion, Sediment, HeightMapWithSediment, HeightmapNoErosion, TerrainAngle, ErosionWithSediment };
    public enum ViewMode { Heightmap, HeightmapNoErosion };
    public ViewMode viewMode = ViewMode.Heightmap;

    public bool listenForChanges = true;
    bool isListeningForChanges = false;

    private void Start()
    {
        loadFromFile();
    }
/*
    void OnValidate()
    {
        if (isListeningForChanges != listenForChanges)
        {
            if (isListeningForChanges)
            {
                this.gameObject.GetComponent<HydraulicErosion>().updatedData.RemoveListener(fastExport);
                this.gameObject.GetComponent<SplineTerrain>().updatedData.RemoveListener(fastExport);
            }
            else
            {
                this.gameObject.GetComponent<HydraulicErosion>().updatedData.AddListener(fastExport);
                this.gameObject.GetComponent<SplineTerrain>().updatedData.AddListener(fastExport);
            }
            isListeningForChanges = !isListeningForChanges;
        }

        if (Application.isEditor && !Application.isPlaying)
        {
            fastExport();
        }

        //exportToTerrain();
    }
*/
    public void fastExport()
    {
        HydraulicErosion erosion = this.gameObject.GetComponent<HydraulicErosion>();
        RenderTexture heightmap = erosion._inputHeight;

        int size = this.gameObject.GetComponent<SplineTerrain>().terrainSize;
        int terrainResolution = this.gameObject.GetComponent<SplineTerrain>().terrainResolution;
        int terrainHeight = this.gameObject.GetComponent<SplineTerrain>().height;

        terrain.gameObject.transform.position = new Vector3(-(size) / 2, 0, -(size) / 2);
        terrain.terrainData.heightmapResolution = terrainResolution;
        terrain.terrainData.size = new Vector3(size, terrainHeight, size);

        if (this.viewMode == ViewMode.HeightmapNoErosion)
        {
            RenderTexture.active = heightmap;
            terrain.terrainData.CopyActiveRenderTextureToHeightmap(new RectInt(0, 0, heightmap.width, heightmap.height), new Vector2Int(0, 0), TerrainHeightmapSyncControl.HeightAndLod);
        } else
        {
            
            RenderTexture result = new RenderTexture(heightmap.width, heightmap.height, 0, RenderTextureFormat.RFloat);
            result.enableRandomWrite = true;
            result.autoGenerateMips = false;
            result.Create();
            Laplace l = this.GetComponent<Laplace>();
            l.SumTwoTextures(result, heightmap, erosion._stateTexture, 1, 0, 1, 0);
            RenderTexture.active = result;
            terrain.terrainData.CopyActiveRenderTextureToHeightmap(new RectInt(0, 0, result.width, result.height), new Vector2Int(0, 0), TerrainHeightmapSyncControl.HeightAndLod);

            //terrain.materialTemplate.SetTexture("_StateTex", erosion._stateTexture);
            //terrain.materialTemplate.SetTexture("_OriginalTex", heightmap);
            result.Release();
        }
        RenderTexture.active = null;

    }


    public void saveToFile()
    {
        HydraulicErosion erosion = this.gameObject.GetComponent<HydraulicErosion>();
        RenderTexture heightmap = erosion._inputHeight;
        RenderTexture result = new RenderTexture(heightmap.width, heightmap.height, 0, RenderTextureFormat.RFloat);

        if (this.viewMode == ViewMode.HeightmapNoErosion)
        {
            RenderTexture.active = heightmap;
        } else
        {
            
            result.enableRandomWrite = true;
            result.autoGenerateMips = false;
            result.Create();
            Laplace l = this.GetComponent<Laplace>();
            l.SumTwoTextures(result, heightmap, erosion._stateTexture, 1, 0, 1, 0);
            RenderTexture.active = result;
        }
        
        Texture2D tex = new Texture2D(heightmap.width, heightmap.height, TextureFormat.RFloat, false);
        tex.ReadPixels(new Rect(0, 0, heightmap.width, heightmap.height), 0, 0);

        byte[] bytes;
        bytes = tex.GetRawTextureData();
        
        string path = "terrain.dat";
        System.IO.File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path);
        Debug.Log("Saved to " + path);
        result.Release();

        
    }

    public void loadFromFile()
    {
        int size = this.gameObject.GetComponent<SplineTerrain>().terrainSize;
        int terrainResolution = this.gameObject.GetComponent<SplineTerrain>().terrainResolution;
        int terrainHeight = this.gameObject.GetComponent<SplineTerrain>().height;

        terrain.gameObject.transform.position = new Vector3(-(size) / 2, 0, -(size) / 2);
        terrain.terrainData.heightmapResolution = terrainResolution;
        terrain.terrainData.size = new Vector3(size, terrainHeight, size);
        
        
        RenderTexture rt = new RenderTexture(terrainResolution + 1, terrainResolution + 1, 0, RenderTextureFormat.ARGBFloat);
        string path = "terrain.dat";

        if (!System.IO.File.Exists(path))
        {
            Debug.Log("Did not find terrain at  " + path);
            return;
        }
            
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RFloat, false);
        byte[] bytes = System.IO.File.ReadAllBytes(path);
        tex.LoadRawTextureData(bytes);
        tex.Apply();
        
        Graphics.Blit(tex, rt);
        RenderTexture.active = rt;
        terrain.terrainData.CopyActiveRenderTextureToHeightmap(new RectInt(0, 0, rt.width, rt.height), new Vector2Int(0, 0), TerrainHeightmapSyncControl.HeightAndLod);
        terrain.materialTemplate.SetTexture("_ReliefMap", tex);
        terrain.materialTemplate.SetTexture("_Restriction", tex);
        RenderTexture.active = null;
        rt.Release();
        Debug.Log("Loaded terrain from " + path);
        
    }

}
