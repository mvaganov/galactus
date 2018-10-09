using UnityEngine;

namespace Spatial {
	public class AABB : ConcreteArea {
		public Vector3 min, max;

		public override void Translate (Vector3 delta) { min += delta; max += delta; }
		public override void Rotate (Quaternion q) { }
		public override void Scale (Vector3 coefficient) { min.Scale(coefficient); max.Scale(coefficient); }

		override public string ToString() { return OMU.Serializer.Stringify(this); }

		public Box ToBox() { return new Box ((min + max) / 2, max - min, Quaternion.identity); }

		public override bool Contains(Vector3 p) {
			return Contains (p, min, max);
		}
		public static bool Contains(Vector3 p, Vector3 min, Vector3 max) {
			return min.x <= p.x && p.x <= max.x
				&& min.y <= p.y && p.y <= max.y
				&& min.z <= p.z && p.z <= max.z;
		}
		public static bool ContainsExcludeBorder(Vector3 p, Vector3 min, Vector3 max) {
			return min.x < p.x && p.x < max.x
				&& min.y < p.y && p.y < max.y
				&& min.z < p.z && p.z < max.z;
		}
		public override Vector3 GetClosestPointTo(Vector3 point) {
			Vector3 p = GetClosestPointTo(point, min, max);
			return p;
		}
		static public Vector3 GetClosestPointTo(Vector3 point, Vector3 min, Vector3 max) {
			Vector3 p = point;
			if (p.x < min.x) { p.x = min.x; }
			else if (p.x > max.x) { p.x = max.x; }
			if (p.y < min.y) { p.y = min.y; }
			else if (p.y > max.y) { p.y = max.y; }
			if (p.z < min.z) { p.z = min.z; }
			else if (p.z > max.z) { p.z = max.z; }
			return p;
		}
		public override Vector3 GetClosestPointOnSurface(Vector3 point, out Vector3 surfaceNormal) {
			return GetClosestPointOnSurface (point, min, max, out surfaceNormal);
		}
		static public Vector3 GetClosestPointOnSurface(Vector3 point, Vector3 min, Vector3 max, out Vector3 surfaceNormal) {
			Vector3 p = point;
			surfaceNormal = Vector3.zero;
			float dx = float.MaxValue, dy = float.MaxValue, dz = float.MaxValue;
			int clipX = 0, clipY = 0, clipZ = 0;
				 if (p.x < min.x) { p.x = min.x; clipX = -1; }
			else if (p.x > max.x) { p.x = max.x; clipX = 1; }
			else dx = Mathf.Min(p.x - min.x, max.x - p.x);
				 if(p.y < min.y) { p.y = min.y; clipY = -1; }
			else if (p.y > max.y) { p.y = max.y; clipY = 1; }
			else dy = Mathf.Min(p.y - min.y, max.y - p.y);
				 if (p.z < min.z) { p.z = min.z; clipZ = -1; }
			else if (p.z > max.z) { p.z = max.z; clipZ = 1; }
			else dz = Mathf.Min(p.z - min.z, max.z - p.z);
			bool pointInside = (clipX == 0 && clipY == 0 && clipZ == 0);
			if (pointInside) {
				if (dx <= dy && dx <= dz) if ((p.x - min.x) <= ((max.x - min.x) / 2)) { p.x = min.x; clipX = -1; } else { p.x = max.x; clipX = 1; }
				if (dy <= dx && dy <= dz) if ((p.y - min.y) <= ((max.y - min.y) / 2)) { p.y = min.y; clipY = -1; } else { p.y = max.y; clipY = 1; }
				if (dz <= dx && dz <= dy) if ((p.z - min.z) <= ((max.z - min.z) / 2)) { p.z = min.z; clipZ = -1; } else { p.z = max.z; clipZ = 1; }
			}
			if ((clipX != 0 && clipY == 0 && clipZ == 0)
			|| (clipX == 0 && clipY != 0 && clipZ == 0)
			|| (clipX == 0 && clipY == 0 && clipZ != 0))
				surfaceNormal = new Vector3(clipX, clipY, clipZ);
			else {
				surfaceNormal = point - p;
				if (pointInside) surfaceNormal *= -1;
				if (surfaceNormal == Vector3.zero) surfaceNormal = new Vector3(clipX, clipY, clipZ);
				surfaceNormal.Normalize();
			}
			return p;
		}
		public static bool CollidesWithAABB(Vector3 Amin, Vector3 Amax, Vector3 Bmin, Vector3 Bmax) {
			return	!( Bmin.x > Amax.x || Bmax.x < Amin.x
						|| Bmin.y > Amax.y || Bmax.y < Amin.y 
						|| Bmin.z > Amax.z || Bmax.z < Amin.z);
		}

		public bool CollidesWithSphere(Sphere s) { return CollidesWithSphere(min, max, s.center, s.radius); }

		public override bool CollidesWith(Area area) {
			if(area.GetType() == typeof(Sphere)) { return CollidesWithSphere (area as Sphere);
			}else if(area.GetType() == typeof(AABB)) {
				AABB b = area as AABB;
				return AABB.CollidesWithAABB(min, max, b.min, b.max);
			} else if(area.GetType() == typeof(Box)) { return (area as Box).CollidesWithAABB (this);
			} else if(area.GetType() == typeof(Planar)) { 
				return ToBox ().CollidesWithPlane (area as Planar);
			}
			return base.CollidesWith (area);
		}

		private static Vector3[] faceDirections = { Vector3.left, Vector3.right, Vector3.down, Vector3.up, Vector3.back, Vector3.forward };
		public override bool Raycast(Ray r, out RaycastHit hit) {
			return Raycast(r, out hit, this.min, this.max);
		}
		public static bool Raycast(Ray r, out RaycastHit hit, Vector3 min, Vector3 max) {
			hit = new RaycastHit();
			//Plane p;
			Planar p;
			float dist = float.MaxValue, closest = float.MaxValue;
			Vector3 point = Vector3.zero;
			for(int i = 0; i < faceDirections.Length; ++i) {
				//p = new UnityEngine.Plane(faceDirections[i], ((i&1)==0)?min:max);
				p = new Planar(((i&1)==0)?min:max, faceDirections[i]);
				bool hitIsValid = Vector3.Dot(r.direction, faceDirections[i]) < 0 && p.Raycast(r, out dist) && Contains(point = r.GetPoint(dist),min,max);
				if (hitIsValid && dist < closest) {
					closest = dist;
					hit.point = point; hit.distance = dist; hit.normal = p.normal;
				}
			}
			return (closest < float.MaxValue);
		}
		public Sphere BoundingSphere() {
			return new Sphere ((max + min) / 2, ((max - min) / 2).magnitude);
		}
		public bool CollidesWith(Sphere s) {
			if (BoundingSphere ().CollidesWith (s)) {
				return CollidesWithSphere (min, max, s.center, s.radius);
			}
			return false;
		}
		public static bool CollidesWithSphere(Vector3 min, Vector3 max, Vector3 center, float radius) {
			bool xRange = center.x >= min.x && center.x <= max.x;
			bool yRange = center.y >= min.y && center.y <= max.y;
			bool zRange = center.z >= min.z && center.z <= max.z;
			bool xRange2 = false, yRange2 = false, zRange2 = false;
			// fully inside
			if((xRange && yRange && zRange)
			// left || right
			|| (zRange && yRange && (xRange2 = (center.x >= min.x-radius && center.x <= max.x+radius)))
			// top || bottom
			|| (xRange && zRange && (yRange2 = (center.y >= min.y-radius && center.y <= max.y+radius)))
			// forward || backward
			|| (xRange && yRange && (zRange2 = (center.z >= min.z-radius && center.z <= max.z+radius)))) {
				return true;
			} else {
				// if it is possibly in the extended range
				if(xRange2 && yRange2 && zRange2) {
					// if it's in a corner...
					float rr = radius * radius;
					return ((center-max).sqrMagnitude < rr)
					|| ((center-min).sqrMagnitude < rr)
					|| ((center-new Vector3(max.x, max.y, min.z)).sqrMagnitude < rr)
					|| ((center-new Vector3(max.x, min.y, max.z)).sqrMagnitude < rr)
					|| ((center-new Vector3(max.x, min.y, min.z)).sqrMagnitude < rr)
					|| ((center-new Vector3(min.x, max.y, max.z)).sqrMagnitude < rr)
					|| ((center-new Vector3(min.x, max.y, min.z)).sqrMagnitude < rr)
					|| ((center-new Vector3(min.x, min.y, max.z)).sqrMagnitude < rr);
				}
			}
			return false;
		}
		public override Vector3 GetLocation() { return (min + max) / 2; }
		public static int Wireframe(Vector3[] out_corners, Vector3 min, Vector3 max) {
			if(out_corners.Length < 24) {
				Debug.LogWarning("expecting to make a box with 24 points, 4 per side");
			}
			int index = 0;
			// top
			out_corners[index++] = new Vector3(max.x, max.y, max.z);
			out_corners[index++] = new Vector3(max.x, max.y, min.z);
			out_corners[index++] = new Vector3(min.x, max.y, min.z);
			out_corners[index++] = new Vector3(min.x, max.y, max.z);
			// front
			out_corners[index++] = new Vector3(max.x, max.y, max.z);
			out_corners[index++] = new Vector3(min.x, max.y, max.z);
			out_corners[index++] = new Vector3(min.x, min.y, max.z);
			out_corners[index++] = new Vector3(max.x, min.y, max.z);
			// right
			out_corners[index++] = new Vector3(max.x, min.y, min.z);
			out_corners[index++] = new Vector3(max.x, max.y, min.z);
			out_corners[index++] = new Vector3(max.x, max.y, max.z);
			out_corners[index++] = new Vector3(max.x, min.y, max.z);
			// bottom
			out_corners[index++] = new Vector3(max.x, min.y, min.z);
			out_corners[index++] = new Vector3(max.x, min.y, max.z);
			out_corners[index++] = new Vector3(min.x, min.y, max.z);
			out_corners[index++] = new Vector3(min.x, min.y, min.z);
			// back
			out_corners[index++] = new Vector3(min.x, min.y, min.z);
			out_corners[index++] = new Vector3(min.x, max.y, min.z);
			out_corners[index++] = new Vector3(max.x, max.y, min.z);
			out_corners[index++] = new Vector3(max.x, min.y, min.z);
			// left
			out_corners[index++] = new Vector3(min.x, min.y, min.z);
			out_corners[index++] = new Vector3(min.x, max.y, min.z);
			out_corners[index++] = new Vector3(min.x, max.y, max.z);
			out_corners[index++] = new Vector3(min.x, min.y, max.z);
			return index;
		}
		public override int Wireframe (Vector3[] out_wireframeVertices) { return Wireframe (out_wireframeVertices, min, max); }
	}
}