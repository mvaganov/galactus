using UnityEngine;
using System.Collections;
using NS;

namespace Spatial {
	public class Triangle : ConcreteArea {
		public Vector3 a, b, c;

		public Triangle(){}
		public Triangle(Vector3 p0, Vector3 p1, Vector3 p2){ a = p0; b = p1; c = p2; }

		public override void Translate (Vector3 delta) { a += delta; b += delta; c += delta; }
		public override void Rotate (Quaternion q) { a = q * a; b = q * b; c = q * c; }
		public override void Scale (Vector3 coefficient) { a.Scale(coefficient); b.Scale(coefficient); c.Scale(coefficient); }

		public Vector3 SurfaceNormal() {
			return Vector3.Cross (a - b, b - c).normalized;
		}

		public Planar GetPlane() {
			return new Planar (a, SurfaceNormal ());
		}

		public bool IsWithinBoundary(Vector3 point) {
			Vector3 sideA = b - a;
			Vector3 sideB = c - b;
			Vector3 sideC = a - c;
			Vector3 normA = Vector3.Cross (Vector3.Cross (sideA, sideB), sideA).normalized;
			Vector3 normB = Vector3.Cross (Vector3.Cross (sideB, sideC), sideB).normalized;
			Vector3 normC = Vector3.Cross (Vector3.Cross (sideC, sideA), sideC).normalized;
			return Planar.IsFacing (point, a, normA) && Planar.IsFacing (point, b, normB) && Planar.IsFacing (point, c, normC);
		}

		public override bool Contains(Vector3 point) {
			return point == a || point == b || point == c || (GetPlane().Contains(point) && IsWithinBoundary(point));
		}

		public override Vector3 GetClosestPointTo(Vector3 point) {
			Vector3 e;
			return GetClosestPointOnSurface (point, out e);
		}

		public override Vector3 GetClosestPointOnSurface(Vector3 point, out Vector3 surfaceNormal) {
			Vector3 sideA = b - a;
			Vector3 sideB = c - b;
			Vector3 sideC = a - c;
			Vector3 normA = Vector3.Cross (Vector3.Cross (sideA, sideB), sideA).normalized;
			Vector3 normB = Vector3.Cross (Vector3.Cross (sideB, sideC), sideB).normalized;
			Vector3 normC = Vector3.Cross (Vector3.Cross (sideC, sideA), sideC).normalized;
			bool isInsideA = Planar.IsFacing (point, a, normA);
			bool isInsideB = Planar.IsFacing (point, b, normB);
			bool isInsideC = Planar.IsFacing (point, c, normC);
			if (isInsideA && isInsideB && isInsideC) {
				return Planar.GetClosestPointOnSurface (point, out surfaceNormal, a, SurfaceNormal ());
			}
			if (!isInsideC && !isInsideA) { surfaceNormal = (point - a).normalized; return a; }
			if (!isInsideA && !isInsideB) { surfaceNormal = (point - b).normalized; return b; }
			if (!isInsideB && !isInsideC) { surfaceNormal = (point - c).normalized; return c; }
			Vector3 p = Vector3.zero;
			if (!isInsideA) { p = Line.GetClosestPointTo (point, a, b); }
			if (!isInsideB) { p = Line.GetClosestPointTo (point, b, c); }
			if (!isInsideC) { p = Line.GetClosestPointTo (point, c, a); }
			surfaceNormal = (point - p).normalized;
			return p;
		}

		public override bool Raycast(Ray r, out RaycastHit hit) {
			Vector3 n = SurfaceNormal ();
			bool hits = Planar.Raycast (r, out hit, a, n);
			if (hits && hit.distance >= 0 && IsWithinBoundary(hit.point)) {
				return true;
			}
			return false;
		}

		public override int Wireframe(Vector3[] out_points) {
			out_points [0] = a;
			out_points [1] = b;
			out_points [2] = c;
			out_points [3] = a;
			return 4;
		}

		public bool CollidesWith(Triangle t) {
			return false; // do sphere check, then planar ray intersect, then check if ray intersect is in sphere, then check if ray is in triangle
		}
		// TODO FIXME
		public override bool CollidesWith(Area area) {
			if(area.GetType() == typeof(Sphere)) {
				// 
			}
			return base.CollidesWith (area);
		}

		// TODO test me
		public Sphere BoundingSphere() {
			Vector3 center, n;
			float r;
			CalculateCircumscription (a,b,c, out center, out r, out n);
			return new Sphere(center, r); // Circumscribed
		}

		public Sphere InscribedSphere() {
			return new Sphere(Vector3.zero, 1); // TODO Circular-inscription
		}

		public override Vector3 GetLocation() { return (a+b+c)/3; } // circum-scribe and circularly inscribed

		/// <summary>
		/// Calculates the circumscription (the circle with these 3 points on it's edge)
		/// </summary>
		/// <param name="tri">tirangle points</param>
		/// <param name="center">where the center will be</param>
		/// <param name="upNormal"></param>
		public static void CalculateCircumscription(Vector3[] tri, out Vector3 center, out float radius,
			out Vector3 upNormal)
		{
			CalculateCircumscription(tri[0], tri[1], tri[2], out center, out radius, out upNormal);
		}

		public static void CalculateCircumscription(Vector3 a, Vector3 b, Vector3 c, out Vector3 center, 
			out float radius, out Vector3 upNormal)
		{
//			GameObject linObj = null;
//			Line lin;
//			Circle circ;
//			lin = new Line(a, b);
//			linObj = null; lin.Outline(ref linObj, Color.grey);
//			lin = new Line(b, c);
//			linObj = null; lin.Outline(ref linObj, Color.grey);
//			lin = new Line(c, a);
//			linObj = null; lin.Outline(ref linObj, Color.grey);
			center = Vector3.zero;
			Vector3 delta0 = b - a;
			Vector3 delta1 = c - b;
			delta0.Normalize();
			delta1.Normalize();
			upNormal = Vector3.Cross(delta0, delta1);
			upNormal.Normalize();
			Vector3 mid0 = (a+b)/2;
			Vector3 mid1 = (b+c)/2;
			Vector3 perp0 = Vector3.Cross(delta0, upNormal).normalized;
			Ray r = new Ray(mid0, -perp0);
//			lin = new Line(mid0, mid0 - perp0*5);
			//Vector3[] linebuffer = new Vector3[10];
//			linObj = null; lin.Outline(ref linObj, Color.cyan); // TODO use this instead of wireframe. maybe rename Wireframe to CalculateWireframe
			Planar p = new Planar(mid1, delta1);
//			linObj = null; p.Outline(ref linObj, Color.blue);
			p.Raycast(r, out radius);
			center = r.GetPoint(radius);
			radius = Vector3.Distance(center, b);
//			circ = new Circle(center, 0.25f, delta1);
//			linObj = null; circ.Outline(ref linObj, Color.red);
//			linObj = null; Lines.MakeArrow(ref linObj, center, center + delta1, Color.magenta);
//			circ = new Circle(center, radius, upNormal);
//			linObj = null; circ.Outline(ref linObj, Color.yellow);
		}
	}
}
