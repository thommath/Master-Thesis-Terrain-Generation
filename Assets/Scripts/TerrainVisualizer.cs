using UnityEngine;

public class TerrainVisualizer : MonoBehaviour
{
    public Terrain terrain;

    public enum ViewMode { Heightmap, HeightmapAndWater, Water, Erosion, Sediment, HeightMapWithSediment, HeightmapNoErosion, TerrainAngle, ErosionWithSediment };
    public ViewMode viewMode = ViewMode.Heightmap;

    public bool listenForChanges = true;
    bool isListeningForChanges = false;

    void OnValidate()
    {
        if (isListeningForChanges != listenForChanges)
        {
            if (isListeningForChanges)
            {
                this.gameObject.GetComponent<HydraulicErosion>().updatedData.RemoveListener(exportToTerrain);
            }
            else
            {
                this.gameObject.GetComponent<HydraulicErosion>().updatedData.AddListener(exportToTerrain);
            }
            isListeningForChanges = !isListeningForChanges;
        }

        exportToTerrain();
    }

    private void exportToTerrain()
    {
        RenderTexture heightmap = this.gameObject.GetComponent<SplineTerrain>().heightmap;
        HydraulicErosion erosion = this.gameObject.GetComponent<HydraulicErosion>();

        int width = heightmap.width;
        int height = heightmap.height;

        if (erosion == null || erosion._stateTexture == null)
        {
            return;
        }

        // Copy other state to terrain
        RenderTexture.active = erosion._stateTexture;
        Texture2D ero = new Texture2D(erosion._stateTexture.width, erosion._stateTexture.height, TextureFormat.RGBAFloat, false);
        ero.ReadPixels(new Rect(0, 0, erosion._stateTexture.width, erosion._stateTexture.height), 0, 0, false);
        ero.Apply();
        RenderTexture.active = null;

        // Copy other state to terrain
        RenderTexture.active = heightmap;
        Texture2D heightTexture = new Texture2D(heightmap.width, heightmap.height, TextureFormat.RFloat, false);
        heightTexture.ReadPixels(new Rect(0, 0, heightmap.width, heightmap.height), 0, 0, false);
        heightTexture.Apply();
        RenderTexture.active = null;

        RenderTexture.active = erosion._terrainFluxTexture;
        Texture2D terrainFlux = new Texture2D(erosion._terrainFluxTexture.width, erosion._terrainFluxTexture.height, TextureFormat.RGBAFloat, false);
        terrainFlux.ReadPixels(new Rect(0, 0, erosion._terrainFluxTexture.width, erosion._terrainFluxTexture.height), 0, 0, false);
        terrainFlux.Apply();
        RenderTexture.active = null;

        float[,] heights = new float[height, width];
        for (int y = 0; y < height - 1; y++)
        {
            for (int x = 0; x < width - 1; x++)
            {
                Color heightColor = heightTexture.GetPixel(x, y);
                Color eroColor = ero.GetPixel(x, y);

                switch (viewMode)
                {
                    case ViewMode.Erosion:
                        heights[y, x] = -heightColor.r + eroColor.r + 0.5f;
                        break;
                    case ViewMode.ErosionWithSediment:
                        heights[y, x] = -heightColor.r + (eroColor.r + eroColor.b) + 0.5f;
                        break;
                    case ViewMode.Heightmap:
                        heights[y, x] = eroColor.r;
                        break;
                    case ViewMode.HeightmapAndWater:
                        heights[y, x] = eroColor.r + eroColor.g;
                        break;
                    case ViewMode.Water:
                        heights[y, x] = eroColor.g;
                        break;
                    case ViewMode.Sediment:
                        heights[y, x] = eroColor.b * 100;
                        break;
                    case ViewMode.HeightMapWithSediment:
                        heights[y, x] = eroColor.b + eroColor.r;
                        break;
                    case ViewMode.HeightmapNoErosion:
                        heights[y, x] = heightColor.r;
                        break;

                    case ViewMode.TerrainAngle:
                        // Copy other state to terrain
                        Color terrainFluxColor = terrainFlux.GetPixel(x, y);
                        heights[y, x] = terrainFluxColor.r * 100;
                        break;
                }
            }
        }

        int size = this.gameObject.GetComponent<SplineTerrain>().size;
        int zoom = this.gameObject.GetComponent<SplineTerrain>().zoom;
        float terrainHeight = this.gameObject.GetComponent<SplineTerrain>().height;

        terrain.gameObject.transform.position = new Vector3(-(size / zoom) / 2, 0, -(size / zoom) / 2);
        terrain.terrainData.heightmapResolution = size;
        terrain.terrainData.size = new Vector3(size / zoom, terrainHeight, size / zoom);
        terrain.terrainData.SetHeights(0, 0, heights);

        terrain.materialTemplate.SetTexture("_StateTex", ero);
        terrain.materialTemplate.SetTexture("_OriginalTex", heightTexture);
    }

}
