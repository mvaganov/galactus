using UnityEngine;
using System.Collections;

namespace Spatial {
    // called 'Planar' to eliminate confusion with UnityEngine.Plane
    public class Planar : ConcreteArea {

        public Vector3 point, normal;
        public Planar() { }
        public Planar(Vector3 point, Vector3 normal) { this.point = point;  this.normal = normal; }

		public override void Translate (Vector3 delta) { point += delta; }
        public override void Rotate (Quaternion q) { point = q * point; normal = q * normal; }
		public override void Scale (Vector3 coefficient) { point.Scale(coefficient); }

        public override void FixGeometryProblems() { if (normal == Vector3.zero) { normal = Vector3.up; } normal.Normalize(); }

        public override bool Contains(Vector3 point) {
            return Contains(point, this.point, this.normal);
        }

		public static bool Contains(Vector3 point, Vector3 planePoint, Vector3 planeNormal) {
			return (Vector3.Dot(planeNormal, point - planePoint) == 0);
		}

		public override Vector3 GetClosestPointTo(Vector3 point) {
            return GetClosestPointTo(point, this.point, this.normal);
        }

		public bool IsFacing(Vector3 point) {
			return Vector3.Dot (normal, point - this.point) >= 0;
		}

		public static bool IsFacing(Vector3 point, Vector3 planePoint, Vector3 planeNormal) {
			return Vector3.Dot (planeNormal, point - planePoint) > 0;
		}

        public static Vector3 GetClosestPointTo(Vector3 point, Vector3 planePoint, Vector3 planeNormal) {
            Plane p = new Plane(planeNormal, planePoint);
            Ray r = new Ray(point, planeNormal);
            float d;
            p.Raycast(r, out d);
            return point + planeNormal * d;
        }

		public override Vector3 GetClosestPointOnSurface(Vector3 point, out Vector3 surfaceNormal) {
			surfaceNormal = IsFacing(point)?normal:-normal;
            return GetClosestPointTo(point);
        }

		public static Vector3 GetClosestPointOnSurface(Vector3 point, out Vector3 surfaceNormal, Vector3 planePoint, Vector3 planeNormal) {
			surfaceNormal = IsFacing(point, planePoint, planeNormal)?planeNormal:-planeNormal;
			return GetClosestPointTo(point, planePoint, planeNormal);
		}

		public bool CollidesWith(Spatial.Planar plane, out Ray r) {
			int result = CollidesWith (this, plane, out r);
			return result != 0;
		}

        /// <param name="Pn1">Pn1.</param>
        /// <param name="Pn2">Pn2.</param>
        /// <param name="r">The 3D intersection of two planes colliding (a ray)</param>
        /// <returns>{0: disjoint (no intersection), 1: planes coincide (same plane), 2: r is good output}</returns>
		public static int CollidesWith(Planar Pn1, Planar Pn2, out Ray r) {
			r = new Ray ();
			Vector3   u = Vector3.Cross(Pn1.normal, Pn2.normal);          // cross product
			float    ax = (u.x >= 0 ? u.x : -u.x);
			float    ay = (u.y >= 0 ? u.y : -u.y);
			float    az = (u.z >= 0 ? u.z : -u.z);
			// test if the two planes are parallel
			if ((ax+ay+az) < SMALL_NUM) {        // Pn1 and Pn2 are near parallel
				// test if disjoint or coincide
				Vector3   v = Pn2.point - Pn1.point;
				if (Vector3.Dot(Pn1.normal, v) == 0) // Pn2.V0 lies in Pn1
					return 1;                       // Pn1 and Pn2 coincide
				return 0;                     // Pn1 and Pn2 are disjoint
			}
			// Pn1 and Pn2 intersect in a line
			// first determine max abs coordinate of cross product
			int maxc;                       // max coordinate
			if(ax>ay){ if (ax > az) { maxc = 1; } else { maxc = 3; }
			} else {   if (ay > az) { maxc = 2; } else { maxc = 3; }
			}
			// next, to get a point on the intersect line
			// zero the max coord, and solve for the other two
			Vector3 iP = Vector3.zero; // intersect point
			float   d1, d2;            // the constants in the 2 plane equations
			d1 = -Vector3.Dot(Pn1.normal, Pn1.point);  // note: could be pre-stored  with plane
			d2 = -Vector3.Dot(Pn2.normal, Pn2.point);  // ditto
			switch (maxc) {             // select max coordinate
			case 1:                     // intersect with x=0
				iP.x = 0;
				iP.y = (d2*Pn1.normal.z - d1*Pn2.normal.z) /  u.x;
				iP.z = (d1*Pn2.normal.y - d2*Pn1.normal.y) /  u.x;
				break;
			case 2:                     // intersect with y=0
				iP.x = (d1*Pn2.normal.z - d2*Pn1.normal.z) /  u.y;
				iP.y = 0;
				iP.z = (d2*Pn1.normal.x - d1*Pn2.normal.x) /  u.y;
				break;
			case 3:                     // intersect with z=0
				iP.x = (d2 * Pn1.normal.y - d1 * Pn2.normal.y) / u.z;
				iP.y = (d1 * Pn2.normal.x - d2 * Pn1.normal.x) / u.z;
				iP.z = 0;
				break;
			}
			r.origin = iP;
			r.direction = u.normalized;
			return 2;
		}

		public override bool CollidesWith(Area area) {
			if(area.GetType() == typeof(Line)) { return CollidesWith(area as Line); }
			return base.CollidesWith (area);
        }

		public bool CollidesWithSphere(Sphere s) {
			float dist;
			Raycast(new Ray(s.center, -this.normal), out dist);
			if (dist < 0) dist *= -1;
			return dist < s.radius;
		}

		public override Vector3 GetLocation() {
            return point;
        }

		public override bool Raycast(Ray r, out RaycastHit hit) { return Raycast (this, r, out hit) > 0; }

		public bool Raycast(Ray r, out float rayDistance) {
			Vector3 w = r.origin - this.point;
			float D = Vector3.Dot(this.normal, r.direction), N = -Vector3.Dot(this.normal, w);
			if (Mathf.Abs(D) < SMALL_NUM) { // ray parallel to plane
				rayDistance = 0;
				if (N == 0) return true; // ray lies in plane
				return false; // no intersection
			}
			rayDistance = N / D;
			return true;
		}

		// intersect3D_SegmentPlane(): find the 3D intersection of a segment and a plane
		/// <summary>
		/// Raycast the specified plane and ray, putting output in the given rayhit.
		/// </summary>
		/// <param name="plane">Plane.</param>
		/// <param name="ray">Ray.</param>
		/// <param name="rayhit">Rayhit.</param>
		public static int Raycast(Spatial.Planar plane, Ray ray, out RaycastHit rayhit) {
			rayhit = new RaycastHit ();
			rayhit.distance = 0;
			rayhit.point = ray.origin;
			rayhit.normal = plane.normal;
			Vector3 w = ray.origin - plane.point;
			float D = Vector3.Dot(plane.normal, ray.direction), N = -Vector3.Dot(plane.normal, w);
			if (Mathf.Abs(D) < SMALL_NUM) { // ray parallel to plane
				if (N == 0) return 2; // ray lies in plane
				else return 0; // no intersection
			}
			// they are not parallel compute intersect param
			rayhit.distance = N / D;
			rayhit.point = ray.origin + rayhit.distance * ray.direction;                  // compute segment intersect point
			return 1;
		}

        public static bool Raycast(Ray r, out RaycastHit hit, Vector3 planePoint, Vector3 planeNormal) {
            hit = new RaycastHit();
            Plane p = new Plane(planeNormal, planePoint);
            float d;
			bool actualHit = p.Raycast(r, out d);
            hit.point = r.origin + r.direction * d;
			hit.normal = IsFacing(r.origin, planePoint, planeNormal)?planeNormal:-planeNormal;
			return actualHit;
        }

		public override int Wireframe(Vector3[] out_points) { return Wireframe(out_points, point, normal); }

		public static int Wireframe(Vector3[] out_points, Vector3 point, Vector3 normal) {
            Vector3 otherDir = Vector3.up == normal ? Vector3.right : Vector3.up;
            Vector3 dir = Vector3.Cross(normal, otherDir);
            Vector3 perpDir = Vector3.Cross(dir, normal);
			out_points[0] = point + normal;
			out_points[1] = point;
            Vector3[] shifts = { dir, perpDir, -dir, -perpDir};
            float rad = 1;
			int i = 2;
			for (; i < out_points.Length && i < 24; ++i) {
                out_points[i] = out_points[i - 1] + rad * shifts[i % shifts.Length];
                rad += 1;
            }
			return i;
        }
    }
}
