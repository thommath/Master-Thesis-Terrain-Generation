using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    public static class Rasterize
{

    static int resolution = 100;

    static int GetPixelFromXOrY(float xorz, Texture2D heightmap, float zoom)
    {
        // Scale the xy coordinates to the grid
        return Mathf.RoundToInt((heightmap.width / 2f + heightmap.width * (xorz / 128f) * zoom));
    }
    static Vector2Int Vector3ToPixelPos(Vector3 vec, Texture2D heightmap, float zoom)
    {
        return new Vector2Int(GetPixelFromXOrY(vec.x, heightmap, zoom), GetPixelFromXOrY(vec.z, heightmap, zoom));
    }

    public static void rasterizeSplineTriangles(BezierSpline[] splines, Texture2D heightmap, Texture2D restrictions, Texture2D normals, float zoom, int maxHeight, Camera camera, Shader lineShader)
    {

        RenderTexture renderTexture = new RenderTexture(heightmap.width, heightmap.height, 32, RenderTextureFormat.ARGBFloat);
        camera.targetTexture = renderTexture;

        float lineWidthMultiplier = 1f;


        foreach (BezierSpline spline in splines)
        {
            RasterizingSplineData splineData = spline.rasterizingData;

            if (splineData == null)
            {
                splineData = new RasterizingSplineData();
                spline.rasterizingData = splineData;
            }

            Vector3 lastPoint = Vector3.zero;

            
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
            Color[] colorsLineNormals = new Color[(resolution * spline.CurveCount + 1) * 2];
            Color[] colorsLineRestrictions = new Color[(resolution * spline.CurveCount + 1) * 2];

            Color[] colorsGradientRestrictionsLeft = new Color[(resolution * spline.CurveCount + 1) * 2];
            Color[] colorsGradientRestrictionsRight = new Color[(resolution * spline.CurveCount + 1) * 2];

            Color gradientColorStart = new Color(0.5f, 0.5f, 0, 1f);
            Color gradientColorEnd = new Color(1f, 0.0f, 0, 1f);

            // How many lines the spline should be cut into
            for (int n = 0; n <= resolution * spline.CurveCount; n++)
            {

                float distOnSpline = (1f * n) / (resolution);

                Vector3 point = spline.GetPoint(distOnSpline);
                Vector2 perpendicular;
                if (lastPoint == Vector3.zero)
                {
                    Vector3 nextPoint = spline.GetPoint((1f * n + 1) / (resolution));
                    perpendicular = Vector2.Perpendicular(new Vector2(point.x - nextPoint.x, point.z - nextPoint.z)).normalized;
                }
                else
                {
                    perpendicular = Vector2.Perpendicular(new Vector2(lastPoint.x - point.x, lastPoint.z - point.z)).normalized;
                }

                verteciesLine[n * 2] = point + lineWidthMultiplier * 0.5f * spline.lineRadius * new Vector3(perpendicular.x, 0, perpendicular.y).normalized;
                verteciesLine[n * 2 + 1] = point - lineWidthMultiplier * 0.5f * spline.lineRadius * new Vector3(perpendicular.x, 0, perpendicular.y).normalized;
                colorsLineHeight[n * 2] = new Color(point.y / maxHeight, 0, 0, 1);
                colorsLineHeight[n * 2 + 1] = new Color(point.y / maxHeight, 0, 0, 1);
                colorsLineNormals[n * 2] = new Color((1 + perpendicular.x) / 2, (1 + perpendicular.y) / 2, 0.5f);
                colorsLineNormals[n * 2 + 1] = new Color((1 + perpendicular.x) / 2, (1 + perpendicular.y) / 2, 0.5f);
                colorsLineRestrictions[n * 2] = new Color(0, 0, 0, 1);
                colorsLineRestrictions[n * 2 + 1] = new Color(0, 0, 0, 1);

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


                if (spline.rightGradientEnabled)
                {
                    verteciesRight[n * 2] = point + lineWidthMultiplier * 0.5f * spline.lineRadius * new Vector3(perpendicular.x, 0, perpendicular.y).normalized;
                    verteciesRight[n * 2 + 1] = point + lineWidthMultiplier * 0.5f * spline.lineRadius * new Vector3(perpendicular.x, 0, perpendicular.y).normalized + spline.gradientLengthRight * new Vector3(perpendicular.x, 0, perpendicular.y).normalized;
                    colorsRight[n * 2] = new Color(0f, 0f, 0.5f + spline.gradientAngleRight * 0.001f);
                    colorsRight[n * 2 + 1] = new Color(0f, 0f, 0.5f + spline.gradientAngleRight * 0.001f);
                    colorsGradientRestrictionsRight[n * 2] = gradientColorStart;
                    colorsGradientRestrictionsRight[n * 2 + 1] = gradientColorEnd;

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
                if (spline.leftGradientEnabled)
                {
                    verteciesLeft[n * 2] = point - lineWidthMultiplier * 0.5f * spline.lineRadius * new Vector3(perpendicular.x, 0, perpendicular.y).normalized;
                    verteciesLeft[n * 2 + 1] = point - lineWidthMultiplier * 0.5f * spline.lineRadius * new Vector3(perpendicular.x, 0, perpendicular.y).normalized - spline.gradientLengthLeft * new Vector3(perpendicular.x, 0, perpendicular.y).normalized;
                    colorsLeft[n * 2] = new Color(0f, 0f, 0.5f + spline.gradientAngleLeft * 0.001f);
                    colorsLeft[n * 2 + 1] = new Color(0f, 0f, 0.5f + spline.gradientAngleLeft * 0.001f);
                    colorsGradientRestrictionsLeft[n * 2] = gradientColorStart;
                    colorsGradientRestrictionsLeft[n * 2 + 1] = gradientColorEnd;

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

                lastPoint = point;
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

            splineData.colors = new Color[][] { colorsLineHeight, colorsLineNormals, colorsLineRestrictions };
            splineData.colorsGradientRestrictionsLeft = colorsGradientRestrictionsLeft;
            splineData.colorsGradientRestrictionsRight = colorsGradientRestrictionsRight;

            GameObject goR = splineData.goR;
            goR.layer = 8;
            goR.transform.parent = camera.gameObject.transform;

            MeshFilter mfR;
            if (!goR.TryGetComponent(out mfR))
            {
                mfR = goR.AddComponent<MeshFilter>();
            }
            mfR.mesh = meshRight;
            MeshRenderer mrR;
            if (!goR.TryGetComponent(out mrR))
            {
                mrR = goR.AddComponent<MeshRenderer>();
            }
            mrR.material = new Material(Shader.Find("Sprites/Default"));


            GameObject goL = splineData.goL;
            goL.layer = 8;
            goL.transform.parent = camera.gameObject.transform;

            MeshFilter mfL;
            if (!goL.TryGetComponent(out mfL))
            {
                mfL = goL.AddComponent<MeshFilter>();
            }
            MeshRenderer mrL;
            if (!goL.TryGetComponent(out mrL))
            {
                mrL = goL.AddComponent<MeshRenderer>();
            }
            mfL.mesh = meshLeft;
            mrL.material = new Material(Shader.Find("Sprites/Default"));


            GameObject go = splineData.go;
            go.layer = 10;
            go.transform.parent = camera.gameObject.transform;
            MeshFilter mf;
            if (!go.TryGetComponent(out mf))
            {
                mf = go.AddComponent<MeshFilter>();
            } 
            MeshRenderer mr;
            if (!go.TryGetComponent(out mr))
            {
                mr = go.AddComponent<MeshRenderer>();
            }
            mf.mesh = meshLine;
            mr.material = new Material(lineShader);

        }

        // For each size

        // Take one image of the meshes
        // Take one image of the lines
        // Convert both to texture 2d

        // Iterate over each pixel in the meshes image
        // On every not-0 pixel add color to normal and restrictions

        // Iterate over each pixel in the lines image
        // On every not-0 pixel add color to heightmap, normal and restrictions

        // Then apply and return the results
        camera.cullingMask = 1 << 9;
        camera.backgroundColor = new Color(0, 0, 0, 0);
        camera.Render();
        saveImage("rasterized_lines_" + heightmap.width, renderTexture);

        RenderTexture.active = renderTexture;
        heightmap.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0, false);



        foreach( BezierSpline spline in splines)
        {
            RasterizingSplineData data = spline.rasterizingData;
            data.meshLine.colors = data.colors[1];
            data.meshLine.MarkModified();
        }
        camera.cullingMask = 1 << 8 | 1 << 9;
        camera.backgroundColor = new Color(0, 0, 0, 0);
        camera.Render();
        saveImage("rasterized_gradients_" + heightmap.width, renderTexture);

        RenderTexture.active = renderTexture;
        normals.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0, false);
        RenderTexture.active = null;

        /*
         * If it acts up, this can be enabled to remove miscolors
        for (int x = 0; x < normals.width; x++)
        {
            for (int y = 0; y < normals.height; y++)
            {
                Color c = normals.GetPixel(x, y);
                if (0.3f > c.b || c.b > 0.7f)
                {
                    normals.SetPixel(x, y, new Color(0.0f, 0.0f, 0f, 0f));
                }
            }
        }
        */

        foreach (BezierSpline spline in splines)
        {
            RasterizingSplineData data = spline.rasterizingData;
            data.meshLine.colors = data.colors[2];
            data.meshLeft.colors = data.colorsGradientRestrictionsLeft;
            data.meshRight.colors = data.colorsGradientRestrictionsRight;
            data.meshRight.MarkModified();
            data.meshLine.MarkModified();
            data.meshLeft.MarkModified();
        }
        camera.cullingMask = 1 << 8 | 1 << 9;
        camera.backgroundColor = new Color(1, 0, 0, 0);
        camera.Render();
        saveImage("rasterized_restrictions_" + heightmap.width, renderTexture);

        RenderTexture.active = renderTexture;
        restrictions.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0, false);
        RenderTexture.active = null;

        for(int x = 0; x < restrictions.width; x++)
        {
            for(int y = 0; y < restrictions.height; y++)
            {
                Color c = restrictions.GetPixel(x, y);
                if (c.a == 1)
                {
                    Vector2 v = new Vector2(c.r, c.g).normalized;
                    restrictions.SetPixel(x, y, new Color(v.x* v.x, v.y* v.y, 0, 1f));
                }
            }
        }
        Graphics.Blit(restrictions, renderTexture);
        saveImage("rasterized_restrictions_" + heightmap.width, renderTexture);



        heightmap.Apply();
        normals.Apply();
        restrictions.Apply();


        /*foreach (Transform child in camera.gameObject.transform)
        {
            Object.Destroy(child.gameObject);
        }*/
    }


    public static void rasterizeSplineLines(BezierSpline[] splines, Texture2D heightmap, Texture2D restrictions, Texture2D normals, float zoom, int maxHeight)
    {

        float[,,] normalValues = new float[normals.width, normals.height, 4];
        float[,,] heightValues = new float[normals.width, normals.height, 2];

        foreach (BezierSpline spline in splines)
        {
            Vector3 lastPoint = Vector3.zero;
            // How many lines the spline should be cut into
            for (int n = 0; n < resolution; n++)
            {
                float distOnSpline = (1f * n) / resolution;
                Vector3 point = spline.GetPoint(distOnSpline * spline.CurveCount);

                if (lastPoint != Vector3.zero)
                {
                    Vector2 perpendicular = Vector2.Perpendicular(new Vector2(lastPoint.x - point.x, lastPoint.z - point.z)).normalized;

                    Vector2Int fromPixel = Vector3ToPixelPos(lastPoint, heightmap, zoom);
                    Vector2Int toPixel = Vector3ToPixelPos(point, heightmap, zoom);
                    float distBetweenPoints = Vector2Int.Distance(fromPixel, toPixel);

                    if (distBetweenPoints == 0)
                    {
                        continue;
                    }

                    foreach (Vector2Int pixel in GetPixelsOfLine(fromPixel.x, fromPixel.y, toPixel.x, toPixel.y))
                    {
                        float distOnLine = Vector2Int.Distance(fromPixel, pixel);
                        float interpolatedHeight = lastPoint.y + (point.y - lastPoint.y) * (distOnLine / distBetweenPoints);

                        heightValues[pixel.x, pixel.y, 0] += interpolatedHeight / maxHeight;
                        heightValues[pixel.x, pixel.y, 1] += 1;

                        restrictions.SetPixel(
                            pixel.x, pixel.y,
                            new Color(0, 0, 0, 1));


                        // Normal vectors
                        Vector2Int movedPixel1 = Vector2Int.CeilToInt(pixel + perpendicular * 2);
                        if (movedPixel1.x < normals.width && movedPixel1.x > 0 && movedPixel1.y < normals.height && movedPixel1.y > 0)
                        {
                            Color oldNormalColor1 = normals.GetPixel(movedPixel1.x, movedPixel1.y);
                            normalValues[movedPixel1.x, movedPixel1.y, 0] += (1 + perpendicular.x) / 2;
                            normalValues[movedPixel1.x, movedPixel1.y, 1] += (1 + perpendicular.y) / 2;
                            normalValues[movedPixel1.x, movedPixel1.y, 2] += oldNormalColor1.b;
                            normalValues[movedPixel1.x, movedPixel1.y, 3] += 1;
                        }

                        Vector2Int movedPixel2 = Vector2Int.CeilToInt(pixel - perpendicular * 2);
                        if (movedPixel2.x < normals.width && movedPixel2.x > 0 && movedPixel2.y < normals.height && movedPixel2.y > 0) 
                        {
                            Color oldNormalColor2 = normals.GetPixel(movedPixel2.x, movedPixel2.y);
                            normalValues[movedPixel2.x, movedPixel2.y, 0] += (1 - perpendicular.x) / 2;
                            normalValues[movedPixel2.x, movedPixel2.y, 1] += (1 - perpendicular.y) / 2;
                            normalValues[movedPixel2.x, movedPixel2.y, 2] += oldNormalColor2.b;
                            normalValues[movedPixel2.x, movedPixel2.y, 3] += 1;
                        }

                    }
                }

                lastPoint = point;
            }
        }
        Color[] heightColors = new Color[normals.width * normals.height];
        Color defaultHeightmap = new Color(0, 0, 0, 0);
        for(int x = 0; x < normals.width; x++)
        {
            for (int y = 0; y < normals.height; y++)
            {
                if (heightValues[x, y, 1] > 0)
                {
                    heightColors[x + y*normals.width] = new Color(heightValues[x, y, 0] / heightValues[x, y, 1], 0, 0, 1);
                } else
                {
                    heightColors[x + y * normals.width] = defaultHeightmap;
                }
            }
        }
        heightmap.SetPixels(0, 0, normals.width, normals.height, heightColors);

        Color[] normalColors = new Color[normals.width * normals.height];
        for (int x = 0; x < normals.width; x++)
        {
            for (int y = 0; y < normals.height; y++)
            {
                if (normalValues[x, y, 3] > 0)
                {
                    normalColors[x + y * normals.width] = new Color(normalValues[x, y, 0] / normalValues[x, y, 3], normalValues[x, y, 1] / normalValues[x, y, 3], normalValues[x, y, 2] / normalValues[x, y, 3], 1);
                }
                else
                {
                    Color oldNormalColor1 = normals.GetPixel(x,y);
                    normalColors[x + y * normals.width] = oldNormalColor1;
                }
            }
        }
        normals.SetPixels(0, 0, normals.width, normals.height, normalColors);


        heightmap.Apply();
        normals.Apply();
        restrictions.Apply();
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
