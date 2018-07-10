using UnityEngine;
using System.Collections;

namespace Spatial {
	public class AABB : Area {
		public Vector3 min, max;
		public bool Contains(Vector3 p) {
			return Contains (p, min, max);
		}
		public static bool Contains(Vector3 p, Vector3 min, Vector3 max) {
			return min.x <= p.x && p.x <= max.x
				&& min.y <= p.y && p.y <= max.y
				&& min.z <= p.z && p.z <= max.z;
		}
		public Vector3 GetClosestPointTo(Vector3 point) {
			return GetClosestPointTo (point, min, max);
		}
		static public Vector3 GetClosestPointTo(Vector3 point, Vector3 min, Vector3 max) {
			Vector3 p = point;
			if(p.x < min.x) p.x = min.x;
			else if(p.x > max.x) p.x = max.x;
			if(p.y < min.y) p.y = min.y;
			else if(p.x > max.y) p.y = max.y;
			if(p.z < min.z) p.z = min.z;
			else if(p.z > max.z) p.z = max.z;
			return p;
		}
		public Vector3 GetClosestPointOnSurface(Vector3 point) {
			return GetClosestPointOnSurface (point, min, max);
		}
		static public Vector3 GetClosestPointOnSurface(Vector3 point, Vector3 min, Vector3 max) {
			Vector3 p = point;
			if(p.x < min.x) p.x = min.x;
			else if(p.x > max.x) p.x = max.x;
			else {	p.x = ((p.x - min.x) <= ((max.x - min.x) / 2))?min.x:max.x;	}
			if(p.y < min.y) p.y = min.y;
			else if(p.x > max.y) p.y = max.y;
			else {	p.y = ((p.y - min.y) <= ((max.y - min.y) / 2))?min.y:max.y;	}
			if(p.z < min.z) p.z = min.z;
			else if(p.z > max.z) p.z = max.z;
			else {	p.z = ((p.z - min.z) <= ((max.z - min.z) / 2))?min.z:max.z;	}
			return p;
		}
		public static bool CollidesWith(Vector3 Amin, Vector3 Amax, Vector3 Bmin, Vector3 Bmax) {
			return	!( Bmin.x > Amax.x || Bmax.x < Amin.x
			         || Bmin.y > Amax.y || Bmax.y < Amin.y 
			         || Bmin.z > Amax.z || Bmax.z < Amin.z);
		}
		public bool CollidesWith(Area area) {
			if(area.GetType() == typeof(AABB)) {
				AABB b = area as AABB;
				return AABB.CollidesWith(min, max, b.min, b.max);
			} else if(area.GetType() == typeof(Sphere)) {
				Sphere s = area as Sphere;
				return CollidesWithSphere(min, max, s.center, s.radius);
			}
			throw new System.Exception("don't know how to collide with "+area);
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
		public Vector3 GetLocation() { return (min + max) / 2; }
	}
}