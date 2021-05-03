using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct RasterizedData
{
    public RasterizedData(Texture2D tseedHeightmap, Texture2D tRestrictions, Texture2D tseedNormals, Texture2D tNoise)
    {
        this.tseedHeightmap = tseedHeightmap;
        this.tRestrictions = tRestrictions;
        this.tseedNormals = tseedNormals;
        this.tNoise = tNoise;

        this.seedHeightmap = null;
        this.restrictions = null;
        this.seedNormals = null;
        this.noise = null;
        this.erosion = null;
        this.warp = null;
    }

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

    public Texture2D tseedHeightmap;
    public Texture2D tRestrictions;
    public Texture2D tseedNormals;
    public Texture2D tNoise;
    
    
    public RenderTexture seedHeightmap;
    public RenderTexture restrictions;
    public RenderTexture seedNormals;
    public RenderTexture noise;
    public RenderTexture warp;
    public RenderTexture erosion;
}