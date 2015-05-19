using UnityEngine;
using System.Collections;

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
	/// Lines.Make (ref forwardLine, Color.blue, transform.position,
	///             transform.position + transform.forward, 0.1f, 0);
	/// //This makes a long thin triangle, pointing forward.
	/// </code></para>
	/// </summary>
	/// <param name="lineObject">GameObject host of the LineRenderer</param>
	/// <param name="start">Start, an absolute world-space coordinate</param>
	/// <param name="end">End, an absolute world-space coordinate</param>
	/// <param name="startSize">How wide the line is at the start</param>
	/// <param name="endSize">How wide the line is at the end</param>
	public static LineRenderer Make(ref GameObject lineObject, Color color,
		Vector3 start, Vector3 end, float startSize, float endSize) {
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
	public static LineRenderer Make(ref GameObject lineObject, Color color,
		Vector3[] points, int pointCount, float startSize, float endSize) {
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
	public static void MakeArc(ref Vector3[] points, int pointCount,
		Vector3 normal, Vector3 firstPoint, float angle, Vector3 offset) {
		if(points == null) { points = new Vector3[pointCount]; }
		points[0] = firstPoint;
		Quaternion q = Quaternion.AngleAxis(angle / (pointCount - 1), normal);
		for(int i = 1; i < pointCount; ++i) { points[i] = q * points[i - 1]; }
		if(offset != Vector3.zero)
			for(int i = 0; i < pointCount; ++i) { points[i] += offset; }
	}

	/// <summary>
	/// Make the specified arc line in 3D space. Example usage: <para><code>
	/// /* GameObject turnArc should be a member variable */
	/// Lines.MakeArc(ref turnArc, Color.green, transform.position,
	///     Vector3.Cross(transform.forward, direction), transform.forward,
	/// Vector3.Angle(transform.forward, direction), 10, 0.1f, 0);
	/// // makes a curve showing theturn from transform.forward to direction
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
	public static LineRenderer MakeArc(ref GameObject lineObj, Color color,
		Vector3 center, Vector3 normal, Vector3 firstPoint, float angle,
		int pointCount, float startSize, float endSize) {
		Vector3[] points = null;
		MakeArc(ref points, pointCount, normal, firstPoint, angle, center);
		return Make(ref lineObj, color, points, pointCount, startSize, endSize);
	}

	/// <summary>
	/// Quick! Make a circle! Radius 1, AROUND z axis, line width of .1
	/// </summary>
	/// <returns>The LineRenderer hosting the line</returns>
	/// <param name="lineObject">GameObject host of the LineRenderer</param>
	/// <param name="color">Color of the line</param>
	/// <param name="center">Center of arc</param>
	public static LineRenderer MakeCircle(ref GameObject lineObj, Color color,
		Vector3 center) {
		return Lines.MakeCircle(ref lineObj, color, center,
								Vector3.forward, 1, .1f);
	}

	/// <summary>Makes a circle facing along the z axis.</summary>
	/// <returns>The LineRenderer hosting the line</returns>
	/// <param name="lineObject">GameObject host of the LineRenderer</param>
	/// <param name="color">Color of the line</param>
	/// <param name="center">Absolute world-space 3D coordinate</param>
	/// <param name="radius">Radius.</param>
	/// <param name="linesize">Linesize.</param>
	public static LineRenderer MakeCircle(ref GameObject lineObj, Color color,
		Vector3 center, float radius, float linesize) {
		return Lines.MakeCircle(ref lineObj, color, center,
								Vector3.forward, radius, linesize);
	}

	/// <summary>Makes a circle with a 3D line</summary>
	/// <returns>The LineRenderer hosting the line</returns>
	/// <param name="lineObj">GameObject host of the LineRenderer</param>
	/// <param name="color">Color of the line</param>
	/// <param name="center">Absolute world-space 3D coordinate</param>
	/// <param name="normal">Which way the circle is facing</param>
	/// <param name="radius"></param>
	/// <param name="linesize">The width of the line</param>
	public static LineRenderer MakeCircle(ref GameObject lineObj, Color color,
		Vector3 center, Vector3 normal, float radius, float linesize) {
		Vector3 crossDir = (normal != Vector3.up) ? Vector3.up : Vector3.forward;
		Vector3 r = Vector3.Cross(normal, crossDir).normalized;
		return Lines.MakeArc(ref lineObj, color, center, normal, r * radius,
			360, 24, linesize, linesize);
	}
}