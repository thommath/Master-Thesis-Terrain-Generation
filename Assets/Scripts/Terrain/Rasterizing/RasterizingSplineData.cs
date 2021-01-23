using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RasterizingSplineData
{
    public GameObject go = new GameObject("Line");
    public GameObject goR = new GameObject("Right");
    public GameObject goL = new GameObject("Left");

    public Mesh meshRight = new Mesh();
    public Mesh meshLeft = new Mesh();
    public Mesh meshLine = new Mesh();

    public Color[][] colors;
    public Color[] colorsGradientRestrictionsLeft;
    public Color[] colorsGradientRestrictionsRight;
}
