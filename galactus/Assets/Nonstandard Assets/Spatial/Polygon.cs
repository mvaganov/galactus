using UnityEngine;
using System.Collections;

namespace Spatial {
	// TODO complete me
	public class Polygon : ConcreteArea {
		public Vector3[] p;

		public Polygon(){}
		public Polygon(Vector3[] p){
			this.p = new Vector3[p.Length];
			for (int i = 0; i < p.Length; ++i) {
				this.p [i] = p [i];
			}
			Flatten ();
		}
		public override void Translate (Vector3 delta) { for (int i = 0; i < p.Length; ++i) { p [i] += delta; } }
		public override void Rotate (Quaternion q) { for (int i = 0; i < p.Length; ++i) { p [i] = q * p [i]; } }
		public override void Scale (Vector3 coefficient) { for (int i = 0; i < p.Length; ++i) { p [i].Scale(coefficient); }	}

		/// <summary>
		/// if this polygon has more than 3 points, this method will find the average plane of the points, and clamp each point to the plane
		/// </summary>
		public void Flatten() {
			if (p.Length <= 3) return; 
			Vector3 avg = GetLocation ();
			Vector3 avgCross = Vector3.zero, c;
			for (int i = 1; i < p.Length; ++i) {
				c = Vector3.Cross (p [i - 1] - avg, p [i] - avg);
				avgCross += c;
			}
			c = Vector3.Cross (p [p.Length] - avg, p [0] - avg);
			avgCross /= p.Length;
			Planar plane = new Planar (avg, avgCross.normalized);
			for (int i = 0; i < p.Length; ++i) {
				p [i] = plane.GetClosestPointTo (p [i]);
			}
		}

		public Vector3 SurfaceNormal() {
			return Vector3.Cross (p[0] - p[1], p[1] - p[2]).normalized;
		}

		public override bool Contains(Vector3 point) {
			return false; // also check if the point is for-sure on the surface of the plane?
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
			if (out_points.Length != p.Length+1) {
				out_points = new Vector3[p.Length+1];
			}
			for (int i = 0; i < p.Length; ++i) {
				out_points [i] = p [i];
			}
			out_points [p.Length-1] = p[0];
			return p.Length + 1;
		}

		public bool CollidesWithSphere(Sphere s) {
			// TODO find the closest point on the plane. if it is within the lines, true. if it is outside the lines, get closest point on the line, and if it is within the radius, mark true
			Planar plane = new Planar (p[0], SurfaceNormal ());
			bool hitPlane = plane.CollidesWithSphere (s);
			if (hitPlane) {
				
			}
			return false;
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

		public override Vector3 GetLocation() {
			Vector3 center = Vector3.zero;
			for (int i = 0; i < p.Length; ++i) {
				center += p [i];
			}
			return center / p.Length;
		}
	}
}
