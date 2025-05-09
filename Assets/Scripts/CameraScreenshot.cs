﻿using UnityEngine;

public class CameraScreenshot : MonoBehaviour
{
    public int width = 1024;
    public int height = 1024;

    public bool getHeightFromCamera = false;

    public string name = "Screenshot";
    
    public void screenshot()
    {
        Camera cam = this.GetComponent<Camera>();
        
        int _height = this.height;
        if (getHeightFromCamera)
        {
            _height = Mathf.RoundToInt(width / cam.aspect);
        }

        RenderTexture rt = new RenderTexture(width, _height, 0, RenderTextureFormat.Default);
        rt.Create();

        RenderTexture target = cam.targetTexture;

        cam.targetTexture = rt;
        cam.Render();

        // Now you can read it back to a Texture2D and save it
        RenderTexture.active = rt;
        Texture2D tex2D = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, true);
        tex2D.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0, false);
        tex2D.Apply();
        RenderTexture.active = null;
        System.IO.File.WriteAllBytes(Application.dataPath + "/Images/" + name + ".png", tex2D.EncodeToPNG());
        Debug.Log("Wrote image to " + Application.dataPath + "/Images/" + name + ".png");

        cam.targetTexture = target;
        
        rt.Release();
    }
}
