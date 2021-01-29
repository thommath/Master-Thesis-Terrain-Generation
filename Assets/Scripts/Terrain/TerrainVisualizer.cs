using UnityEngine;

public class TerrainVisualizer : MonoBehaviour
{
    public Terrain terrain;

    //public enum ViewMode { Heightmap, HeightmapAndWater, Water, Erosion, Sediment, HeightMapWithSediment, HeightmapNoErosion, TerrainAngle, ErosionWithSediment };
    public enum ViewMode { Heightmap, HeightmapNoErosion };
    public ViewMode viewMode = ViewMode.Heightmap;

    public bool listenForChanges = true;
    bool isListeningForChanges = false;

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
        fastExport();

        //exportToTerrain();
    }

    public void fastExport()
    {
        HydraulicErosion erosion = this.gameObject.GetComponent<HydraulicErosion>();
        RenderTexture heightmap = this.gameObject.GetComponent<SplineTerrain>().heightmap;

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
            RenderTexture.active = erosion._stateTexture;
            terrain.terrainData.CopyActiveRenderTextureToHeightmap(new RectInt(0, 0, erosion._stateTexture.width, erosion._stateTexture.height), new Vector2Int(0, 0), TerrainHeightmapSyncControl.HeightAndLod);

            terrain.materialTemplate.SetTexture("_StateTex", erosion._stateTexture);
            terrain.materialTemplate.SetTexture("_OriginalTex", heightmap);
        }
    }

}
