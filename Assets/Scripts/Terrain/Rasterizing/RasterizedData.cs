using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct RasterizedData
{
    public void release()
    {
        if (this.seedHeightmap != null && this.seedHeightmap.IsCreated())
        {
            this.seedHeightmap.Release();
        }
        if (this.restrictions != null && this.restrictions.IsCreated())
        {
            this.restrictions.Release();
        }
        if (this.seedNormals != null && this.seedNormals.IsCreated())
        {
            this.seedNormals.Release();
        }
        if (this.noise != null && this.noise.IsCreated())
        {
            this.noise.Release();
        }
        if (this.erosion != null && this.erosion.IsCreated())
        {
            this.erosion.Release();
        }
    }
    
    public RenderTexture seedHeightmap;
    public RenderTexture restrictions;
    public RenderTexture seedNormals;
    public RenderTexture noise;
    public RenderTexture warp;
    public RenderTexture erosion;
}