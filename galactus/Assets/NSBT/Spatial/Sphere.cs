using UnityEngine;
using System.Collections;

namespace Spatial {
	public class Sphere : Area {
		public Vector3 center;
		public float radius;
		public bool Contains(Vector3 p) {
			return Vector3.Distance (center, p) <= radius;
		}
		public Vector3 GetClosestPointTo(Vector3 point) {
			return GetClosestPointTo (point, center, radius);
		}
		static public Vector3 GetClosestPointTo(Vector3 point, Vector3 center, float radius) {
			Vector3 delta = point - center;
			if(delta.sqrMagnitude < radius*radius)
				return point;
			return center + (delta.normalized * radius);
		}
		public Vector3 GetClosestPointOnSurface(Vector3 point) {
			return GetClosestPointOnSurface (point, center, radius);
		}
		static public Vector3 GetClosestPointOnSurface(Vector3 point, Vector3 center, float radius) {
			Vector3 delta = point - center;
			return center + (delta.normalized * radius);
		}
		public static bool CollidesWith(Vector3 Acenter, float Aradius, Vector3 Bcenter, float Bradius) {
			return (Acenter - Bcenter).sqrMagnitude < (Aradius+Bradius);
		}
		public bool CollidesWith(Area area) {
			if(area.GetType() == typeof(Sphere)) {
				Sphere s = area as Sphere;
				return CollidesWith (center, radius, s.center, s.radius);
			} else if(area.GetType() == typeof(AABB)) {
				AABB b = area as AABB;
				return AABB.CollidesWithSphere(b.min, b.max, center, radius);
			}
			throw new System.Exception("don't know how to collide with "+area);
		}
		public Vector3 GetLocation() { return center; }
	}
}