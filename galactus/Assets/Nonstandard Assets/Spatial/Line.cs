using UnityEngine;

namespace Spatial
{
    public class Line : ConcreteArea {
        public Vector3 start, end;

		public override void Translate (Vector3 delta) { start += delta; end += delta; }
		public override void Rotate (Quaternion q) { start = q * start; end = q * end; }
		public override void Scale (Vector3 coefficient) { start.Scale(coefficient); end.Scale(coefficient); }

		public Line() { start = end = Vector3.zero; }
		public Line(Vector3 start, Vector3 end) { this.start = start; this.end = end; }

		public override bool Contains(Vector3 point) { return point == start || point == end || GetClosestPointTo(point) == point; }

        public static Vector3 GetClosestPointTo(Vector3 point, Vector3 start, Vector3 end) {
			Vector3 delta = end - start;
			float dist = delta.magnitude;
			Vector3 n = delta / dist;
			Ray r = new Ray(start, n);
			float rayDist;
			Plane p = new Plane(n, point); // TODO use Planar.Raycast instead
			p.Raycast(r, out rayDist);
			if(rayDist <= 0) { return start;
			} else if(rayDist >= dist) { return end;
			}
			return n * rayDist + start;
        }

		public static Vector3 GetClosestPointTo(Vector3 point, Vector3 start, Vector3 end, out float distanceAlongLine) {
			Vector3 delta = end - start;
			float dist = delta.magnitude;
			Vector3 n = delta / dist;
			Ray r = new Ray(start, n);
			Plane p = new Plane(n, point); // TODO use Planar.Raycast instead
			p.Raycast(r, out distanceAlongLine);
			if(distanceAlongLine <= 0) { return start;
			} else if(distanceAlongLine >= dist) { return end;
			}
			return n * distanceAlongLine + start;
		}

		public static bool IsPointInRange(Vector3 point, Vector3 start, Vector3 end) {
			if (start == end) { return point == start; }
			Vector3 d = (end - start).normalized;
			return Planar.IsFacing (point, start, d) && Planar.IsFacing(point, end, -d);
		}

		public bool IsPointInRange(Vector3 point) { return IsPointInRange (point, start, end); }

		public override Vector3 GetClosestPointTo(Vector3 point) {
            Vector3 delta = end - start;
            float dist = delta.magnitude;
            Vector3 n = delta / dist;
            Plane p = new Plane(n, point);
            Ray r = new Ray(start, n);
            float rayDist;
            p.Raycast(r, out rayDist);
            if(rayDist <= 0) { return start; }
            if(rayDist >= dist) { return end; }
            return n * rayDist + start;
        }

        public static Vector3 GetClosestPointOnSurface(Vector3 point, out Vector3 surfaceNormal, Vector3 start, Vector3 end) {
            Vector3 p = GetClosestPointTo(point, start, end);
            surfaceNormal = (point - p).normalized;
            return p;
        }

		public override Vector3 GetClosestPointOnSurface(Vector3 point, out Vector3 surfaceNormal) {
            Vector3 p = GetClosestPointTo(point);
            surfaceNormal = (point-p).normalized;
            return p;
        }

        public static bool CollidesWith(Area area, Vector3 start, Vector3 end) {
            RaycastHit rh;
            Vector3 delta = end - start;
            float dist = delta.magnitude;
            Ray r = new Ray(start, delta / dist);
            bool doesHit = area.Raycast(r, out rh);
            return doesHit && rh.distance >= 0 && rh.distance <= dist;
        }

		public override bool CollidesWith(Area area) {
            return CollidesWith(area, start, end);
        }

		public override Vector3 GetLocation() { return start; }

		public Vector3 GetCenter() { return (start+end)/2; }

		public Vector3 GetDelta() { return end - start; }

		public Vector3 GetDirection() { return (end - start).normalized; }

		public float GetDistance() { return Vector3.Distance (start, end); }
		public float Length() { return GetDistance(); }
		public float Magnitude() { return GetDistance(); }

		public Vector3 GetPositionOnLine(float distance) {
			return start + GetDirection () * distance;
		}

		public override bool Raycast(Ray r, out RaycastHit hit) {
            hit = new RaycastHit();
            return false;
        }

		public static void GetShortestLineBetweenLines(Line s1, Line s2, Line out_s) {
			Vector3 u = s1.end - s1.start;
			Vector3 v = s2.end - s2.start;
			Vector3 w = s1.start - s2.start;
			float a = Vector3.Dot (u, u); // always >= 0
			float b = Vector3.Dot (u, v);
			float c = Vector3.Dot (v, v); // always >= 0
			float d = Vector3.Dot (u, w);
			float e = Vector3.Dot (v, w);
			float D = a * c - b * b; // always >= 0
			float sc, sN, sD = D; // sc = sN / sD, default sD >= 0
			float tc, tN, tD = D; // tc = tN / tD, default tD >= 0
			// compute the line parameters of the two closest points
			if (D < ConcreteArea.SMALL_NUM) { // lines are almost parallel
				sN = 0; // force using p0 on segment 1
				sD = 1; // prevent division by zero later
				tN = e;
				tD = c;
			} else { // get closest points on infinite lines
				sN = (b * e - c * d);
				tN = (a * e - b * d);
				if (sN < 0) { // sc < 0 => the s=0 edge is visible
					sN = 0;
					tN = e;
					tD = c;
				} else if (sN > sD) { // sc > 1 => the s=1 edge is visible
					sN = sD;
					tN = e + b;
					tD = c;
				}
			}
			if (tN < 0) { // tc < 0 => the t=0 edge is visible
				tN = 0;
				// recompute sc for this edge
				if (-d < 0) {
					sN = 0;
				} else if (-d > a) {
					sN = sD;
				} else {
					sN = -d;
					sD = a;
				}
			} else if (tN > tD) {
				tN = tD;
				// recompute sc for this edge
				if (-d + b < 0) {
					sN = 0;
				} else if (-d + b > a) {
					sN = sD;
				} else {
					sN = -d + b;
					sD = a;
				}
			}
			// finally do the division to get sc and tc
			sc = (Mathf.Abs (sN) < ConcreteArea.SMALL_NUM ? 0 : sN / sD);
			tc = (Mathf.Abs (tN) < ConcreteArea.SMALL_NUM ? 0 : tN / tD);
			// get the two closest points
			out_s.start = sc * u + s1.start;
			out_s.end =   tc * v + s2.start;
		}
		public override int Wireframe(Vector3[] out_corners) { out_corners [0] = start; out_corners [1] = end; return 2; }

    }
}
