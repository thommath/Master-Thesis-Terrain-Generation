using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Rasterize
{
    static int GetPixelFromXOrY(float xorz, Texture2D heightmap, int terrainSize)
    {
        // Scale the xy coordinates to the grid
        return Mathf.RoundToInt((terrainSize / 2f + xorz) / (1f * terrainSize / heightmap.width));
    }
    static Vector2Int Vector3ToPixelPos(Vector3 vec, Texture2D heightmap, int terrainSize)
    {
        return new Vector2Int(GetPixelFromXOrY(vec.x, heightmap, terrainSize), GetPixelFromXOrY(vec.z, heightmap, terrainSize));
    }

    public static void rasterizeSplineTriangles(BezierSpline[] splines, Texture2D heightmap, Texture2D restrictions, Texture2D normals, int terrainSize, int maxHeight, int resolution)
    {
        Color gradientColorStart = new Color(0.7f, 0.3f, 0, 1f);
        Color gradientColorEnd = new Color(1f, 0.0f, 0, 1f);

        foreach (BezierSpline spline in splines)
        {
            RasterizingSplineData splineData = spline.rasterizingData;

            if (splineData == null || true)
            {
                splineData = new RasterizingSplineData();
                spline.rasterizingData = splineData;
            }

            Mesh meshRight = splineData.meshRight;
            Mesh meshLeft = splineData.meshLeft;

            Vector3[] verteciesRight = new Vector3[(resolution * spline.CurveCount + 1) * 2];
            int[] trianglesRight = new int[(resolution * spline.CurveCount) * 2 * 3];
            Color[] colorsRight = new Color[(resolution * spline.CurveCount + 1) * 2];

            Vector3[] verteciesLeft = new Vector3[(resolution * spline.CurveCount + 1) * 2];
            int[] trianglesLeft = new int[(resolution * spline.CurveCount) * 2 * 3];
            Color[] colorsLeft = new Color[(resolution * spline.CurveCount + 1) * 2];

            Mesh meshLine = splineData.meshLine;
            Vector3[] verteciesLine = new Vector3[(resolution * spline.CurveCount + 1) * 2];
            int[] trianglesLine = new int[(resolution * spline.CurveCount) * 2 * 3];
            Color[] colorsLineHeight = new Color[(resolution * spline.CurveCount + 1) * 2];


            // How many lines the spline should be cut into
            for (int n = 0; n <= resolution * spline.CurveCount; n++)
            {
                float distOnSpline = (1f * n) / (resolution);

                Vector3 point = spline.GetPoint(distOnSpline);
                SplineMetaPoint metaPoint = spline.getMetaPointInterpolated(distOnSpline);

                verteciesLine[n * 2] = metaPoint.getLineLeftEnd(spline);// point + 0.5f * spline.lineRadius * new Vector3(perpendicular.x, 0, perpendicular.y).normalized;
                verteciesLine[n * 2 + 1] = metaPoint.getLineRightEnd(spline); //point - 0.5f * spline.lineRadius * new Vector3(perpendicular.x, 0, perpendicular.y).normalized;
                colorsLineHeight[n * 2] = new Color(point.y / maxHeight, 0, 0, 1);
                colorsLineHeight[n * 2 + 1] = new Color(point.y / maxHeight, 0, 0, 1);

                if (n > 0)
                {
                    trianglesLine[(n * 2 - 1) * 3] = n * 2;
                    trianglesLine[(n * 2 - 1) * 3 + 2] = n * 2 + 1;
                    trianglesLine[(n * 2 - 1) * 3 + 1] = n * 2 - 1;
                }
                if (n < resolution * spline.CurveCount)
                {
                    trianglesLine[n * 2 * 3] = n * 2;
                    trianglesLine[n * 2 * 3 + 2] = n * 2 + 2;
                    trianglesLine[n * 2 * 3 + 1] = n * 2 + 1;
                }

                {
                    verteciesRight[n * 2] = metaPoint.getLineRightEnd(spline); //point + 0.5f * spline.lineRadius * new Vector3(perpendicular.x, 0, perpendicular.y).normalized;
                    verteciesRight[n * 2 + 1] = metaPoint.getGradientRightEnd(spline); //point + 0.5f * spline.lineRadius * new Vector3(perpendicular.x, 0, perpendicular.y).normalized + spline.gradientLengthRight * new Vector3(perpendicular.x, 0, perpendicular.y).normalized;
                    colorsRight[n * 2] = new Color(0f, 0f, 0.5f - 0.5f * (metaPoint.gradientAngleRight / metaPoint.gradientLengthRight) / maxHeight);
                    colorsRight[n * 2 + 1] = new Color(0f, 0f, 0.5f - 0.5f * (metaPoint.gradientAngleRight / metaPoint.gradientLengthRight) / maxHeight);

                    if (n > 0)
                    {
                        trianglesRight[(n * 2 - 1) * 3] = n * 2;
                        trianglesRight[(n * 2 - 1) * 3 + 1] = n * 2 + 1;
                        trianglesRight[(n * 2 - 1) * 3 + 2] = n * 2 - 1;
                    }
                    if (n < resolution * spline.CurveCount)
                    {
                        trianglesRight[n * 2 * 3] = n * 2;
                        trianglesRight[n * 2 * 3 + 1] = n * 2 + 2;
                        trianglesRight[n * 2 * 3 + 2] = n * 2 + 1;
                    }
                }
                {
                    verteciesLeft[n * 2] = metaPoint.getLineLeftEnd(spline); //point - 0.5f * spline.lineRadius * new Vector3(perpendicular.x, 0, perpendicular.y).normalized;
                    verteciesLeft[n * 2 + 1] = metaPoint.getGradientLeftEnd(spline); // point - 0.5f * spline.lineRadius * new Vector3(perpendicular.x, 0, perpendicular.y).normalized - spline.gradientLengthLeft * new Vector3(perpendicular.x, 0, perpendicular.y).normalized;
                    colorsLeft[n * 2] = new Color(0f, 0f, 0.5f - 0.5f * (metaPoint.gradientAngleLeft / metaPoint.gradientLengthLeft) / maxHeight);
                    colorsLeft[n * 2 + 1] = new Color(0f, 0f, 0.5f - 0.5f * (metaPoint.gradientAngleLeft / metaPoint.gradientLengthLeft) / maxHeight);

                    if (n > 0)
                    {
                        trianglesLeft[(n * 2 - 1) * 3] = n * 2;
                        trianglesLeft[(n * 2 - 1) * 3 + 2] = n * 2 + 1;
                        trianglesLeft[(n * 2 - 1) * 3 + 1] = n * 2 - 1;
                    }
                    if (n < resolution * spline.CurveCount)
                    {
                        trianglesLeft[n * 2 * 3] = n * 2;
                        trianglesLeft[n * 2 * 3 + 2] = n * 2 + 2;
                        trianglesLeft[n * 2 * 3 + 1] = n * 2 + 1;
                    }
                }
            }
            meshRight.vertices = verteciesRight;
            meshRight.triangles = trianglesRight;
            meshRight.colors = colorsRight;

            meshLeft.vertices = verteciesLeft;
            meshLeft.triangles = trianglesLeft;
            meshLeft.colors = colorsLeft;

            meshLine.vertices = verteciesLine;
            meshLine.triangles = trianglesLine;
            meshLine.colors = colorsLineHeight;
        }


        Color[] seed = new Color[heightmap.width * heightmap.height];
        Color[] restriction = new Color[heightmap.width * heightmap.height];
        Color[] normal = new Color[heightmap.width * heightmap.height];
        ushort[] counter = new ushort[heightmap.width * heightmap.height];

        foreach (BezierSpline spline in splines)
        {
            RasterizingSplineData data = spline.rasterizingData;

            // Rasterize Shoulders
            int[][] indices = { data.meshLeft.triangles, data.meshRight.triangles };
            Vector3[][] vertices = { data.meshLeft.vertices, data.meshRight.vertices };
            Color[][] vertColors = { data.meshLeft.colors, data.meshRight.colors };

            for (int item = 0; item < indices.Length; item += 1)
            {
                for (int n = 0; n < indices[item].Length; n += 3)
                {
                    foreach (PixelData pixel in TriangleRenderer.RasterizeTriangle(heightmap.width, heightmap.height,
                        Vector3ToPixelPos(vertices[item][indices[item][n]], heightmap, terrainSize),
                        Vector3ToPixelPos(vertices[item][indices[item][n+1]], heightmap, terrainSize),
                        Vector3ToPixelPos(vertices[item][indices[item][n+2]], heightmap, terrainSize)))
                    {
                        counter[pixel.position] += 1;

                        Color[] currentColorForRestriction =
                        {
                            indices[item][n] % 2 == 0 ? gradientColorStart : gradientColorEnd,
                            indices[item][n+1] % 2 == 0 ? gradientColorStart : gradientColorEnd,
                            indices[item][n+2] % 2 == 0 ? gradientColorStart : gradientColorEnd
                        };
                        restriction[pixel.position] = pixel.getColor(currentColorForRestriction[0], currentColorForRestriction[1], currentColorForRestriction[2]);

                        normal[pixel.position] = pixel.getColor(
                            vertColors[item][indices[item][n]],
                            vertColors[item][indices[item][n + 1]],
                            vertColors[item][indices[item][n + 2]]);
                    }
                }
            }

            // Rasterize lines
            indices = new int[][]{ data.meshLine.triangles };
            vertices = new Vector3[][] { data.meshLine.vertices };
            vertColors = new Color[][] { data.meshLine.colors };

            for (int item = 0; item < indices.Length; item += 1)
            {
                for (int n = 0; n < indices[item].Length; n += 3)
                {
                    foreach (PixelData pixel in TriangleRenderer.RasterizeTriangle(heightmap.width, heightmap.height,
                        Vector3ToPixelPos(vertices[item][indices[item][n]], heightmap, terrainSize),
                        Vector3ToPixelPos(vertices[item][indices[item][n + 1]], heightmap, terrainSize),
                        Vector3ToPixelPos(vertices[item][indices[item][n + 2]], heightmap, terrainSize)))
                    {
                        counter[pixel.position] = 1;

                        seed[pixel.position] = pixel.getColor(vertColors[item][indices[item][n]],
                            vertColors[item][indices[item][n + 1]],
                            vertColors[item][indices[item][n + 2]]);

                        /*if (pixel.getColor(vertColors[item][indices[item][n]],
                            vertColors[item][indices[item][n + 1]],
                            vertColors[item][indices[item][n + 2]]).r < 3f / maxHeight)
                        {
                            Debug.LogError("Pixel height is 0 at position " + pixel.position.ToString());
                        }*/

                        restriction[pixel.position] = new Color(0, 0, 0, 1);
                    }
                }
            }
        }
        
        for(int n = 0; n < restriction.Length; n += 1)
        {
            if (counter[n] != 1)
            {
                //
                normal[n] = new Color(0, 0, 0, 0);
                
                //if (counter[n] == 0)
                {
                    restriction[n] = new Color(1, 0, 0, 1);
                }
            } 
            
        }

        heightmap.SetPixels(0, 0, heightmap.width, heightmap.height, seed);
        restrictions.SetPixels(0, 0, heightmap.width, heightmap.height, restriction);
        normals.SetPixels(0, 0, heightmap.width, heightmap.height, normal);
    }


    public static void rasterizeSplineLines(BezierSpline[] splines, Texture2D heightmap, Texture2D restrictions, Texture2D normals, Texture2D noise, int terrainSize, int maxHeight, int resolution)
    {
        float[,,] normalValues = new float[normals.width, normals.height, 3];
        float[,,] heightValues = new float[normals.width, normals.height, 2];
        float[,,] noiseValues = new float[normals.width, normals.height, 3];

        foreach (BezierSpline spline in splines)
        {
            Vector3 lastPoint = Vector3.zero;
            // How many lines the spline should be cut into
            for (int n = 0; n < resolution; n++)
            {
                float distOnSpline = (1f * n) / resolution;
                Vector3 point = spline.GetPoint(distOnSpline);

                if (lastPoint != Vector3.zero)
                {
                    Vector2 perpendicular = Vector2.Perpendicular(new Vector2(lastPoint.x - point.x, lastPoint.z - point.z)).normalized;

                    Vector2Int fromPixel = Vector3ToPixelPos(lastPoint, heightmap, terrainSize);
                    Vector2Int toPixel = Vector3ToPixelPos(point, heightmap, terrainSize);
                    float distBetweenPoints = Vector2Int.Distance(fromPixel, toPixel);

                    if (distBetweenPoints == 0)
                    {
                        continue;
                    }

                    foreach (Vector2Int pixel in GetPixelsOfLine(fromPixel.x, fromPixel.y, toPixel.x, toPixel.y))
                    {
                        if (pixel.x >= normals.width || pixel.y >= normals.height || pixel.x < 0 || pixel.y < 0)
                        {
                            continue;
                        }

                        float distOnLine = Vector2Int.Distance(fromPixel, pixel);
                        float interpolatedHeight = lastPoint.y + (point.y - lastPoint.y) * (distOnLine / distBetweenPoints);

                        heightValues[pixel.x, pixel.y, 0] += interpolatedHeight / maxHeight;
                        heightValues[pixel.x, pixel.y, 1] += 1;

                        restrictions.SetPixel(
                            pixel.x, pixel.y,
                            new Color(0, 0, 0, 1));


                        // Normal vectors
                        Vector2Int movedPixel1 = Vector2Int.CeilToInt(pixel + perpendicular * 1.5f);
                        if (movedPixel1.x < normals.width && movedPixel1.x > 0 && movedPixel1.y < normals.height && movedPixel1.y > 0)
                        {
                            normalValues[movedPixel1.x, movedPixel1.y, 0] += (1 + perpendicular.x) / 2;
                            normalValues[movedPixel1.x, movedPixel1.y, 1] += (1 + perpendicular.y) / 2;
                            normalValues[movedPixel1.x, movedPixel1.y, 2] += 1;
                        }

                        Vector2Int movedPixel2 = Vector2Int.CeilToInt(pixel - perpendicular * 1.5f);
                        if (movedPixel2.x < normals.width && movedPixel2.x > 0 && movedPixel2.y < normals.height && movedPixel2.y > 0) 
                        {
                            normalValues[movedPixel2.x, movedPixel2.y, 0] += (1 - perpendicular.x) / 2;
                            normalValues[movedPixel2.x, movedPixel2.y, 1] += (1 - perpendicular.y) / 2;
                            normalValues[movedPixel2.x, movedPixel2.y, 2] += 1;
                        }

                        // Noise
                        if (spline.metaPoints.Length > 0)
                        {
                            SplineMetaPoint metaPoint = spline.getMetaPointInterpolated(distOnSpline);
                            noiseValues[pixel.x, pixel.y, 0] += metaPoint.noiseAmplitude;
                            noiseValues[pixel.x, pixel.y, 1] += metaPoint.noiseRoughness;
                            noiseValues[pixel.x, pixel.y, 2] += 1;
                        }

                    }
                }
                lastPoint = point;
            }
        }
        Color[] heightColors = new Color[normals.width * normals.height];
        Color[] normalColors = new Color[normals.width * normals.height];
        Color[] noiseColors = new Color[normals.width * normals.height];
        for (int x = 0; x < normals.width; x++)
        {
            for (int y = 0; y < normals.height; y++)
            {
                // Height
                Color oldSeedColor = heightmap.GetPixel(x, y);
                if (oldSeedColor.r == 0)
                {
                    heightColors[x + y*normals.width] = new Color(heightValues[x, y, 0] / heightValues[x, y, 1], 0, 0, 1);
                } else
                {
                    heightColors[x + y * normals.width] = oldSeedColor;
                }

                // Normals
                Color oldNormalColor1 = normals.GetPixel(x,y);
                if (normalValues[x, y, 2] > 0)
                {
                    normalColors[x + y * normals.width] = new Color(normalValues[x, y, 0] / normalValues[x, y, 2], normalValues[x, y, 1] / normalValues[x, y, 2], oldNormalColor1.b, 1);
                }
                else
                {
                    normalColors[x + y * normals.width] = oldNormalColor1;
                }

                // Noise
                if (noiseValues[x, y, 2] > 0)
                {
                    noiseColors[x + y * noise.width] = new Color(noiseValues[x, y, 0] / noiseValues[x, y, 2], noiseValues[x, y, 1] / noiseValues[x, y, 2], 0, 1);
                } else
                {
                    noiseColors[x + y * noise.width] = Color.clear;
                }
            }
        }
        heightmap.SetPixels(0, 0, normals.width, normals.height, heightColors);
        normals.SetPixels(0, 0, normals.width, normals.height, normalColors);
        noise.SetPixels(0, 0, normals.width, normals.height, noiseColors);
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
