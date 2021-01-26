using UnityEngine;
using System.Collections;

// Create a texture and fill it with Perlin noise.
// Try varying the xOrg, yOrg and scale values in the inspector
// while in Play mode to see the effect they have on the noise.

public static class Noise
{
    // The number of cycles of the basic noise pattern that are repeated
    // over the width and height of the texture.
    public static void CalcNoise(Texture2D noise, Texture2D noiseSeed, Vector2 from, float scale)
    {
        int n = 4;
        int r = 2;

        Color[] pix = new Color[noise.height * noise.width];
        Color[] seedPix = noiseSeed.GetPixels(0, 0, noise.width, noise.height);
        // For each pixel in the texture...
        float y = 0.0F;

        while (y < noise.height)
        {

            float x = 0.0F;
            while (x < noise.width)
            {
                float xCoord = from.x + x / noise.width * scale;
                float yCoord = from.y + y / noise.height * scale;
                /*
                float sample = Mathf.PerlinNoise(xCoord, yCoord);

                pix[(int)y * noise.width + (int)x] = new Color(sample, sample, sample);
                */

                float noiseValue = 0;

                for(int k = 0; k < n; k++)
                {
                    noiseValue += Mathf.PerlinNoise(Mathf.Pow(r, k) * xCoord, Mathf.Pow(r, k) * yCoord) / (Mathf.Pow(r, k*(1-seedPix[(int)x + (int)y * noise.width].g)));
                }
                noiseValue *= seedPix[(int)x + (int)y * noise.width].r;
                pix[(int)y * noise.width + (int)x] = new Color(noiseValue, noiseValue, noiseValue);

                x++;
            }
            y++;
        }

        // Copy the pixel data to the texture and load it into the GPU.
        noise.SetPixels(pix);
        noise.Apply();
    }

}