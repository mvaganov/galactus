using UnityEngine;
using System.Collections;
// TODO test me next!
namespace Spatial {
	public class Circle : ConcreteArea {
		public Vector3 center;
		public Vector3 normal;
		public float radius;

        public override string ToString()
        {
            return "Spatial.Circle{center:"+center+",normal:"+normal+",radius:"+radius+"}";
        }

        public Circle() { normal = Vector3.up; radius = 0.5f; }
		/// <summary>Initializes a new instance of the <see cref="Spatial.Circle"/> class.</summary>
		/// <param name="c">C. center</param>
		/// <param name="r">R. radius</param>
		/// <param name="n">N. normal</param>
		public Circle(Vector3 c, float r, Vector3 n = default(Vector3) ) { 
			center = c; normal = n; radius = r; if (normal == Vector3.zero) { normal = Vector3.up; }
			if(radius < 0){ Debug.LogWarning("Circle radius "+radius+", is that an error?");}
		}

		public override void FixGeometryProblems() { if (normal == Vector3.zero) { normal = Vector3.up; } normal.Normalize (); }

		public override void Translate (Vector3 delta) { center += delta; }
		public override void Rotate (Quaternion q) { normal = q * normal; }
		public override void Scale (Vector3 coefficient) { 
			center.Scale(coefficient);
			radius *= (coefficient.x+coefficient.y+coefficient.z)/3;
		}

		public override bool Contains(Vector3 p) {
			return Planar.Contains(p, center, normal) && Vector3.Distance(p, center) <= radius;
		}

		/// <returns>The closest point on the circle edge to the given point. If the given point is the center, returns the center</returns>
		/// <param name="point">Point. a reference to look for a point-on-the-circle for</param>
		/// <param name="center">Center. circle center</param>
		/// <param name="normal">Normal. circle normal</param>
		public static Vector3 GetClosestPointOnEdgeTo(Vector3 point, Vector3 center, Vector3 normal, float radius) {
			if (point == center) return center;
			Vector3 p = Planar.GetClosestPointTo (point, center, normal);
			Vector3 d = p - center;
			return d.normalized * radius + center;
		}

		public override Vector3 GetClosestPointTo(Vector3 point) {
			Vector3 p = Planar.GetClosestPointTo (point, center, normal);
			Vector3 d = p - center;
			float dist = d.magnitude;
			if (dist <= radius) {
				return p;
			}
			return d.normalized * radius + center;
		}

		public override Vector3 GetClosestPointOnSurface(Vector3 point, out Vector3 surfaceNormal) {
			Vector3 p = Planar.GetClosestPointTo (point, center, normal);
			Vector3 d = p - center;
			float dist = d.magnitude;
			if (dist <= radius) {
				surfaceNormal = this.normal;
				if(!Planar.IsFacing(point, center, this.normal)) {
					surfaceNormal *= -1;
				}
				return p;
			}
			Vector3 edgeP = d.normalized * radius + center;
			surfaceNormal = (point - edgeP).normalized;
			return edgeP;
		}

		public static Vector3 GetClosestPointOnSurface(Vector3 point, out Vector3 surfaceNormal, Vector3 center, Vector3 normal, float radius) {
			Vector3 p = Planar.GetClosestPointTo (point, center, normal);
			Vector3 d = p - center;
			float dist = d.magnitude;
			if (dist <= radius) {
				surfaceNormal = normal;
				if(!Planar.IsFacing(point, center, normal)) {
					surfaceNormal *= -1;
				}
				return p;
			}
			Vector3 edgeP = d.normalized * radius + center;
			surfaceNormal = (point - edgeP).normalized;
			return edgeP;
		}

		public override bool CollidesWith(Area area) {
			return base.CollidesWith (area);
		}

		public override Vector3 GetLocation() {
			return center;
		}

		public override bool Raycast(Ray r, out RaycastHit hit) {
			// find planar collision
			Planar.Raycast(r, out hit, center, normal);
			// return if its in the radius
			return Vector3.Distance(hit.point, center) <= radius;
		}

		public static bool Raycast(Ray r, out RaycastHit hit, Vector3 center, Vector3 normal, float radius) {
			// find planar collision
			Planar.Raycast(r, out hit, center, normal);
			// return if its in the radius
			return Vector3.Distance(hit.point, center) <= radius;
		}

		public override int Wireframe(Vector3[] out_wireframeVertices)
		{
			return Wireframe(out_wireframeVertices, center, normal, radius, 0);
		}

		public int Wireframe(Vector3[] out_wireframeVertices, int numPoints)
		{
			return Wireframe(out_wireframeVertices, center, normal, radius, numPoints);
		}

		public static int Wireframe (Vector3[] out_wireframeVertices, Vector3 center, Vector3 normal, float radius, 
			int numPoints = 0)
		{
			return NS.Lines.WriteCircle(ref out_wireframeVertices, center, normal, radius, numPoints);
//			Vector3[] points = null;
//			NS.Lines.WriteCircle(ref points, center, normal, radius, numPoints);
//			for (int i = 0; i < points.Length && i < out_wireframeVertices.Length; ++i) {
//				out_wireframeVertices [i] = points [i] * radius + center;
//			}
//			return Mathf.Min (points.Length, out_wireframeVertices.Length);
		}
	}
}