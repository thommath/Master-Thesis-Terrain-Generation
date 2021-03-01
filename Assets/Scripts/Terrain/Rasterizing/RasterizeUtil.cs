using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RasterizeUtil
{
    public static int GetPixelFromXOrY(float xorz, int width, int terrainSize)
    {
        // Scale the xy coordinates to the grid
        return Mathf.RoundToInt((terrainSize / 2f + xorz) / (1f * terrainSize / width));
    }
    public static int GetPixelFromXOrY(float xorz, Texture2D heightmap, int terrainSize)
    {
        return GetPixelFromXOrY(xorz, heightmap.width, terrainSize);
    }
    public static Vector2Int Vector3ToPixelPos(Vector3 vec, int width, int terrainSize)
    {
        return new Vector2Int(GetPixelFromXOrY(vec.x, width, terrainSize), GetPixelFromXOrY(vec.z, width, terrainSize));
    }
    public static Vector2Int Vector3ToPixelPos(Vector3 vec, Texture2D heightmap, int terrainSize)
    {
        return Vector3ToPixelPos(vec, heightmap.width, terrainSize);
    }
}
