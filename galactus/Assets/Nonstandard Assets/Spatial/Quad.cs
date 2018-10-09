using UnityEngine;
using System.Collections;

namespace Spatial {
	public class Quad : ConcreteArea {
		public Vector3 p0, p1, p2, p3;

		public Quad(){}
		public Quad(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3){ this.p0 = p0; this.p1 = p1; this.p2 = p2; this.p3 = p3; }

		public override void Translate (Vector3 delta) { p0 += delta; p1 += delta; p2 += delta; p3 += delta; }
		public override void Rotate (Quaternion q) { p0 = q * p0; p1 = q * p1; p2 = q * p2; p3 = q * p3; }
		public override void Scale (Vector3 coefficient) { p0.Scale(coefficient); p1.Scale(coefficient); p2.Scale(coefficient); p3.Scale (coefficient); }

		public Vector3 SurfaceNormal() {
			return Vector3.Cross (p0 - p1, p1 - p2).normalized;
		}

		public bool IsPlanar() {
			Vector3 d = Vector3.Cross (p0 - p1, p1 - p2).normalized - Vector3.Cross (p1 - p2, p2 - p3).normalized;
			return d.magnitude < ConcreteArea.SMALL_NUM;
		}

		public override bool Contains(Vector3 point) {
			return point == p0 || point == p1 || point == p2; // also check if the point is for-sure on the surface of the plane?
		}

		public override Vector3 GetClosestPointTo(Vector3 point) {
			return Vector3.zero; // use Planar, clamp to edges/corners
		}

		public override Vector3 GetClosestPointOnSurface(Vector3 point, out Vector3 surfaceNormal) {
			surfaceNormal = Vector3.zero;
			return Vector3.zero; // use Planar, clamp to edges/corners
		}

		public override bool Raycast(Ray r, out RaycastHit hit) {
			hit = new RaycastHit ();
			return false; // check that one page...
		}

		public override int Wireframe(Vector3[] out_points) {
			out_points [0] = p0;
			out_points [1] = p1;
			out_points [2] = p2;
			out_points [3] = p0;
			return 4;
		}

		public bool CollidesWith(Triangle t) {
			return false; // do sphere check, then planar ray intersect, then check if ray intersect is in sphere, then check if ray is in triangle
		}

		public override bool CollidesWith(Area area) {
			if(area.GetType() == typeof(Sphere)) {
				// 
			}
			return base.CollidesWith (area);
		}

		public Sphere BoundingSphere() {
			return new Sphere(Vector3.zero, 1); // Circumscribed
		}

		public Sphere InscribedSphere() {
			return new Sphere(Vector3.zero, 1); // Circular-inscription
		}

		public override Vector3 GetLocation() { return (p0+p1+p2)/3; } // circum-scribe and circularly inscribed
	}
}
