using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Rasterize
{

    public static void rasterizeSplines(BezierSpline[] splines, Dictionary<int, RasterizedData> rasterizedDataDict, int terrainSizeExp, int maxHeight, int resolution, int breakOnLevel = 1)
    {
        int terrainSize = Mathf.RoundToInt(Mathf.Pow(2, terrainSizeExp)) + 1;

        foreach (BezierSpline spline in splines)
        {
            spline.rasterizingData = RasterizingTriangles.getSplineData(spline, terrainSize, maxHeight, resolution);
        }


        float[,,,] normalValues = new float[terrainSizeExp - breakOnLevel, terrainSize, terrainSize, 4];
        float[,,,] heightValues = new float[terrainSizeExp - breakOnLevel, terrainSize, terrainSize, 2];
        float[,,,] restrictionValues = new float[terrainSizeExp - breakOnLevel, terrainSize, terrainSize, 3];
        float[,,,] noiseValues = new float[terrainSizeExp - breakOnLevel, terrainSize, terrainSize, 3];


        Color gradientColorStart = new Color(0.7f, 0.3f, 0, 1f);
        Color gradientColorEnd = new Color(1f, 0.0f, 0, 1f);


        //////////////////////////////////////
        ///
        ///   Rasterizing each spline
        ///
        //////////////////////////////////////

        foreach (BezierSpline spline in splines)
        {
            RasterizingSplineData data = spline.rasterizingData;


            //////////////////////////////////////
            ///
            ///   Rasterize gradients
            ///
            //////////////////////////////////////
            int[][] indices = { data.meshLeft.triangles, data.meshRight.triangles };
            Vector3[][] vertices = { data.meshLeft.vertices, data.meshRight.vertices };
            Color[][] vertColors = { data.meshLeft.colors, data.meshRight.colors };

            for (int item = 0; item < indices.Length; item += 1)
            {
                for (int n = 0; n < indices[item].Length; n += 3)
                {
                    for (int gridLevel = terrainSizeExp - breakOnLevel - 1; gridLevel >= 0; gridLevel--)
                    {
                        bool anyPixels = false;
                        int gridLevelSize = Mathf.RoundToInt(Mathf.Pow(2, gridLevel + 1 + breakOnLevel)) + 1;

                        foreach (PixelData pixel in TriangleRenderer.RasterizeTriangle(gridLevelSize, gridLevelSize,
                            RasterizeUtil.Vector3ToPixelPos(vertices[item][indices[item][n]], gridLevelSize, terrainSize),
                            RasterizeUtil.Vector3ToPixelPos(vertices[item][indices[item][n + 1]], gridLevelSize, terrainSize),
                            RasterizeUtil.Vector3ToPixelPos(vertices[item][indices[item][n + 2]], gridLevelSize, terrainSize)))
                        {
                            anyPixels = true;

                            Color[] currentColorForRestriction;
                            if (spline.elevationConstraint)
                            {
                                currentColorForRestriction = new Color[]
                                {
                                    indices[item][n] % 2 == 0 ? gradientColorStart : gradientColorEnd,
                                    indices[item][n+1] % 2 == 0 ? gradientColorStart : gradientColorEnd,
                                    indices[item][n+2] % 2 == 0 ? gradientColorStart : gradientColorEnd
                                };
                            }
                            else
                            {
                                currentColorForRestriction = new Color[]
                                {
                                    indices[item][n] % 2 == 0 ? gradientColorStart : gradientColorStart,
                                    indices[item][n+1] % 2 == 0 ? gradientColorStart : gradientColorStart,
                                    indices[item][n+2] % 2 == 0 ? gradientColorStart : gradientColorStart
                                };
                            }
                            Color c = pixel.getColor(currentColorForRestriction[0], currentColorForRestriction[1], currentColorForRestriction[2]);
                            restrictionValues[gridLevel, pixel.x, pixel.y, 0] = 1f - c.g;
                            restrictionValues[gridLevel, pixel.x, pixel.y, 1] = c.g;
                            restrictionValues[gridLevel, pixel.x, pixel.y, 2] += 1;

                            c = pixel.getColor(
                                vertColors[item][indices[item][n]],
                                vertColors[item][indices[item][n + 1]],
                                vertColors[item][indices[item][n + 2]]);

                            //normalValues[gridLevel, pixel.x, pixel.y, 0] = c.r;
                            //normalValues[gridLevel, pixel.x, pixel.y, 1] = c.g;
                            normalValues[gridLevel, pixel.x, pixel.y, 2] = c.b;
                            // normalValues[gridLevel, pixel.x, pixel.y, 3] += 1;
                        }

                        if (!anyPixels) break;
                    }
                }
            }

            //////////////////////////////////////
            ///
            ///   Rasterize thick lines
            ///
            //////////////////////////////////////
            indices = new int[][] { data.meshLine.triangles };
            vertices = new Vector3[][] { data.meshLine.vertices };
            vertColors = new Color[][] { data.meshLine.colors };

            if (spline.elevationConstraint)
            {
                for (int item = 0; item < indices.Length; item += 1)
                {
                    for (int n = 0; n < indices[item].Length; n += 3)
                    {
                        for (int gridLevel = terrainSizeExp - breakOnLevel - 1; gridLevel >= 0; gridLevel--)
                        {
                            bool anyPixels = false;
                            int gridLevelSize = Mathf.RoundToInt(Mathf.Pow(2, gridLevel + 1 + breakOnLevel)) + 1;

                            foreach (PixelData pixel in TriangleRenderer.RasterizeTriangle(gridLevelSize, gridLevelSize,
                                RasterizeUtil.Vector3ToPixelPos(vertices[item][indices[item][n]], gridLevelSize, terrainSize),
                                RasterizeUtil.Vector3ToPixelPos(vertices[item][indices[item][n + 1]], gridLevelSize, terrainSize),
                                RasterizeUtil.Vector3ToPixelPos(vertices[item][indices[item][n + 2]], gridLevelSize, terrainSize)))
                            {
                                anyPixels = true;

                                heightValues[gridLevel, pixel.x, pixel.y, 0] += 
                                    pixel.getColor(vertColors[item][indices[item][n]],
                                        vertColors[item][indices[item][n + 1]],
                                        vertColors[item][indices[item][n + 2]]).r;

                                heightValues[gridLevel, pixel.x, pixel.y, 1] += 1;

                                restrictionValues[gridLevel, pixel.x, pixel.y, 0] = 0;
                                restrictionValues[gridLevel, pixel.x, pixel.y, 1] = 0;
                                restrictionValues[gridLevel, pixel.x, pixel.y, 2] += 1;
                            }

                            if (!anyPixels) break;
                        }
                    }
                }
            }

            ////////////////////////////////////////////////////
            ////
            ///       Line renderer
            ////
            ///////////////////////////////////////////////////

            Vector3 lastPoint = spline.GetPoint(0);
            // How many lines the spline should be cut into
            for (int n = 1; n <= resolution; n++)
            {
                float distOnSpline = (1f * n) / resolution;
                Vector3 point = spline.GetPoint(distOnSpline);

                Vector2 perpendicular = Vector2.Perpendicular(new Vector2(lastPoint.x - point.x, lastPoint.z - point.z)).normalized;


                for (int gridLevel = terrainSizeExp - breakOnLevel - 1; gridLevel >= 0; gridLevel--)
                {
                    int gridLevelSize = Mathf.RoundToInt(Mathf.Pow(2, gridLevel + 1 + breakOnLevel)) + 1;

                    Vector2Int fromPixel = RasterizeUtil.Vector3ToPixelPos(lastPoint, gridLevelSize, terrainSize);
                    Vector2Int toPixel = RasterizeUtil.Vector3ToPixelPos(point, gridLevelSize, terrainSize);
                    float distBetweenPoints = Vector2Int.Distance(fromPixel, toPixel);

                    if (distBetweenPoints == 0)
                    {
                        break;
                    }

                    //foreach (Vector2Int pixel in GetPixelsOfLine(fromPixel.x, fromPixel.y, toPixel.x, toPixel.y))
                    foreach (Vector3Int pixelWithErr in GetPixelsOfLineAntiAliased(fromPixel.x, fromPixel.y, toPixel.x, toPixel.y))
                    {
                        Vector2Int pixel = new Vector2Int(pixelWithErr.x, pixelWithErr.y);
                        if (pixel.x >= gridLevelSize || pixel.y >= gridLevelSize || pixel.x < 0 || pixel.y < 0)
                        {
                            continue;
                        }

                        float distOnLine = Vector2Int.Distance(fromPixel, pixel);
                        float interpolatedHeight = lastPoint.y + (point.y - lastPoint.y) * (distOnLine / distBetweenPoints);

                        if (spline.elevationConstraint)
                        {
                            heightValues[gridLevel, pixel.x, pixel.y, 0] += interpolatedHeight / maxHeight;
                            heightValues[gridLevel, pixel.x, pixel.y, 1] += 1;

                            float val = pixelWithErr.z / 255f;
                            restrictionValues[gridLevel, pixel.x, pixel.y, 0] *= val;
                            restrictionValues[gridLevel, pixel.x, pixel.y, 1] *= val;
                            restrictionValues[gridLevel, pixel.x, pixel.y, 2] += 1;
                        }


                        // Normal vectors
                        Vector2Int movedPixel1 = Vector2Int.CeilToInt(pixel + perpendicular * 1.5f);
                        if (movedPixel1.x < gridLevelSize && movedPixel1.x > 0 && movedPixel1.y < gridLevelSize && movedPixel1.y > 0)
                        {
                            if (spline.elevationConstraint)
                            {
                                normalValues[gridLevel, movedPixel1.x, movedPixel1.y, 0] += (1 + perpendicular.x) / 2;
                                normalValues[gridLevel, movedPixel1.x, movedPixel1.y, 1] += (1 + perpendicular.y) / 2;
                                normalValues[gridLevel, movedPixel1.x, movedPixel1.y, 3] += 1;
                            }
                            else
                            {
                                normalValues[gridLevel, movedPixel1.x, movedPixel1.y, 0] += (1 - perpendicular.x) / 2;
                                normalValues[gridLevel, movedPixel1.x, movedPixel1.y, 1] += (1 - perpendicular.y) / 2;
                                normalValues[gridLevel, movedPixel1.x, movedPixel1.y, 3] += 1;
                            }
                        }

                        Vector2Int movedPixel2 = Vector2Int.CeilToInt(pixel - perpendicular * 1.5f);
                        if (movedPixel2.x < gridLevelSize && movedPixel2.x > 0 && movedPixel2.y < gridLevelSize && movedPixel2.y > 0)
                        {
                            if (spline.elevationConstraint)
                            {
                                normalValues[gridLevel, movedPixel2.x, movedPixel2.y, 0] += (1 - perpendicular.x) / 2;
                                normalValues[gridLevel, movedPixel2.x, movedPixel2.y, 1] += (1 - perpendicular.y) / 2;
                                normalValues[gridLevel, movedPixel2.x, movedPixel2.y, 3] += 1;
                            }
                            else
                            {
                                normalValues[gridLevel, movedPixel2.x, movedPixel2.y, 0] += (1 + perpendicular.x) / 2;
                                normalValues[gridLevel, movedPixel2.x, movedPixel2.y, 1] += (1 + perpendicular.y) / 2;
                                normalValues[gridLevel, movedPixel2.x, movedPixel2.y, 3] += 1;
                            }
                        }

                        // Noise
                        if (spline.metaPoints.Length > 0)
                        {
                            SplineMetaPoint metaPoint = spline.getMetaPointInterpolated(distOnSpline);
                            noiseValues[gridLevel, pixel.x, pixel.y, 0] += metaPoint.noiseAmplitude;
                            noiseValues[gridLevel, pixel.x, pixel.y, 1] += metaPoint.noiseRoughness;
                            noiseValues[gridLevel, pixel.x, pixel.y, 2] += 1;
                        }

                    }
                }

                lastPoint = point;
            }

        }



        ////////////////////////////////////////////////////
        ////
        ///       Process values
        ////
        ///////////////////////////////////////////////////

        for (int gridLevel = terrainSizeExp - breakOnLevel - 1; gridLevel >= 0; gridLevel--)
        {
            int gridLevelSize = Mathf.RoundToInt(Mathf.Pow(2, gridLevel + 1 + breakOnLevel)) + 1;

            Color[] heightColors = new Color[gridLevelSize * gridLevelSize];
            Color[] normalColors = new Color[gridLevelSize * gridLevelSize];
            Color[] noiseColors = new Color[gridLevelSize * gridLevelSize];
            Color[] restrictionColors = new Color[gridLevelSize * gridLevelSize];
            for (int x = 0; x < gridLevelSize; x++)
            {
                for (int y = 0; y < gridLevelSize; y++)
                {
                    // Height
                    if (heightValues[gridLevel, x, y, 1] > 0)
                    {
                        heightColors[x + y * gridLevelSize] = new Color(heightValues[gridLevel, x, y, 0] / heightValues[gridLevel, x, y, 1], 0, 0, 1);
                    } else
                    {
                        heightColors[x + y * gridLevelSize] = Color.clear;
                    }
                    // Normals
                    if (normalValues[gridLevel, x, y, 3] > 0)
                    {
                        normalColors[x + y * gridLevelSize] = new Color(normalValues[gridLevel, x, y, 0] / normalValues[gridLevel, x, y, 3], normalValues[gridLevel, x, y, 1] / normalValues[gridLevel, x, y, 3], normalValues[gridLevel, x, y, 2], 1);
                    }
                    else
                    {
                        normalColors[x + y * gridLevelSize] = new Color(0, 0, normalValues[gridLevel, x, y, 2], 0);
                    }
                    // Noise
                    if (noiseValues[gridLevel, x, y, 2] > 0)
                    {
                        noiseColors[x + y * gridLevelSize] = new Color(noiseValues[gridLevel, x, y, 0] / noiseValues[gridLevel, x, y, 2], noiseValues[gridLevel, x, y, 1] / noiseValues[gridLevel, x, y, 2], 0, 1);
                    }
                    else
                    {
                        noiseColors[x + y * gridLevelSize] = Color.clear;
                    }

                    // Restriction
                    if (restrictionValues[gridLevel, x, y, 2] > 0)
                    {
                        restrictionColors[x + y * gridLevelSize] = new Color(restrictionValues[gridLevel, x, y, 0], restrictionValues[gridLevel, x, y, 1], 0, 1);
                    }
                    else
                    {
                        restrictionColors[x + y * gridLevelSize] = Color.red;
                    }
                }
            }


            Texture2D heightmap = new Texture2D(gridLevelSize, gridLevelSize, TextureFormat.RGBAFloat, false);
            Texture2D restrictions = new Texture2D(gridLevelSize, gridLevelSize, TextureFormat.RGBAFloat, false);
            Texture2D normals = new Texture2D(gridLevelSize, gridLevelSize, TextureFormat.RGBAFloat, false);
            Texture2D noise = new Texture2D(gridLevelSize, gridLevelSize, TextureFormat.RGBAFloat, false);

            heightmap.SetPixels(0, 0, normals.width, normals.height, heightColors);
            normals.SetPixels(0, 0, normals.width, normals.height, normalColors);
            noise.SetPixels(0, 0, normals.width, normals.height, noiseColors);
            restrictions.SetPixels(0, 0, normals.width, normals.height, restrictionColors);


            heightmap.Apply();
            normals.Apply();
            noise.Apply();
            restrictions.Apply();

            rasterizedDataDict.Add(gridLevelSize, new RasterizedData(heightmap, restrictions, normals, noise));
        }
    }

    /**
     * Brenderson line drawing
     */
    public static IEnumerable<Vector2Int> GetPixelsOfLine(int x, int y, int x2, int y2)
    {
        int w = x2 - x;
        int h = y2 - y;
        int dx1 = 0, dy1 = 0, dx2 = 0, dy2 = 0;
        if (w < 0) dx1 = -1; else if (w > 0) dx1 = 1;
        if (h < 0) dy1 = -1; else if (h > 0) dy1 = 1;
        if (w < 0) dx2 = -1; else if (w > 0) dx2 = 1;
        int longest = Mathf.Abs(w);
        int shortest = Mathf.Abs(h);
        if (!(longest > shortest))
        {
            longest = Mathf.Abs(h);
            shortest = Mathf.Abs(w);
            if (h < 0) dy2 = -1; else if (h > 0) dy2 = 1;
            dx2 = 0;
        }
        int numerator = longest >> 1;
        for (int i = 0; i <= longest; i++)
        {
            yield return new Vector2Int(x, y);

            numerator += shortest;
            if (!(numerator < longest))
            {
                numerator -= longest;
                x += dx1;
                y += dy1;
            }
            else
            {
                x += dx2;
                y += dy2;
            }
        }
        yield break;
    }

    /**
     * Anti Aliased Brenderson line drawing
     */
    public static IEnumerable<Vector3Int> GetPixelsOfLineAntiAliased(int x0, int y0, int x1, int y1)
    {
        int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int x2, e2, err = dx - dy; /* error value e_xy */
        int ed = dx + dy == 0 ? 1 : ((int) Math.Sqrt((float)dx * dx + (float)dy * dy));
        for (; ; )
        { /* pixel loop */
            yield return new Vector3Int(x0, y0, 255 * Math.Abs(err - dx + dy) / ed);
            e2 = err; x2 = x0;
            if (2 * e2 >= -dx)
            { /* x step */
                if (x0 == x1) break;
                if (e2 + dy < ed) yield return new Vector3Int(x0, y0 + sy, 255 * (e2 + dy) / ed);
                err -= dy; x0 += sx;
            }
            if (2 * e2 <= dy)
            { /* y step */
                if (y0 == y1) break;
                if (dx - e2 < ed) yield return new Vector3Int(x2 + sx, y0, 255 * (dx - e2) / ed);
                err += dx; y0 += sy;
            }
        }

        yield break;
    }


    private static void saveImage(string name, RenderTexture tex, bool useExr = false)
    {
        if (useExr)
        {
            RenderTexture.active = tex;
            Texture2D tex2D = new Texture2D(tex.width, tex.height, TextureFormat.RGBAHalf, false);
            tex2D.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0, false);
            tex2D.Apply();
            RenderTexture.active = null;
            System.IO.File.WriteAllBytes(Application.dataPath + "/Images/" + name + ".exr", tex2D.EncodeToEXR(Texture2D.EXRFlags.None));
            Debug.Log("Wrote image to " + Application.dataPath + "/Images/" + name + ".exr");
        }
        else
        {
            // Now you can read it back to a Texture2D and save it
            RenderTexture.active = tex;
            Texture2D tex2D = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, true);
            tex2D.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0, false);
            tex2D.Apply();
            RenderTexture.active = null;
            System.IO.File.WriteAllBytes(Application.dataPath + "/Images/" + name + ".png", tex2D.EncodeToPNG());
            Debug.Log("Wrote image to " + Application.dataPath + "/Images/" + name + ".png");
        }

    }

}
