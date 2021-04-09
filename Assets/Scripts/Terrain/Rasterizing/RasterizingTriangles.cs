using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RasterizingTriangles 
{
    public static RasterizingSplineData getSplineData(BezierSpline spline, int terrainSize, int maxHeight, int resolution)
    {
        Mesh meshRight = new Mesh();
        Mesh meshLeft = new Mesh();

        Vector3[] verteciesRight = new Vector3[(resolution * spline.CurveCount + 1) * 2];
        int[] trianglesRight = new int[(resolution * spline.CurveCount) * 2 * 3];
        Color[] colorsRight = new Color[(resolution * spline.CurveCount + 1) * 2];

        Vector3[] verteciesLeft = new Vector3[(resolution * spline.CurveCount + 1) * 2];
        int[] trianglesLeft = new int[(resolution * spline.CurveCount) * 2 * 3];
        Color[] colorsLeft = new Color[(resolution * spline.CurveCount + 1) * 2];

        Mesh meshLine = new Mesh();
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

                if (spline.elevationConstraint)
                {
                    colorsRight[n * 2] = new Color(0f, 0f, 0.5f - 0.5f * (metaPoint.gradientAngleRight / metaPoint.gradientLengthRight) / maxHeight);
                    colorsRight[n * 2 + 1] = new Color(0f, 0f, 0.5f - 0.5f * (metaPoint.gradientAngleRight / metaPoint.gradientLengthRight) / maxHeight);
                    
                    colorsRight[n * 2] = new Color(0f, 0f, point.y / maxHeight);
                    colorsRight[n * 2 + 1] = new Color(0f, 0f, (point.y + metaPoint.gradientAngleRight) / maxHeight);
                }
                else
                {
                    colorsRight[n * 2] = new Color(0f, 0f, 0.5f + 0.5f * (metaPoint.gradientAngleRight / metaPoint.gradientLengthRight) / maxHeight);
                    colorsRight[n * 2 + 1] = new Color(0f, 0f, 0.5f + 0.5f * (metaPoint.gradientAngleRight / metaPoint.gradientLengthRight) / maxHeight);
                }

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
                if (spline.elevationConstraint)
                {
                    colorsLeft[n * 2] = new Color(0f, 0f, 0.5f - 0.5f * (metaPoint.gradientAngleLeft / metaPoint.gradientLengthLeft) / maxHeight);
                    colorsLeft[n * 2 + 1] = new Color(0f, 0f, 0.5f - 0.5f * (metaPoint.gradientAngleLeft / metaPoint.gradientLengthLeft) / maxHeight);
                    
                    colorsLeft[n * 2] = new Color(0f, 0f, point.y / maxHeight);
                    colorsLeft[n * 2 + 1] = new Color(0f, 0f, (point.y + metaPoint.gradientAngleLeft) / maxHeight);
                }
                else
                {
                    colorsLeft[n * 2] = new Color(0f, 0f, 0.5f + 0.5f * (metaPoint.gradientAngleLeft / metaPoint.gradientLengthLeft) / maxHeight);
                    colorsLeft[n * 2 + 1] = new Color(0f, 0f, 0.5f + 0.5f * (metaPoint.gradientAngleLeft / metaPoint.gradientLengthLeft) / maxHeight);
                }

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

        RasterizingSplineData data = new RasterizingSplineData();
        data.meshLeft = meshLeft;
        data.meshRight = meshRight;
        data.meshLine = meshLine;

        return data;
    }



}
