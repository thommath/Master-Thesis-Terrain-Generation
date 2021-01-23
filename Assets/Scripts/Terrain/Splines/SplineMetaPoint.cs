using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SplineMetaPoint
{
	public float position = 0.5f;

	public Vector3 testPos = Vector3.zero;

	public float lineRadius = 0;

	public float gradientLengthLeft = 1;
	public float gradientAngleLeft = 0;

	public float gradientLengthRight = 1;
	public float gradientAngleRight = 0;

}
