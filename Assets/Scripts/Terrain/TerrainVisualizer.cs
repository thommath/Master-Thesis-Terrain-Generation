using System;
using UnityEditor;
using UnityEngine;

public class TerrainVisualizer : MonoBehaviour
{
    public Terrain terrain;
    public Material material;

    //public enum ViewMode { Heightmap, HeightmapAndWater, Water, Erosion, Sediment, HeightMapWithSediment, HeightmapNoErosion, TerrainAngle, ErosionWithSediment };
    public enum ViewMode { Heightmap, HeightmapNoErosion };
    public ViewMode viewMode = ViewMode.Heightmap;

    public bool listenForChanges = true;
    bool isListeningForChanges = false;

    private void Start()
    {
        loadFromFile();
    }
    
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

        if (terrain.materialTemplate.shader.name == "Custom/NewSurfaceShader")
        {
            terrain.materialTemplate.SetTexture("_MainTex", tex);
            Debug.Log("Loaded texture");
            //terrain.materialTemplate.SetTexture("_Restriction", tex);
        }
        else
        {
            Debug.Log(terrain.materialTemplate.shader.name);
        }
        RenderTexture.active = null;
        rt.Release();
        Debug.Log("Loaded terrain from " + path);

        saveTexture();
    }

    public void saveTexture()
    {
        Texture2D tex = GetTextureFromSurfaceShader(material, 2048, 2048);
        
        string path = "terrain.png";
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path);
        Debug.Log("Saved to " + path);
    }
    
    public Texture2D GetTextureFromSurfaceShader(Material mat, int width, int height)
    {
        //Create render texture:
        RenderTexture temp = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
 
        //Create a Quad:
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        MeshRenderer rend = quad.GetComponent<MeshRenderer>();
        rend.material = mat;
        Vector3 quadScale = quad.transform.localScale;
        quad.transform.position = Vector3.forward;
 
        //Setup camera:
        GameObject camGO = new GameObject("CaptureCam");
        Camera cam = camGO.AddComponent<Camera>();
        cam.renderingPath = RenderingPath.Forward;
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(1, 1, 1, 0);
        if (cam.rect.width < 1 || cam.rect.height < 1)
        {
            cam.rect = new Rect(cam.rect.x, cam.rect.y, 1, 1);
        }
        cam.orthographicSize = 0.5f;
        cam.rect = new Rect(0, 0, quadScale.x, quadScale.y);
        cam.aspect = quadScale.x / quadScale.y;
        cam.targetTexture = temp;
        cam.allowHDR = false;
 
 
        //Capture image and write to the render texture:
        cam.Render();
        temp = cam.targetTexture;
 
        //Apply changes:
        Texture2D newTex = new Texture2D(temp.width, temp.height, TextureFormat.ARGB32, true, true);
        RenderTexture.active = temp;
        newTex.ReadPixels(new Rect(0, 0, temp.width, temp.height), 0, 0);
        newTex.Apply();
 
        //Clean up:
        RenderTexture.active = null;
        temp.Release();
        DestroyImmediate(quad);
        DestroyImmediate(camGO);
 
        return newTex;
    }

}
