using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RenderBehaviour : MonoBehaviour
{
    public Camera FilmCamera;
    public string FilmName = "ErosionFilm";
    public bool Film = false;
    [Range(0.0001f, 0.1f)]
    public float FrameFrequency = 0.01f;

    public int width = 1920;
    public int height = 1080;

    float lastFrame = 0f;
    RenderTexture rt;
    Texture2D tex2D;

    int frameCounter = 0;

    public void initiateFilm()
    {
        frameCounter = 0;
        lastFrame = 0f;

        if (rt != null)
        {
            rt.Release();
        }
        rt = new RenderTexture(width, height, 8, RenderTextureFormat.ARGB32);
        rt.Create();
        FilmCamera.targetTexture = rt;

        tex2D = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);

        if (!System.IO.Directory.Exists(Application.streamingAssetsPath))
        {
            System.IO.Directory.CreateDirectory(Application.streamingAssetsPath);
        }
        if (!System.IO.Directory.Exists(Application.streamingAssetsPath + "/Film"))
        {
            System.IO.Directory.CreateDirectory(Application.streamingAssetsPath + "/Film");
        }
        if (!System.IO.Directory.Exists(Application.streamingAssetsPath + "/Film/"+ FilmName))
        {
            System.IO.Directory.CreateDirectory(Application.streamingAssetsPath + "/Film/"+ FilmName);
        }
    }

    public void filmStep(float time)
    {

        this.GetComponent<TerrainVisualizer>().fastExport();

        if (Film && time - lastFrame > FrameFrequency)
        {
            lastFrame = time;
            frameCounter += 1;

            // Render image
            FilmCamera.Render();

            // Save image
            RenderTexture.active = rt;
            tex2D.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false);
            tex2D.Apply();
            RenderTexture.active = null;


            System.IO.File.WriteAllBytes(Application.streamingAssetsPath + "/Film/" + FilmName + "/frame-" + System.String.Format("{0:00000}", frameCounter )+ ".png", tex2D.EncodeToPNG());
        }
    }
}
