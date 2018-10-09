using UnityEngine;
using System.Collections;

namespace Spatial {
	// TODO FIXME finish this class plz.
	public class Cone : ConcreteArea {
		public Vector3 start, end;
		public float rStart, rEnd;

		public override void Translate (Vector3 delta) { start += delta; end += delta; }
		public override void Rotate (Quaternion q) { start = q * start; end = q * end; }
		public override void Scale (Vector3 coefficient) {
			start.Scale(coefficient); end.Scale(coefficient);
			float s = (coefficient.x + coefficient.y + coefficient.z) / 3;
			rStart *= s; rEnd *= s;
		}

		public Vector3 GetDirection() {
			return (end - start).normalized;
		}

		public override bool Contains(Vector3 point) {
			Vector3 coneDelta = end - start;
			float mag = coneDelta.magnitude;
			Vector3 dir = coneDelta / mag;
			bool pointInsideStart = Planar.IsFacing (point, start, dir);
			bool pointInsideEnd = Planar.IsFacing (point, end, -dir);
			if (!pointInsideStart || !pointInsideEnd) { return false; }
			float d;
			Vector3 p = Line.GetClosestPointTo (point, start, end, out d);
			float radAt_d = (rEnd - rStart) * (d / mag) + rStart;
			Vector3 fromCenter = point - p;
			float dist = fromCenter.magnitude;
			return dist <= radAt_d;
		}
		
		public override Vector3 GetClosestPointTo(Vector3 point) {
			if (Contains (point)) {
				return point;
			}
			Vector3 o;
			return GetClosestPointOnSurface(point, out o);
		}
		
		public override Vector3 GetClosestPointOnSurface(Vector3 point, out Vector3 surfaceNormal) {
			Vector3 coneDelta = end - start;
			float mag = coneDelta.magnitude;
			Vector3 dir = coneDelta / mag;
			Vector3 relativeP = point - start;
			Vector3 refPoint = Circle.GetClosestPointOnEdgeTo (relativeP, Vector3.zero, dir, 1);
			Vector3 _start = start + refPoint * rStart, _end = end + refPoint * rEnd;
			Vector3 _dir = (_end - _start).normalized;

			float d;
			Vector3 p = Line.GetClosestPointTo (point, start, end, out d);
			float radAt_d = (rEnd - rStart) * (d / mag) + rStart;
			Vector3 fromCenter = point - p;
			float distFromCenterLine = fromCenter.magnitude;

			bool pointInsideStart = false, pointInsideEnd = false;
			if (distFromCenterLine > radAt_d) {
				pointInsideStart = Planar.IsFacing (point, _start, _dir);
				pointInsideEnd = Planar.IsFacing (point, _end, -_dir);
			} else {
				pointInsideStart = Planar.IsFacing (point, start, dir);
				pointInsideEnd = Planar.IsFacing (point, end, -dir);
			}

			if (!pointInsideStart) {
				return Circle.GetClosestPointOnSurface (point, out surfaceNormal, start, -dir, rStart);
			} else if (!pointInsideEnd) {
				return Circle.GetClosestPointOnSurface (point, out surfaceNormal, end, dir, rEnd);
			}
			refPoint = Line.GetClosestPointTo (point, _start, _end);
			surfaceNormal = (point - refPoint).normalized;
			return refPoint;
		}
		
		public override bool CollidesWith(Area area) {
			return base.CollidesWith (area);
		}
		
		public override Vector3 GetLocation() {
			return start;
		}

		public override bool Raycast(Ray r, out RaycastHit hit) {
			hit = new RaycastHit();
			hit.distance = -1;
			Vector3 coneDelta = end - start;
			float mag = coneDelta.magnitude;
			Vector3 dir = coneDelta / mag;
			// raycast start plate
			RaycastHit rayHit = new RaycastHit();
			if (rStart > 0 && Circle.Raycast (r, out rayHit, start, dir, rStart)) {
				hit = rayHit;
			}
			// raycast end plate
			if (rEnd > 0 && Circle.Raycast (r, out rayHit, end, -dir, rEnd)
			&& (hit.distance < 0 || rayHit.distance < hit.distance)) {
				hit = rayHit;
			}
			// find the shortest line between the ray and start->end, which will raycast the midsection
			// TODO WRONG! needs a new algorithm!
			Line resultLine = new Line();
			Line coneLine = new Line (start, end);
			float rayDistMax = Vector3.Distance (start, r.origin) + coneDelta.magnitude;
			Line rayLine = new Line (r.origin, r.origin + r.direction*rayDistMax);
			Line.GetShortestLineBetweenLines (coneLine, rayLine, resultLine);
			if (resultLine.end != start && resultLine.end != end ) {
				float d;
				Vector3 p = Line.GetClosestPointTo (resultLine.end, start, end, out d);
				float radAt_d = (rEnd - rStart) * (d / mag) + rStart;
				Vector3 fromCenter = resultLine.end - p;
				float distFromCenterLine = fromCenter.magnitude;
				if (distFromCenterLine < radAt_d) {
					Vector3 rayTrace = (resultLine.end - r.origin);
					float rayDist = rayTrace.magnitude;
					if (hit.distance < 0 || rayDist < hit.distance) {
						hit.point = resultLine.end;
						hit.distance = rayDist;
						hit.normal = rayTrace / -rayDist;
					}
				}
			}
			// return the closest collision: start, end, or midsection
			return hit.distance >= 0;
        }

		public override int Wireframe (Vector3[] out_wireframeVertices) {
			int index = 0;
			Vector3 d = GetDirection ();
			Vector3 crossDir = (d != Vector3.up) ? Vector3.up : Vector3.forward;
			Vector3 r = Vector3.Cross(d, crossDir).normalized;
			int columns = 12;
			if (rStart > 0) {
				NS.Lines.WriteArc (ref out_wireframeVertices, columns, d, r*rStart, 360, start, index);
				index += columns;
			} else {
				out_wireframeVertices [index++] = start;
			}
			if (rEnd > 0) {
				NS.Lines.WriteArc (ref out_wireframeVertices, columns, d, r*rEnd, 360, end, index);
				index += columns;
			} else {
				out_wireframeVertices [index++] = end;
			}
			for (int i = 1; index+1 < out_wireframeVertices.Length && i < columns; ++i) {
				out_wireframeVertices [index++] = out_wireframeVertices [i];
				out_wireframeVertices [index++] = out_wireframeVertices [i+columns];
			}
			return index;
		}
    }
}