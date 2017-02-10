using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Lines : MonoBehaviour {
	[Tooltip("Used to draw lines. Ideally a white Self-Illumin/Diffuse shader.")]
	public Material lineMaterial;

	/// <summary>The singleton instance.</summary>
	static Lines instance;
	public static Lines Instance() {
		if(instance == null) {
			if((instance = FindObjectOfType(typeof(Lines)) as Lines) == null) {
				GameObject g = new GameObject();
				instance = g.AddComponent<Lines>();
				g.name = "<" + instance.GetType().Name + ">";
			}
		}
		return instance;
	}

	/// <summary>
	/// Make the specified Line.
	/// example usage:
	/// <para><code>
	/// /* GameObject forwardLine should be a member variable */
	/// Lines.Make (ref forwardLine, transform.position,
	///             transform.position + transform.forward, Color.blue, 0.1f, 0);
	/// //This makes a long thin triangle, pointing forward.
	/// </code></para>
	/// </summary>
	/// <param name="lineObject">GameObject host of the LineRenderer</param>
	/// <param name="start">Start, an absolute world-space coordinate</param>
	/// <param name="end">End, an absolute world-space coordinate</param>
	/// <param name="startSize">How wide the line is at the start</param>
	/// <param name="endSize">How wide the line is at the end</param>
	public static LineRenderer Make(ref GameObject lineObject, Vector3 start, Vector3 end,
		Color color = default(Color), float startSize = 0.125f, float endSize = 0.125f) {
		if(lineObject == null) { lineObject = new GameObject(); }
		LineRenderer lr = lineObject.GetComponent<LineRenderer>();
		if(lr == null) { lr = lineObject.AddComponent<LineRenderer>(); }
		lr.SetWidth(startSize, endSize);
		lr.SetVertexCount(2);
		lr.SetPosition(0, start); lr.SetPosition(1, end);
		SetColor(lr, color);
		return lr;
	}

	/// <summary>Make the specified Line from a list of points</summary>
	/// <returns>The LineRenderer hosting the line</returns>
	/// <param name="lineObject">GameObject host of the LineRenderer</param>
	/// <param name="color">Color of the line</param>
	/// <param name="points">List of absolute world-space coordinates</param>
	/// <param name="pointCount">Number of the points used points list</param>
	/// <param name="startSize">How wide the line is at the start</param>
	/// <param name="endSize">How wide the line is at the end</param>
	public static LineRenderer Make(ref GameObject lineObject, Vector3[] points, int pointCount,
		Color color = default(Color), float startSize = 0.125f, float endSize = 0.125f) {
		if(lineObject == null) { lineObject = new GameObject(); }
		LineRenderer lr = lineObject.GetComponent<LineRenderer>();
		if(lr == null) { lr = lineObject.AddComponent<LineRenderer>(); }
		lr.SetWidth(startSize, endSize);
		lr.SetVertexCount(pointCount);
		for(int i = 0; i < pointCount; ++i) { lr.SetPosition(i, points[i]); }
		SetColor(lr, color);
		return lr;
	}

	public static void SetColor(LineRenderer lr, Color color) {
		Material mat = Instance().lineMaterial;
		if(mat == null) {
			const string colorShaderName = "Unlit/Color";
			Shader s = Shader.Find(colorShaderName);
			if(s == null) {
				throw new System.Exception("Missing shader: " + colorShaderName
					+ ". Please make sure it is in the \"Resources\" folder, "
					+ "or used by at least one other object. Or, create an "
					+ " object with Lines, and assign the material manually");
			}
			mat = new Material(s);
			Instance().lineMaterial = mat;
		}
		if(lr.material == null || lr.material.name != mat.name) { lr.material = mat; }
		lr.material.color = color;
	}

	/// <summary>Write 2D arc in 3D space, into given Vector3 array</summary>
	/// <param name="points">Will host the list of coordinates</param>
	/// <param name="pointCount">How many vertices to make &gt; 1</param>
	/// <param name="normal">The surface-normal of the arc's plane</param>
	/// <param name="firstPoint">Arc start, rotate about Vector3.zero</param>
	/// <param name="angle">2D angle. Tip: Vector3.Angle(v1, v2)</param>
	/// <param name="offset">How to translate the arc</param>
	public static void WriteArc(ref Vector3[] points, int pointCount,
		Vector3 normal, Vector3 firstPoint, float angle = 360, Vector3 offset = default(Vector3), int startIndex = 0) {
		if(points == null) { points = new Vector3[pointCount]; }
		points[startIndex] = firstPoint;
		Quaternion q = Quaternion.AngleAxis(angle / (pointCount - 1), normal);
		for(int i = startIndex+1; i < startIndex+pointCount; ++i) { points[i] = q * points[i - 1]; }
		if(offset != Vector3.zero)
			for(int i = startIndex; i < startIndex+pointCount; ++i) { points[i] += offset; }
	}

	/// <summary>
	/// Make the specified arc line in 3D space. Example usage: <para><code>
	/// /* GameObject turnArc should be a member variable */
	/// Lines.MakeArc(ref turnArc, Vector3.Angle(transform.forward, direction), 
	/// 	10, Vector3.Cross(transform.forward, direction), 
	/// 	transform.forward, transform.position, Color.green, 0.1f, 0);
	/// // makes a curve showing the turn from transform.forward to direction
	/// </code></para>
	/// </summary>
	/// <returns>The LineRenderer hosting the line</returns>
	/// <param name="lineObject">GameObject host of the LineRenderer</param>
	/// <param name="color">Color of the line</param>
	/// <param name="center">Center of arc</param>
	/// <param name="normal">surface-normal of arc's plane</param>
	/// <param name="firstPoint">Arc start, rotate about Vector3.zero</param>
	/// <param name="angle">2D angle. Tip: Vector3.Angle(v1, v2)</param>
	/// <param name="pointCount">How many vertices to make &gt; 1</param>
	/// <param name="startSize">How wide the line is at the start</param>
	/// <param name="endSize">How wide the line is at the end</param>
	public static LineRenderer MakeArc(ref GameObject lineObj,
		float angle, int pointCount, Vector3 normal, Vector3 firstPoint,
		Vector3 center = default(Vector3), Color color = default(Color), float startSize = 0.125f, float endSize = 0.125f) {
		Vector3[] points = null;
		WriteArc(ref points, pointCount, normal, firstPoint, angle, center);
		return Make(ref lineObj, points, pointCount, color, startSize, endSize);
	}

	/// <summary>Makes a circle with a 3D line</summary>
	/// <returns>The LineRenderer hosting the line</returns>
	/// <param name="lineObj">GameObject host of the LineRenderer</param>
	/// <param name="color">Color of the line</param>
	/// <param name="center">Absolute world-space 3D coordinate</param>
	/// <param name="normal">Which way the circle is facing</param>
	/// <param name="radius"></param>
	/// <param name="linesize">The width of the line</param>
	public static LineRenderer MakeCircle(ref GameObject lineObj,
		Vector3 center, Vector3 normal, Color color = default(Color), float radius = 1, float linesize = 0.125f) {
		Vector3 crossDir = (normal != Vector3.up) ? Vector3.up : Vector3.forward;
		Vector3 r = Vector3.Cross(normal, crossDir).normalized;
		return Lines.MakeArc(ref lineObj, 360, 24, normal, r * radius, center, color,
			linesize, linesize);
	}

	/// <returns>a line renderer in the shape of a sphere made of 3 circles, for the x.y.z axis</returns>
	/// <param name="lineObj">Line object.</param>
	/// <param name="radius">Radius.</param>
	/// <param name="center">Center.</param>
	/// <param name="color">Color.</param>
	/// <param name="linesize">Linesize.</param>
	public static LineRenderer MakeSphere(ref GameObject lineObj, float radius = 1, 
		Vector3 center = default(Vector3), Color color = default(Color), float linesize = 0.125f) {
		Vector3[] circles = new Vector3[24 * 3];
		Lines.WriteArc (ref circles, 24, Vector3.forward, Vector3.up, 360, center, 24*0);
		Lines.WriteArc (ref circles, 24, Vector3.right,   Vector3.up, 360, center, 24*1);
		Lines.WriteArc (ref circles, 24, Vector3.up, Vector3.forward, 360, center, 24*2);
		if (radius != 1) { for (int i = 0; i < circles.Length; ++i) { circles [i] *= radius; } }
		return Lines.Make (ref lineObj, circles, circles.Length, color, linesize, linesize);
	}

	private static Vector3[] thumbtack_points_base = null;
	/// <summary>Draws a "thumbtack", which shows a visualization for direction and orientation</summary>
	/// <returns>The LineRenderer hosting the thumbtack line. The LineRenderer's transform can be adjusted!</returns>
	/// <param name="lineObj">Line object.</param>
	/// <param name="c">C: color</param>
	/// <param name="size">Size: radius of the thumbtack</param>
	/// <param name="lineWidth">Line width.</param>
	public static LineRenderer MakeThumbtack(ref GameObject lineObj, Color c = default(Color), float size = 1, float lineWidth = 0.1f) {
		const float epsilon = 1/1024.0f;
		if(thumbtack_points_base == null) {
			Vector3 pstn = Vector3.zero, fwrd = Vector3.forward * size, rght = Vector3.right * size, up__ = Vector3.up;
			float startAngle = (360.0f / 4) - (360.0f / 32);
			Vector3 v = Quaternion.AngleAxis(startAngle, up__) * fwrd;
			Lines.WriteArc(ref thumbtack_points_base, 32, up__, v, 360, pstn);
			Vector3 tip = pstn + fwrd * Mathf.Sqrt(2);
			thumbtack_points_base[0] = thumbtack_points_base[thumbtack_points_base.Length - 1];
			int m = (32 * 5 / 8) + 1;
			thumbtack_points_base[m++] = thumbtack_points_base[m] + (tip - thumbtack_points_base[m]) * (1 - epsilon);
			thumbtack_points_base[m++] = tip;
			int n = (32 * 7 / 8) + 1;
			while(n < 32) { thumbtack_points_base[m++] = thumbtack_points_base[n++]; }
			Vector3 side = pstn + rght;
			thumbtack_points_base[m++] = thumbtack_points_base[m] + (side - thumbtack_points_base[m]) * (1 - epsilon);
			thumbtack_points_base[m++] = pstn + rght;
			thumbtack_points_base[m++] = pstn + rght * epsilon;
			thumbtack_points_base[m++] = pstn;
			thumbtack_points_base[m++] = pstn + up__ * size * (1 - epsilon);
			thumbtack_points_base[m++] = pstn + up__ * size;
		}
		LineRenderer lr = Lines.Make(ref lineObj, thumbtack_points_base, thumbtack_points_base.Length, c, lineWidth, lineWidth);
		lr.useWorldSpace = false;
		return lr;
	}

	/// <summary>Draws a "thumbtack", which shows a visualization for direction and orientation</summary>
	/// <returns>The LineRenderer hosting the thumbtack line</returns>
	/// <param name="lineObj">Line object.</param>
	/// <param name="t">t: the transform to attach the thumbtack visualisation to</param>
	/// <param name="c">C: color</param>
	/// <param name="size">Size: radius of the thumbtack</param>
	/// <param name="lineWidth">Line width.</param>
	public static LineRenderer SetThumbtack(ref GameObject lineObj, Transform t, Color c = default(Color), float size = 1, float lineWidth = 0.125f) {
		LineRenderer line_ = MakeThumbtack (ref lineObj, c, size, lineWidth);
		line_.transform.SetParent (t);
		line_.transform.localPosition = Vector3.zero;
		line_.transform.localRotation = Quaternion.identity;
		return line_;
	}

	public static Vector3 GetForwardVector(Quaternion q) {
		return new Vector3 (2 * (q.x * q.z + q.w * q.y),
			2 * (q.y * q.z + q.w * q.x),
			1-2*(q.x * q.x + q.y * q.y));
	}
	public static Vector3 GetUpVector(Quaternion q) {
		return new Vector3 (2 * (q.x * q.y + q.w * q.z),
			1-2*(q.x * q.x + q.z * q.z),
			2 * (q.y * q.z + q.w * q.x));
	}
	public static Vector3 GetRightVector(Quaternion q) {
		return new Vector3 (1-2*(q.y * q.y + q.z * q.z),
			2 * (q.x * q.y + q.w * q.z),
			2 * (q.x * q.z + q.w * q.y));
	}

	/// <example>CreateSpiralSphere(transform.position, 0.5f, transform.up, transform.forward, 16, 8);</example>
	/// <summary>creates a line spiraled onto a sphere</summary>
	/// <param name="center"></param>
	/// <param name="radius"></param>
	/// <param name="axis">example: Vector3.up</param>
	/// <param name="axisFace">example: Vector3.right</param>
	/// <param name="sides"></param>
	/// <param name="rotations"></param>
	/// <returns></returns>
	public static Vector3[] CreateSpiralSphere(Vector3 center, float radius, Vector3 axis, Vector3 axisFace,
		float sides, float rotations) {
		List<Vector3> points = new List<Vector3>(); // List instead of Array because sides and rotations are floats!
		if (sides != 0 && rotations != 0) {
			float iter = 0;
			float increment = 1f / (rotations * sides);
			points.Add(center + axis * radius);
			do {
				iter += increment;
				Quaternion faceTurn = Quaternion.AngleAxis(iter * 360 * rotations, axis);
				Vector3 newFace = faceTurn * axisFace;
				Quaternion q = Quaternion.LookRotation(newFace);
				Vector3 right = GetUpVector(q);
				Vector3 r = right * radius;
				q = Quaternion.AngleAxis(iter * 180, newFace);
				Vector3 newPoint = center + q * r;
				points.Add(newPoint);
			}
			while (iter < 1);
		}
		return points.ToArray();
	}

	/// <returns>a line renderer in the shape of a spiraling sphere, spiraling about the Vector3.up axis</returns>
	/// <param name="lineObj">Line object.</param>
	/// <param name="radius">Radius.</param>
	/// <param name="center">Center.</param>
	/// <param name="color">Color.</param>
	/// <param name="linesize">Linesize.</param>
	public static LineRenderer MakeSpiralSphere(ref GameObject lineObj, float radius = 1, 
		Vector3 center = default(Vector3), Color color = default(Color), float linesize = 0.125f) {
		Vector3[] verts = CreateSpiralSphere (center, radius, Vector3.up, Vector3.right, 24, 3);
		return Make (ref lineObj, verts, verts.Length, color, linesize, linesize);
	}
}