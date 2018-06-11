using UnityEngine;
using System.Collections.Generic;

// author: mvaganov@hotmail.com
// license: Copyfree, public domain. This is free code! Great artists, steal this code!
// latest version at: https://pastebin.com/raw/8m69iTut -- added MakeBox (2018/03/02)
namespace NS {
public class Lines : MonoBehaviour {
	[Tooltip("Used to draw lines. Ideally a white Sprites/Default shader.")]
	public Material lineMaterial;
	public bool autoParentLinesToGlobalObject = true;

	/// <summary>The singleton instance.</summary>
	static Lines instance;
	public static Lines Instance() {
		if(instance == null) {
			instance = FindComponentInstance<Lines>();
		}
		return instance;
	}
	public static T FindComponentInstance<T>() where T : Component {
		T instance = null;
		if((instance = FindObjectOfType(typeof(T)) as T) == null) {
			GameObject g = new GameObject("<" + typeof(T).Name + ">");
			instance = g.AddComponent<T>();
		}
		return instance;
	}

	void Start() {
		if (instance != null && instance != this) {
			Debug.LogWarning ("<Lines> should be a singleton. Deleting extra");
			Destroy (this);
		}
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
		if (lineObject == null) { lineObject = new GameObject(); }
		if (Instance().autoParentLinesToGlobalObject) { lineObject.transform.SetParent (instance.transform); }
		LineRenderer lr = lineObject.GetComponent<LineRenderer>();
		if(lr == null) { lr = lineObject.AddComponent<LineRenderer>(); }
		lr.startWidth = startSize;
		lr.endWidth = endSize;
		lr.positionCount = 2;
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
		if (lineObject == null) { lineObject = new GameObject(); }
		if (Instance().autoParentLinesToGlobalObject) { lineObject.transform.SetParent (instance.transform); }
		LineRenderer lr = lineObject.GetComponent<LineRenderer>();
		if(lr == null) { lr = lineObject.AddComponent<LineRenderer>(); }
		lr.startWidth = startSize;
		lr.endWidth = endSize;
		lr.positionCount = pointCount;
		for(int i = 0; i < pointCount; ++i) { lr.SetPosition(i, points[i]); }
		SetColor(lr, color);
		return lr;
	}

	public static Material FindShaderMaterial(string shadername){
		Shader s = Shader.Find(shadername);
		if(s == null) {
			throw new System.Exception("Missing shader: " + shadername
				+ ". Please make sure it is in the \"Resources\" folder, "
				+ "or used by at least one other object. Or, create an "
				+ " object with Lines, and assign the material manually");
		}
		return new Material(s);
	}

	public static void SetColor(LineRenderer lr, Color color) {
		Material mat = Instance().lineMaterial;
		if(mat == null) {
			const string colorShaderName = "Sprites/Default";//"Unlit/Color";
			mat = FindShaderMaterial(colorShaderName);
			Instance ().lineMaterial = mat;
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

	public static LineRenderer MakeLineOnSphere(ref GameObject lineObj, Vector3 sphereCenter, Vector3 start, Vector3 end, 
		Color color=default(Color), float startSize=0.125f, float endSize=0.125f, int pointCount = 24) {
		Vector3[] points = null;
		WriteArcOnSphere(ref points, pointCount, sphereCenter, start, end);
		return Make(ref lineObj, points, pointCount, color, startSize, endSize);
	}
	public static void WriteArcOnSphere(ref Vector3[] points, int pointCount, Vector3 sphereCenter, Vector3 start, Vector3 end) {
		Vector3 axis;
		if(start == -end) {
			axis = (start != Vector3.up && end != Vector3.up)? Vector3.up : Vector3.right;
		} else {
			axis = Vector3.Cross(start, end).normalized;
		}
		Vector3 a = start - sphereCenter, b = end - sphereCenter;
		float arad = a.magnitude, brad = b.magnitude;
		a /= arad; b /= brad;
		float angle = Vector3.Angle(a,b);
		WriteArc(ref points, pointCount, axis, a, angle, Vector3.zero);
		float raddelta = brad-arad;
		for(int i=0; i < points.Length; ++i) {
			points[i] = points[i] * ((i * raddelta / points.Length) + arad);
			points[i] += sphereCenter;
		}
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
		LineRenderer lr = Lines.MakeArc(ref lineObj, 360, 24, normal, r * radius, center, color,
			linesize, linesize);
		lr.loop = true;
		return lr;
	}
	/// <summary>As MakeCircle, but using an assured GameObject</summary>
	public static LineRenderer MakeCircle_With(GameObject lineObj,
		Vector3 center, Vector3 normal, Color color = default(Color), float radius = 1, float linesize = 0.125f) {
		return MakeCircle (ref lineObj, center, normal, color, radius, linesize);
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
	public static LineRenderer MakeBox(ref GameObject lineObj, Vector3 center, 
		Vector3 size, Quaternion rotation, Color color = default(Color), float linesize = 0.125f) {
		Vector3 u = Vector3.up / 2 * size.y;
		Vector3 r = Vector3.right / 2 * size.x;
		Vector3 f = Vector3.forward / 2 * size.z;
		Vector3[] line = new Vector3[] { 
			f+u-r,
			-f+u-r,
			-f-u-r,
			-f-u+r,
			-f+u+r,
			f+u+r,
			f-u+r,
			f-u-r,
			f+u-r,
			f+u+r,
			f-u+r,
			-f-u+r,
			-f+u+r,
			-f+u-r,
			-f-u-r,
			f-u-r
		};
		for (int i = 0; i < line.Length; ++i) { line [i] = rotation * line [i] + center; }
		return Make (ref lineObj, line, line.Length, color, linesize, linesize);
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
	public static Vector3[] CreateSpiralSphere(Vector3 center = default(Vector3), float radius = 1,
		Vector3 axis = default(Vector3), Vector3 axisFace = default(Vector3), float sides = 12, float rotations = 6) {
		List<Vector3> points = new List<Vector3>(); // List instead of Array because sides and rotations are floats!
		if(axis == Vector3.zero) { axis=Vector3.up; }
		if(axisFace == Vector3.zero) { axisFace=Vector3.right; }
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

	public const float ARROWSIZE = 3;
	public static LineRenderer MakeArrow(ref GameObject lineObject, Vector3 start, Vector3 end,
		Color color = default(Color), float startSize = 0.125f, float endSize = 0.125f, float arrowHeadSize = ARROWSIZE) {
		return MakeArrow(ref lineObject, new Vector3[] { start, end }, 2, color, startSize, endSize, arrowHeadSize);
	}
	public static LineRenderer MakeArrowBothEnds(ref GameObject lineObject, Vector3 start, Vector3 end,
		Color color = default(Color), float startSize = 0.125f, float endSize = 0.125f, float arrowHeadSize = ARROWSIZE) {
		return MakeArrowBothEnds(ref lineObject, new Vector3[] { start, end }, 2, color, startSize, endSize, arrowHeadSize);
	}
	public static LineRenderer MakeArrow(ref GameObject lineObject, Vector3[] points, int pointCount,
		Color color = default(Color), float startSize = 0.125f, float endSize = 0.125f, float arrowHeadSize = ARROWSIZE, Keyframe[] lineKeyFrames = null) {
		float arrowSize = endSize*arrowHeadSize;
		int lastGoodIndex = 0;
		Vector3 arrowheadBase = Vector3.zero, arrowheadWidest = Vector3.zero, delta, dir = Vector3.zero;
		float dist = 0, backtracked = 0, extraFromLastGoodIndex = 0;
		for(int i = points.Length-1; i>0; --i) {
			float d = Vector3.Distance(points[i], points[i-1]);
			dist += d;
			backtracked += d;
			if(backtracked >= arrowSize && dir == Vector3.zero) {
				lastGoodIndex = i-1;
				delta = points[i] - points[i-1];
				dir = delta.normalized;
				extraFromLastGoodIndex = backtracked - arrowSize;
				arrowheadBase = points[lastGoodIndex] + dir * extraFromLastGoodIndex;
			}
		}
		if(dist <= arrowSize) { return Make(ref lineObject, points[0], points[points.Length-1], color, arrowSize, 0); }
		delta = points[points.Length-1] - arrowheadBase;
		dir = delta.normalized;
		const float factionalArrowheadExpanseDelta = 1.0f/512;
		arrowheadWidest = arrowheadBase + dir * (dist*factionalArrowheadExpanseDelta);
		Vector3[] line = new Vector3[lastGoodIndex+4];
		for(int i = 0; i<=lastGoodIndex; i++) {
			line[i] = points[i];
		}
		line[lastGoodIndex+3] = points[points.Length-1];
		line[lastGoodIndex+2] = arrowheadWidest;
		line[lastGoodIndex+1] = arrowheadBase;
		LineRenderer lr;
		Keyframe[] keyframes;
		float arrowHeadBaseStart = 1 - arrowSize/dist;
		float arrowHeadBaseWidest = 1 - (arrowSize/dist-factionalArrowheadExpanseDelta);
		if(lineKeyFrames == null) {
			keyframes= new Keyframe[] {
				new Keyframe(0, startSize), new Keyframe(arrowHeadBaseStart, endSize),
				new Keyframe(arrowHeadBaseWidest, arrowSize), new Keyframe(1, 0)
			};
		} else {
			// count how many there are after arrowHeadBaseStart.
			float t = 0;
			int validCount = lineKeyFrames.Length;
			for(int i = 0; i < lineKeyFrames.Length; ++i) {
				t = lineKeyFrames[i].time;
				if(t > arrowHeadBaseStart) { validCount = i; break; }
			}
			// those are irrelivant now. they'll be replaced by the 3 extra points
			keyframes = new Keyframe[validCount+3];
			for(int i=0;i<validCount; ++i) { keyframes[i] = lineKeyFrames[i]; }
			keyframes[validCount+0] = new Keyframe(arrowHeadBaseStart, endSize);
			keyframes[validCount+1] = new Keyframe(arrowHeadBaseWidest, arrowSize);
			keyframes[validCount+2] = new Keyframe(1, 0);
		}
		lr = Make(ref lineObject, line, line.Length, color, startSize, endSize);
		lr.widthCurve = new AnimationCurve(keyframes);
		return lr;
	}
	public static LineRenderer MakeArrowBothEnds(ref GameObject lineObject, Vector3[] points, int pointCount,
		Color color = default(Color), float startSize = 0.125f, float endSize = 0.125f, float arrowHeadSize = ARROWSIZE) {
		LineRenderer lr = MakeArrow(ref lineObject, points, pointCount, color, startSize, endSize, arrowHeadSize, null);
		ReverseLineInternal(ref lr);
		Vector3[] p = new Vector3[lr.positionCount];
		lr.GetPositions(p);
		lr = MakeArrow(ref lineObject, p, p.Length, color, endSize, startSize, arrowHeadSize, lr.widthCurve.keys);
		ReverseLineInternal(ref lr);
		return lr;
	}
	public static LineRenderer ReverseLineInternal(ref LineRenderer lr) {
		Vector3[] p = new Vector3[lr.positionCount];
		lr.GetPositions(p);
		System.Array.Reverse(p);
		lr.SetPositions(p);
		if(lr.widthCurve != null && lr.widthCurve.length > 1) {
			Keyframe[] kf = new Keyframe[lr.widthCurve.keys.Length];
			Keyframe[] okf = lr.widthCurve.keys;
			for(int i = 0; i<kf.Length; ++i) { kf[i]=okf[i]; }
			System.Array.Reverse(kf);
			for(int i = 0; i<kf.Length; ++i) { kf[i].time = 1-kf[i].time; }
			lr.widthCurve = new AnimationCurve(kf);
		}
		return lr;
	}

	public static LineRenderer MakeArcArrow(ref GameObject lineObj,
		float angle, int pointCount, Vector3 arcPlaneNormal = default(Vector3), Vector3 firstPoint = default(Vector3),
		Vector3 center = default(Vector3), Color color = default(Color), float startSize = 0.125f, float endSize = 0.125f, float arrowHeadSize = ARROWSIZE) {
		if(arcPlaneNormal == default(Vector3)) { arcPlaneNormal = Vector3.up; }
		if(center == default(Vector3) && firstPoint == default(Vector3)) { firstPoint = Vector3.right; }
		Vector3[] points = null;
		WriteArc(ref points, pointCount, arcPlaneNormal, firstPoint, angle, center);
		return MakeArrow(ref lineObj, points, pointCount, color, startSize, endSize, arrowHeadSize);
	}

	public static LineRenderer MakeArcArrowBothEnds(ref GameObject lineObj,
		float angle, int pointCount, Vector3 arcPlaneNormal = default(Vector3), Vector3 firstPoint = default(Vector3),
		Vector3 center = default(Vector3), Color color = default(Color), float startSize = 0.125f, float endSize = 0.125f, float arrowHeadSize = ARROWSIZE) {
		LineRenderer lr = MakeArcArrow(ref lineObj, angle, pointCount, arcPlaneNormal, firstPoint, center, color, startSize, endSize, arrowHeadSize);
		ReverseLineInternal(ref lr);
		Vector3[] p = new Vector3[lr.positionCount];
		lr.GetPositions(p);
		lr = MakeArrow(ref lineObj, p, p.Length, color, endSize, startSize, arrowHeadSize, lr.widthCurve.keys);
		ReverseLineInternal(ref lr);
		return lr;
	}

	public static void MakeQuaternion(ref GameObject axisObj, ref GameObject angleObj, Quaternion quaternion, 
		Vector3 position=default(Vector3), Color color=default(Color), Quaternion orientation=default(Quaternion), 
		int arcPoints = 24, float lineSize = 0.125f, float arrowHeadSize = ARROWSIZE, 
		Vector3 startPoint=default(Vector3)) {
		if (orientation == default(Quaternion)) { orientation = Quaternion.identity; }
		float angle;
		Vector3 axis;
		quaternion.ToAngleAxis (out angle, out axis);
		MakeQuaternion (ref axisObj, ref angleObj, axis, angle, 
			position, color, orientation, arcPoints, lineSize, arrowHeadSize, startPoint);
	}
	public static void MakeQuaternion(ref GameObject axisObj, ref GameObject angleObj, Vector3 axis, float angle, 
		Vector3 position=default(Vector3), Color color=default(Color), Quaternion orientation=default(Quaternion), 
		int arcPoints = 24, float lineSize = 0.125f, float arrowHeadSize = ARROWSIZE, Vector3 startPoint=default(Vector3)) {
		if (startPoint == default(Vector3)) {
			float a = Vector3.Angle (axis, Vector3.up);
			if (a < 45 || a > 315) { // if the quaternion axis is too vertical
				startPoint = Vector3.forward; // start from forward
			} else {
				startPoint = Vector3.up; // otherwise start from top
			}
			startPoint = orientation * startPoint;
			// find the closest point to the starPoint on the arc-circle
			Vector3 forwardVector = Vector3.Cross(axis, startPoint);
			forwardVector.Normalize ();
			Vector3 turnedStartPoint = Vector3.Cross (forwardVector, axis);
			turnedStartPoint.Normalize ();
			startPoint = turnedStartPoint;
			angle *= -1;
		}
		while (angle > 180) { angle -= 360; }
		while (angle < -180) { angle += 360; }
		Vector3 axisRotated = orientation * axis;
		Lines.MakeArrow (ref axisObj, position - axisRotated/2, position + axisRotated/2, color, lineSize, lineSize, arrowHeadSize);
		Lines.MakeArcArrow (ref angleObj, angle, arcPoints, axisRotated, startPoint, position, color, lineSize, lineSize, arrowHeadSize);
	}
}
}