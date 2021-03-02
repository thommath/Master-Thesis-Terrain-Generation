using System;
using System.Collections.Generic;
using UnityEngine;

class TriangleRenderer
{

    private static float edgeFunction(Vector2 a, Vector2 b, Vector2 c)
    {
        return (c.x - a.x) * (b.y - a.y) - (c.y - a.y) * (b.x - a.x);
    }

    public static void RasterizeTriangle(Color[] image, int width, int height, Vector2 a, Vector2 b, Vector2 c, Color ca, Color cb, Color cc)
    {

        int minx = Mathf.FloorToInt(Math.Max(0, Math.Min(a.x, Math.Min(b.x, c.x))));
        int miny = Mathf.FloorToInt(Math.Max(0, Math.Min(a.y, Math.Min(b.y, c.y))));
        int maxx = Mathf.CeilToInt(Math.Min(width, Math.Max(a.x, Math.Max(b.x, c.x))));
        int maxy = Mathf.CeilToInt(Math.Min(height, Math.Max(a.y, Math.Max(b.y, c.y))));

        float area = edgeFunction(a, b, c);

        for(int x = minx; x < maxx; x++)
        {
            for(int y = miny; y < maxy; y++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);

                float w0 = edgeFunction(b, c, p);
                float w1 = edgeFunction(c, a, p);
                float w2 = edgeFunction(a, b, p);

                if (w0 >= 0 && w1 >= 0 && w2 >= 0)
                {
                    w0 /= area;
                    w1 /= area;
                    w2 /= area;

                    image[x + y * width] = new Color(
                        w0 * ca.r + w1 * cb.r + w2 * cc.r,
                        w0 * ca.g + w1 * cb.g + w2 * cc.g,
                        w0 * ca.b + w1 * cb.b + w2 * cc.b,
                        1
                        );
                }
            }
        }
    }
    public static IEnumerable<PixelData> RasterizeTriangle(int width, int height, Vector2 a, Vector2 b, Vector2 c)
    {

        int minx = Mathf.FloorToInt(Math.Max(0, Math.Min(a.x, Math.Min(b.x, c.x))));
        int miny = Mathf.FloorToInt(Math.Max(0, Math.Min(a.y, Math.Min(b.y, c.y))));
        int maxx = Mathf.CeilToInt(Math.Min(width, Math.Max(a.x, Math.Max(b.x, c.x))));
        int maxy = Mathf.CeilToInt(Math.Min(height, Math.Max(a.y, Math.Max(b.y, c.y))));

        for (int x = minx; x < maxx; x++)
        {
            for (int y = miny; y < maxy; y++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);

                Barycentric bary = new Barycentric(a, b, c, p);

                if (bary.IsInside)
                {
                    yield return new PixelData(x, y, bary);
                }
                continue;

            }
        }
    }
}

public class PixelData
{
    public int x;
    public int y;
    public Barycentric colorMixing;

    public PixelData(int x, int y, Barycentric colorMixing)
    {
        this.x = x;
        this.y = y;
        this.colorMixing = colorMixing;
    }

    public Color getColor(Color ca, Color cb, Color cc)
    {
        return new Color(
            colorMixing.u * ca.r + colorMixing.v * cb.r + colorMixing.w * cc.r,
            colorMixing.u * ca.g + colorMixing.v * cb.g + colorMixing.w * cc.g,
            colorMixing.u * ca.b + colorMixing.v * cb.b + colorMixing.w * cc.b,
            1
            );
    }

}