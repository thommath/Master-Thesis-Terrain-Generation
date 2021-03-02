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
    }

    public Texture2D tseedHeightmap;
    public Texture2D tRestrictions;
    public Texture2D tseedNormals;
    public Texture2D tNoise;
}