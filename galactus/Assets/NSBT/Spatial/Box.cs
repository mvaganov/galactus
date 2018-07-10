using UnityEngine;
using System.Collections;

namespace Spatial {
	public class Box : Area {
		public Vector3 center, size;
		public Quaternion rotation;

		static public Vector3 UnRotate(Quaternion rotation, Vector3 position) {
			return Quaternion.Inverse (rotation) * position;
		}

		public bool Contains(Vector3 point) {
			Vector3 half = size / 2;
			return AABB.Contains (UnRotate(rotation, (center - point)), -1*half, half);
		}

		public Vector3 GetClosestPointTo(Vector3 point) {
			Vector3 half = size / 2;
			return AABB.GetClosestPointTo (UnRotate(rotation, (center - point)), -1*half, half);
		}

		public Vector3 GetClosestPointOnSurface(Vector3 point) {
			Vector3 half = size / 2;
			return AABB.GetClosestPointOnSurface (UnRotate(rotation, (center - point)), -1*half, half);
		}

		public bool CollidesWith(Area area) {
			if(area.GetType() == typeof(Sphere)) {
				Sphere s = area as Sphere;
				Vector3 half = size / 2;
				return AABB.CollidesWithSphere(-1*half, half, UnRotate(rotation, (center - s.center)), s.radius);
			} else if(area.GetType() == typeof(Box)) {

			}
			throw new System.Exception("don't know how to collide with "+area);
		}
		public Vector3 GetLocation() { return center; }
	}
}