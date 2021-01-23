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
			Vector2 perpendicular = spline.GetPerpendicular(spline.metaPoints[i].position);

			Handles.color = Color.green;
			Handles.DrawLine(p1, p1 + new Vector3(perpendicular.x, 0, perpendicular.y) * spline.metaPoints[i].gradientLengthLeft);
			Handles.DrawLine(p1, p1 - new Vector3(perpendicular.x, 0, perpendicular.y) * spline.metaPoints[i].gradientLengthRight);
		}
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
		Vector3 point = spline.GetPoint(metaPoint.position);

		Vector3 dir = spline.GetVelocity(metaPoint.position);

		Vector2 perpendicular = spline.GetPerpendicular(metaPoint.position);


		Vector3 gradientRightEnd = point - new Vector3(perpendicular.x, 0, perpendicular.y) * metaPoint.gradientLengthRight;
		Vector3 gradientLeftEnd = point + new Vector3(perpendicular.x, 0, perpendicular.y) * metaPoint.gradientLengthLeft;

		float size = HandleUtility.GetHandleSize(point);
		Handles.color = Color.red;
		if (Handles.Button(point, handleRotation, size * handleSize, size * pickSize, Handles.CircleCap))
		{
			selectedIndex = index;
		}
		if (selectedIndex == index)
		{
			EditorGUI.BeginChangeCheck();

			Vector3 leftHandlePos = MetaPointHandle.DragHandle(gradientLeftEnd, perpendicular, size, Color.yellow);

			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(spline, "Move MetaPoint");
				EditorUtility.SetDirty(spline);

				metaPoint.gradientLengthLeft = Vector3.Project(leftHandlePos - point, perpendicular).magnitude;
			}

			EditorGUI.BeginChangeCheck();

			Vector3 rightHandlePos = MetaPointHandle.DragHandle(gradientRightEnd, -perpendicular, size, Color.yellow);


			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(spline, "Move MetaPoint");
				EditorUtility.SetDirty(spline);

				metaPoint.gradientLengthRight = Vector3.Project(rightHandlePos - point, perpendicular).magnitude;
			}
		}
		return point;
	}
}