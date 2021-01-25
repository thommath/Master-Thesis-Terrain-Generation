using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BezierSpline))]
public class BezierSplineInspector : Editor {

	private const int stepsPerCurve = 10;
	private const float directionScale = 0.5f;
	private const float handleSize = 0.04f;
	private const float pickSize = 0.06f;

	private BezierSpline spline;
	private Transform handleTransform;
	private Quaternion handleRotation;
	private int selectedIndex = -1;

	private MetaPointHandle metaHandler;

	public override void OnInspectorGUI () {
		DrawDefaultInspector();
		spline = target as BezierSpline;
		if (GUILayout.Button("Add Curve")) {
			Undo.RecordObject(spline, "Add Curve");
			spline.AddCurve();
			EditorUtility.SetDirty(spline);
		}
		if (GUILayout.Button("Add MetaPoint"))
		{
			Undo.RecordObject(spline, "Add MetaPoint");
			spline.AddMetaPoint();
			EditorUtility.SetDirty(spline);
		}
	}

	private void OnSceneGUI () {
		spline = target as BezierSpline;
		handleTransform = spline.transform;
		handleRotation = Tools.pivotRotation == PivotRotation.Local ?
			handleTransform.rotation : Quaternion.identity;

		metaHandler = new MetaPointHandle();

		Vector3 p0 = ShowPoint(0);
		for (int i = 1; i < spline.points.Length; i += 3) {
			Vector3 p1 = ShowPoint(i);
			Vector3 p2 = ShowPoint(i + 1);
			Vector3 p3 = ShowPoint(i + 2);
			
			Handles.color = Color.gray;
			Handles.DrawLine(p0, p1);
			Handles.DrawLine(p2, p3);
			
			Handles.DrawBezier(p0, p3, p1, p2, Color.white, null, 2f);
			p0 = p3;
		}
		ShowDirections();

		if (spline.metaPoints == null)
		{
			spline.metaPoints = new SplineMetaPoint[0];
		}

		for (int i = 0; i < spline.metaPoints.Length; i += 1)
		{
			Vector3 p1 = ShowMetaPoint(spline.points.Length + i, spline.metaPoints[i]);

			// Debug.Log(spline.metaPoints[i].gradientLengthLeft);

			/*
			Vector2 perpendicular = spline.GetPerpendicular(spline.metaGetTime(i));
			Vector3 perendicular3D = new Vector3(perpendicular.x, 0, perpendicular.y);

			Handles.color = Color.green;
			Handles.DrawLine(p1 + perendicular3D * spline.metaPoints[i].lineRadius, p1 + perendicular3D * (spline.metaPoints[i].gradientLengthLeft + spline.metaPoints[i].lineRadius) + spline.metaPoints[i].gradientAngleLeft * Vector3.up);
			Handles.DrawLine(p1 - perendicular3D * spline.metaPoints[i].lineRadius, p1 - perendicular3D * (spline.metaPoints[i].gradientLengthRight + spline.metaPoints[i].lineRadius) + spline.metaPoints[i].gradientAngleRight * Vector3.up);

			Handles.color = Color.blue;
			Handles.DrawLine(p1, p1 + perendicular3D * spline.metaPoints[i].lineRadius);
			Handles.DrawLine(p1, p1 - perendicular3D * spline.metaPoints[i].lineRadius);
			*/
			DrawMetaPoint(spline.metaPoints[i]);
		}
		if (spline.metaPoints.Length > 0)
		{
			DrawMetaPoint(spline.getMetaPointInterpolated(0));
			DrawMetaPoint(spline.getMetaPointInterpolated(1));
		}

		float last = 0;
		float maxStepSize = 0.05f;
		List<SplineMetaPoint> metaPoints = spline.getSortedMetaPoints();
		for (int i = 0; i <= metaPoints.Count; i += 1)
		{
			SplineMetaPoint metaPoint;
			if (metaPoints.Count == i)
			{
				metaPoint = spline.getMetaPointInterpolated(1);
			}
			else
			{
				metaPoint = metaPoints[i];
			}

			while (last < metaPoint.getSplineTime(spline.CurveCount))
			{
				float current = Mathf.Min(metaPoint.getSplineTime(spline.CurveCount), last + maxStepSize);

				SplineMetaPoint startPoint = spline.getMetaPointInterpolated(last);
				SplineMetaPoint endPoint = spline.getMetaPointInterpolated(current);


				Handles.color = Color.blue;
				Handles.DrawLine(startPoint.getLineLeftEnd(spline), endPoint.getLineLeftEnd(spline));
				Handles.DrawLine(startPoint.getLineRightEnd(spline), endPoint.getLineRightEnd(spline));
				Handles.DrawLine(startPoint.getPoint(spline), endPoint.getLineLeftEnd(spline));
				Handles.DrawLine(startPoint.getPoint(spline), endPoint.getLineRightEnd(spline));

				Handles.color = Color.green;
				Handles.DrawLine(startPoint.getGradientLeftEnd(spline), endPoint.getGradientLeftEnd(spline));
				Handles.DrawLine(startPoint.getGradientRightEnd(spline), endPoint.getGradientRightEnd(spline));
				Handles.DrawLine(startPoint.getLineLeftEnd(spline), endPoint.getGradientLeftEnd(spline));
				Handles.DrawLine(startPoint.getLineRightEnd(spline), endPoint.getGradientRightEnd(spline));

				DrawMetaPoint(spline.getMetaPointInterpolated(current));

				last = current;
			}
		}
	}

	private void DrawMetaPoint(SplineMetaPoint metaPoint)
	{
		Handles.color = Color.green;
		Handles.DrawLine(metaPoint.getLineLeftEnd(spline), metaPoint.getGradientLeftEnd(spline));
		Handles.DrawLine(metaPoint.getLineRightEnd(spline), metaPoint.getGradientRightEnd(spline));

		Handles.color = Color.blue;
		Handles.DrawLine(metaPoint.getPoint(spline), metaPoint.getLineLeftEnd(spline));
		Handles.DrawLine(metaPoint.getPoint(spline), metaPoint.getLineRightEnd(spline));
	}

	private void ShowDirections () {
		Handles.color = Color.green;
		Vector3 point = spline.GetPoint(0f);
		Handles.DrawLine(point, point + spline.GetDirection(0f) * directionScale);
		int steps = stepsPerCurve * spline.CurveCount;
		for (int i = 1; i <= steps; i++) {
			point = spline.GetPoint(i / (float)steps);
			Handles.DrawLine(point, point + spline.GetDirection(i / (float)steps) * directionScale);
		}
	}

	private Vector3 ShowPoint (int index) {
		Vector3 point = handleTransform.TransformPoint(spline.points[index]);
		float size = HandleUtility.GetHandleSize(point);
		Handles.color = Color.white;
		if (Handles.Button(point, handleRotation, size * handleSize, size * pickSize, Handles.DotCap)) {
			selectedIndex = index;
		}
		if (selectedIndex == index) {
			EditorGUI.BeginChangeCheck();
			point = Handles.DoPositionHandle(point, handleRotation);
			if (EditorGUI.EndChangeCheck()) {
				Undo.RecordObject(spline, "Move Point");
				EditorUtility.SetDirty(spline);
				spline.points[index] = handleTransform.InverseTransformPoint(point);
			}
		}
		return point;
	}

	private Vector3 ShowMetaPoint(int index, SplineMetaPoint metaPoint)
	{
		Vector3 point = metaPoint.getPoint(spline);
		Vector2 perpendicular = spline.GetPerpendicular(metaPoint.getSplineTime(spline.CurveCount));
		Vector3 perendicular3D = metaPoint.getPerpendicular3D(spline);

		Vector3 lineRightEnd = metaPoint.getLineRightEnd(spline);
		Vector3 lineLeftEnd = metaPoint.getLineLeftEnd(spline);

		Vector3 gradientRightEnd = metaPoint.getGradientRightEnd(spline);
		Vector3 gradientLeftEnd = metaPoint.getGradientLeftEnd(spline);

		float size = HandleUtility.GetHandleSize(point);
		Handles.color = Color.red;
		if (Handles.Button(point, handleRotation, size * handleSize, size * pickSize, Handles.DotCap))
		{
			selectedIndex = index;
		}

		if (selectedIndex == index)
		{
			EditorGUI.BeginChangeCheck();
			Vector3 leftLineHandlePos = MetaPointHandle.DragHandle(lineLeftEnd, perendicular3D, size, Color.blue);
			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(spline, "Move MetaPoint");
				EditorUtility.SetDirty(spline);
				metaPoint.lineRadius = Vector3.Project(leftLineHandlePos - point, perendicular3D).magnitude;
			}


			EditorGUI.BeginChangeCheck();
			Vector3 leftHandlePos = MetaPointHandle.DragHandle(gradientLeftEnd, perendicular3D, size, Color.yellow);
			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(spline, "Move MetaPoint");
				EditorUtility.SetDirty(spline);
				metaPoint.gradientLengthLeft = Vector3.Project(leftHandlePos - lineLeftEnd, perendicular3D).magnitude;
			}

			EditorGUI.BeginChangeCheck();
			Vector3 rightHandlePos = MetaPointHandle.DragHandle(gradientRightEnd, -perendicular3D, size, Color.yellow);
			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(spline, "Move MetaPoint");
				EditorUtility.SetDirty(spline);
				metaPoint.gradientLengthRight = Vector3.Project(rightHandlePos - lineRightEnd, perendicular3D).magnitude;
			}


			EditorGUI.BeginChangeCheck();
			Vector3 leftAngleHandlePos = MetaPointHandle.DragHandle(gradientLeftEnd, Vector3.up, size, Color.yellow);
			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(spline, "Move MetaPoint");
				EditorUtility.SetDirty(spline);
				metaPoint.gradientAngleLeft = (leftAngleHandlePos - point).y;
			}
			EditorGUI.BeginChangeCheck();
			Vector3 rightAngleHandlePos = MetaPointHandle.DragHandle(gradientRightEnd, Vector3.up, size, Color.yellow);
			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(spline, "Move MetaPoint");
				EditorUtility.SetDirty(spline);
				metaPoint.gradientAngleRight = (rightAngleHandlePos - point).y;
			}
		}
		return point;
	}
}