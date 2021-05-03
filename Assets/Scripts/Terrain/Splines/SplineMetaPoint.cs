using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SplineMetaPoint
{
	public float position = 0.5f;

	public float lineRadius = 0.5f;

	public float gradientLengthLeft = 1;
	public float gradientAngleLeft = 0;

	public float gradientLengthRight = 1;
	public float gradientAngleRight = 0;

	[Header("Nosie")]
	[Range(0, 1f)]
	public float noiseAmplitude = 0;
	[Range(0, 1f)]
	public float noiseRoughness = 0;

	[Header("Warp")]
	[Range(0, 1f)]
	public float warpA = 0.5f;
	[Range(0, 1f)]
	public float warpB = 0.5f;

	[Header("Erosion")]
	[Range(0, 1f)]
	public float erosionRain = 1f;
	[Range(0, 1f)]
	public float erosionHardness = 1f;
	[Range(0, 1f)]
	public float sedimentCapacity = 1f;
	


	public SplineMetaPoint clone()
	{
		SplineMetaPoint point = new SplineMetaPoint();

		point.position = this.position;
		point.lineRadius = this.lineRadius;
		point.gradientLengthLeft = this.gradientLengthLeft;
		point.gradientAngleLeft = this.gradientAngleLeft;
		point.gradientLengthRight = this.gradientLengthRight;
		point.gradientAngleRight = this.gradientAngleRight;
		point.noiseAmplitude = this.noiseAmplitude;
		point.noiseRoughness = this.noiseRoughness;

		return point;

	}

	public static SplineMetaPoint Lerp(SplineMetaPoint a, SplineMetaPoint b, float t)
	{
		SplineMetaPoint point = new SplineMetaPoint();

		point.position = Mathf.Lerp(a.position, b.position, t);
		point.lineRadius = Mathf.Lerp(a.lineRadius, b.lineRadius, t);
		point.gradientLengthLeft = Mathf.Lerp(a.gradientLengthLeft, b.gradientLengthLeft, t);
		point.gradientAngleLeft = Mathf.Lerp(a.gradientAngleLeft, b.gradientAngleLeft, t);
		point.gradientLengthRight = Mathf.Lerp(a.gradientLengthRight, b.gradientLengthRight, t);
		point.gradientAngleRight = Mathf.Lerp(a.gradientAngleRight, b.gradientAngleRight, t);
		point.noiseAmplitude = Mathf.Lerp(a.noiseAmplitude, b.noiseAmplitude, t);
		point.noiseRoughness = Mathf.Lerp(a.noiseRoughness, b.noiseRoughness, t);

		return point;
	}

	public float getSplineTime(int curveCount)
	{
		return position / (curveCount);
	}

	public Vector3 getPoint(BezierSpline spline)
	{
		return spline.GetPoint(getSplineTime(spline.CurveCount));
	}

	public Vector3 getPerpendicular3D(BezierSpline spline)
	{
		Vector2 perpendicular = spline.GetPerpendicular(getSplineTime(spline.CurveCount));
		return new Vector3(perpendicular.x, 0, perpendicular.y);
	}

	public Vector3 getGradientLeftEnd(BezierSpline spline)
	{
		return getLineLeftEnd(spline) + getPerpendicular3D(spline) * gradientLengthLeft + gradientAngleLeft * Vector3.up;
	}
	public Vector3 getGradientRightEnd(BezierSpline spline)
	{
		return getLineRightEnd(spline) - getPerpendicular3D(spline) * gradientLengthRight + gradientAngleRight * Vector3.up;
	}
	public Vector3 getLineLeftEnd(BezierSpline spline)
	{
		return getPoint(spline) + getPerpendicular3D(spline) * lineRadius;
	}
	public Vector3 getLineRightEnd(BezierSpline spline)
	{
		return getPoint(spline) - getPerpendicular3D(spline) * lineRadius;
	}

}
