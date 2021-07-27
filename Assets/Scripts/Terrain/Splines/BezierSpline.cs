using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEditor;

public class BezierSpline : MonoBehaviour {

	public Vector3[] points;

	public bool elevationConstraint = true;
	public bool noiseConstraint = false;
	public bool erosionConstraint = false;
	public bool warpConstraint = false;

	public int minGridLevel = 0;
	public int maxGridLevel = Int32.MaxValue;


	public SplineMetaPoint[] metaPoints;

	public RasterizingSplineData rasterizingData;

	public int CurveCount {
		get {
			return (points.Length - 1) / 3;
		}
	}

	public Vector3 GetPoint (float t) {
		int i;
		if (t >= 1f) {
			t = 1f;
			i = points.Length - 4;
		}
		else {
			t = Mathf.Clamp01(t) * CurveCount;
			i = (int)t;
			t -= i;
			i *= 3;
		}
		
		return transform.TransformPoint(Bezier.GetPoint(points[i], points[i + 1], points[i + 2], points[i + 3], t));
	}

	public Vector3 GetVelocity(float t)
	{
		int i;
		if (t >= 1f)
		{
			t = 1f;
			i = points.Length - 4;
		}
		else
		{
			t = Mathf.Clamp01(t) * CurveCount;
			i = (int)t;
			t -= i;
			i *= 3;
		}
		return transform.TransformPoint(Bezier.GetFirstDerivative(points[i], points[i + 1], points[i + 2], points[i + 3], t)) - transform.position;
	}

	public Vector3 GetDirection(float t)
	{
		return GetVelocity(t).normalized;
	}

	public void AddCurve () {
		Vector3 point = points[points.Length - 1];
		Array.Resize(ref points, points.Length + 3);
		point.x += 100f;
		points[points.Length - 3] = point;
		point.x += 100f;
		points[points.Length - 2] = point;
		point.x += 100f;
		points[points.Length - 1] = point;
	}
	public void RemoveCurve()
	{
		if (points.Length == 4)
		{
			return;
		}
		Array.Resize(ref points, points.Length - 3);
	}

	public void AddMetaPoint ()
	{
		if (metaPoints == null)
		{
			metaPoints = new SplineMetaPoint[0];
		}
		Array.Resize(ref metaPoints, metaPoints.Length + 1);
		metaPoints[metaPoints.Length - 1] = new SplineMetaPoint();
	}

	public void RemoveMetaPoint(int index)
	{
		List<SplineMetaPoint> foos = new List<SplineMetaPoint>(metaPoints);
		foos.RemoveAt(index);
		metaPoints = foos.ToArray();
	}

	public void Reset () {
		points = new Vector3[] {
			new Vector3(-200f, 0f, 0f),
			new Vector3(-100f, 0f, 0f),
			new Vector3(100f, 0f, 0f),
			new Vector3(200f, 0f, 0f)
		};
		metaPoints = new SplineMetaPoint[0];
	}


	private void OnDrawGizmos()
	{
		if (UnityEditor.Selection.activeGameObject == this.gameObject) return;

		Gizmos.color = Color.yellow;
		float points = 20 * this.points.Length;
		for (float n = 0; n < points; n++)
		{
			Gizmos.DrawLine(GetPoint(n / points), GetPoint((n + 1) / points));
		}
	}


	public Vector2 GetPerpendicular(float t)
	{
		Vector3 lastPoint = this.GetPoint(t-0.01f);
		Vector3 point = this.GetPoint(t+0.01f);

		return Vector2.Perpendicular(new Vector2(lastPoint.x - point.x, lastPoint.z - point.z)).normalized;
	}

	public float metaGetTime(int metaIndex)
	{
		return this.metaPoints[metaIndex].getSplineTime(this.CurveCount);
	}

	public List<SplineMetaPoint> getSortedMetaPoints()
	{
		List<SplineMetaPoint> list = new List<SplineMetaPoint>(metaPoints);
		list.Sort((x, y) => x.position.CompareTo(y.position));
		return list;
	}

	public SplineMetaPoint getMetaPointInterpolated(float time)
	{
		if (metaPoints.Length == 0)
		{
			return new SplineMetaPoint();
		}

		SplineMetaPoint last = null;
		foreach(SplineMetaPoint metaPoint in getSortedMetaPoints())
		{
			if (last == null)
			{
				last = metaPoint;
			}

			float metaPointTime = metaPoint.getSplineTime(CurveCount);
			if (time < metaPointTime)
			{
				if (last == metaPoint)
				{
					SplineMetaPoint point = last.clone();
					point.position = time * CurveCount;
					return point;
				}

				float t = (time - last.getSplineTime(CurveCount)) / (metaPointTime - last.getSplineTime(CurveCount));

				return SplineMetaPoint.Lerp(last, metaPoint, t);
			}

			last = metaPoint;
		}
		{
			SplineMetaPoint point = last.clone();
			point.position = time * CurveCount;
			return point;
		}
	}

}